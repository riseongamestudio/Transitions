using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using RiseOn.Serializables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// One object of a <see cref="Transition"/> and the nodes written on it: properties, and branches reaching other objects from it.<br/>
    /// <typeparamref name="T"/> is fixed when the target is set up; the object may be a <typeparamref name="T"/> or derive from it.
    /// </summary>
    [Serializable]
    public sealed class TransitionTarget<T> : ITransitionTarget, IUntypedTarget where T : class {
        [SerializeField]
        internal string label;

        [SerializeField, Required]
        internal SerObject<T> target;

        [SerializeField]
        internal ListSerRef<ITransitionNode<T>> nodes = new();

        [NonSerialized] private bool isReported;

        public TransitionTarget() { }

        public TransitionTarget(T target, string label = null) {
            this.target = new SerObject<T>(target);
            this.label = label;
        }

        /// <summary>
        /// A name for the target, such as its role in a preset; empty leaves it named after its object.
        /// </summary>
        public string Label {
            get => label;
            set => label = value;
        }

        public Type TargetType => typeof(T);

        public T Target {
            get => target.Value;
            set => target = new SerObject<T>(value);
        }

        public IList<ITransitionNode<T>> Nodes => nodes;

        UnityEngine.Object IUntypedTarget.Object {
            get => target.Value as UnityEngine.Object;
            set => target = new SerObject<T>(value as T);
        }

        Type IUntypedNodes.NodeTarget => typeof(T);

        IEnumerable IUntypedNodes.Nodes => nodes;

        bool IUntypedNodes.TryAdd(object node) {
            if (node != null && node is not ITransitionNode<T>) return false;

            nodes.Add((ITransitionNode<T>)node);
            return true;
        }

        void IUntypedTarget.ListWrites(ICollection<(object Instance, PropertyInfo Member)> writes) {
            if (target.IsNull) return;

            var value = target.Value;
            foreach (var node in nodes) (node as IUntypedNode)?.ListWrites(value, writes);
        }

        /// <summary>
        /// Adds a property written through <paramref name="member"/>, such as <c>group => group.alpha</c>.<br/>
        /// The member is named in code, so the build keeps it without any link.xml entry.
        /// </summary>
        public TransitionTarget<T> Add<V>(Expression<Func<T, V>> member, ITransitionModifier<T, V> modifier) {
            nodes.Add(new TransitionProperty<T, V>(MemberAccess.GetName(member), modifier));
            return this;
        }

        /// <summary>
        /// Adds a branch reaching another object through <paramref name="path"/>, such as <c>page => page.Root.Group</c>, and returns it.
        /// </summary>
        public TransitionBranch<T, U> Branch<U>(Expression<Func<T, U>> path) where U : class {
            var branch = new TransitionBranch<T, U>(MemberAccess.GetPath(path));
            nodes.Add(branch);
            return branch;
        }

        public void Apply(float t) {
            if (target.IsNull) {
                TransitionLog.Once(ref isReported, $"Target {(string.IsNullOrEmpty(label) ? string.Empty : $"\"{label}\" ")}({typeof(T).Name}) has no object; it is skipped.");
                return;
            }

            var value = target.Value;
            for (var i = 0; i < nodes.Count; i++) nodes[i]?.Apply(value, t);
        }
    }
}
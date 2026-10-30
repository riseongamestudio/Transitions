using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using RiseOn.Serializables;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Transitions {
    /// <summary>
    /// The nodes of a target without its object: written on whichever object is passed to <see cref="Apply"/>, such as each element a page covers.<br/>
    /// <typeparamref name="T"/> is fixed in code, often an interface naming what the nodes reach. Each object keeps its own rest values.<br/>
    /// A struct passed through an interface is boxed, so writes on it would be lost; it is reported once and skipped.
    /// </summary>
    [Serializable]
    public sealed class TransitionUnboundTarget<T> : IUntypedNodes where T : class {
        [SerializeField]
        internal ListSerRef<ITransitionNode<T>> nodes = new();

        [NonSerialized] private bool isNoneReported;
        [NonSerialized] private bool isStructReported;

        public IList<ITransitionNode<T>> Nodes => nodes;

        Type IUntypedNodes.NodeTarget => typeof(T);

        IEnumerable IUntypedNodes.Nodes => nodes;

        bool IUntypedNodes.TryAdd(object node) {
            if (node != null && node is not ITransitionNode<T>) return false;

            nodes.Add((ITransitionNode<T>)node);
            return true;
        }

        /// <summary>
        /// Adds a property written through <paramref name="member"/>, such as <c>group => group.alpha</c>.<br/>
        /// The member is named in code, so the build keeps it without any link.xml entry.
        /// </summary>
        public TransitionUnboundTarget<T> Add<V>(Expression<Func<T, V>> member, ITransitionModifier<T, V> modifier) {
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

        public void Apply(T target, float t) {
            if (target == null || target is Object unity && unity == null) {
                TransitionLog.Once(ref isNoneReported, $"An unbound target ({typeof(T).Name}) was applied to nothing; it is skipped.");
                return;
            }

            if (target.GetType().IsValueType) {
                TransitionLog.Once(ref isStructReported, $"An unbound target ({typeof(T).Name}) was applied to {target.GetType().Name}, a struct, whose writes would be lost on a boxed copy; it is skipped.");
                return;
            }

            for (var i = 0; i < nodes.Count; i++) nodes[i]?.Apply(target, t);
        }
    }
}
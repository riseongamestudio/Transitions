using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using RiseOn.Serializables;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Transitions {
    /// <summary>
    /// Reaches another object from the target through public properties (<see cref="Path"/>), one or several in a row, and writes its own nodes on it:<br/>
    /// a page's root to its <c>RectTransform</c>, or an interface's <c>Root.Group</c> to the <c>CanvasGroup</c> it names.<br/>
    /// <typeparamref name="U"/> is the type the last property is declared as. The path is read again on every apply, so it follows the object the properties name now;<br/>
    /// when it reaches nothing, the branch is skipped.
    /// </summary>
    [Serializable]
    public sealed class TransitionBranch<T, U> : ITransitionNode<T>, IUntypedNode, IUntypedBranch where T : class where U : class {
        // Picked when the branch is added, and named by the Inspector's header.
        [SerializeField, HideInInspector]
        internal string path;

        [SerializeField]
        internal ListSerRef<ITransitionNode<U>> nodes = new();

        [NonSerialized] private string foundPath;
        [NonSerialized] private PropertyInfo[] hops;
        [NonSerialized] private bool isFound;
        [NonSerialized] private bool isReported;
        [NonSerialized] private bool isEmptyReported;

        public TransitionBranch() { }

        public TransitionBranch(string path) {
            this.path = path;
        }

        public string Path => path;

        public IList<ITransitionNode<U>> Nodes => nodes;

        string IUntypedNode.Name => path;

        Type IUntypedNodes.NodeTarget => typeof(U);

        IEnumerable IUntypedNodes.Nodes => nodes;

        bool IUntypedNodes.TryAdd(object node) {
            if (node != null && node is not ITransitionNode<U>) return false;

            nodes.Add((ITransitionNode<U>)node);
            return true;
        }

        // The nodes are kept as they are: they work on U, which the path reaches from the new type as from T.
        object IUntypedNode.Retarget(Type target) {
            var found = MemberAccess.FindPath(target, path);
            if (found == null || found[^1].PropertyType != typeof(U)) return null;

            var copy = (IUntypedNodes)Activator.CreateInstance(typeof(TransitionBranch<,>).MakeGenericType(target, typeof(U)), path);
            foreach (var node in nodes) copy.TryAdd(node);
            return copy;
        }

        void IUntypedNode.ListWrites(object target, ICollection<(object Instance, PropertyInfo Member)> writes) {
            var reached = Reach(target);
            if (reached == null) return;

            foreach (var node in nodes) (node as IUntypedNode)?.ListWrites(reached, writes);
        }

        object IUntypedBranch.Reach(object target) {
            return Reach(target);
        }

        // Read through reflection, so it leaves nothing behind for the transitions running.
        private object Reach(object target) {
            var found = MemberAccess.FindPath(typeof(T), path);
            if (found == null || found[^1].PropertyType != typeof(U)) return null;

            var current = target;
            foreach (var hop in found) {
                current = IsNone(current) ? null : hop.GetValue(current);
            }

            return IsNone(current) ? null : current;
        }

        /// <summary>
        /// Adds a property written through <paramref name="member"/> of the object reached, such as <c>group => group.alpha</c>.<br/>
        /// The member is named in code, so the build keeps it without any link.xml entry.
        /// </summary>
        public TransitionBranch<T, U> Add<V>(Expression<Func<U, V>> member, ITransitionModifier<U, V> modifier) {
            nodes.Add(new TransitionProperty<U, V>(MemberAccess.GetName(member), modifier));
            return this;
        }

        /// <summary>
        /// Adds a branch reaching on from the object reached through <paramref name="path"/>, such as <c>page => page.Root.Group</c>, and returns it.
        /// </summary>
        public TransitionBranch<U, W> Branch<W>(Expression<Func<U, W>> path) where W : class {
            var branch = new TransitionBranch<U, W>(MemberAccess.GetPath(path));
            nodes.Add(branch);
            return branch;
        }

        public void Apply(T target, float t) {
            var reached = Follow(target);
            if (reached == null) return;

            for (var i = 0; i < nodes.Count; i++) nodes[i]?.Apply(reached, t);
        }

        private U Follow(T target) {
            var found = Find();
            if (found == null) return null;

            object current = target;
            for (var i = 0; i < found.Length; i++) {
                current = TransitionRestHub.Follow(current, found[i]);
                if (!IsNone(current)) continue;

                TransitionLog.Once(ref isEmptyReported, $"Branch {typeof(T).Name}.{path} reaches nothing; it is skipped.");
                return null;
            }

            return (U)current;
        }

        // The path is looked up on T, as a property's member is, and must end at a property declared as U.
        private PropertyInfo[] Find() {
            if (isFound && path == foundPath) return hops;

            isFound = true;
            foundPath = path;
            hops = MemberAccess.FindPath(typeof(T), path);
            if (hops == null) {
                TransitionLog.Once(ref isReported, $"{typeof(T).Name} has no path \"{path}\" of public properties with a public get; the branch is skipped.");
            } else if (hops[^1].PropertyType != typeof(U)) {
                TransitionLog.Once(ref isReported, $"{typeof(T).Name}.{path} is {hops[^1].PropertyType.Name}, not {typeof(U).Name}; the branch is skipped.");
                hops = null;
            }

            return hops;
        }

        private static bool IsNone(object value) {
            return value == null || value is Object unity && unity == null;
        }
    }
}
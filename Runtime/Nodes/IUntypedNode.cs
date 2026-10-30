using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RiseOn.Transitions {
    /// <summary>
    /// What the Editor reaches on a node without knowing its target type: Change Type carries it to the target's new type,<br/>
    /// and a preview saves and restores what it writes.
    /// </summary>
    internal interface IUntypedNode {
        /// <summary>The member a property writes, or the path a branch follows.</summary>
        string Name { get; }

        /// <summary>
        /// The same node for targets of <paramref name="target"/>, all it holds carried over;<br/>
        /// null when it cannot work on one: <paramref name="target"/> lacks the member or the path, or a modifier is set up for a type it does not derive from.
        /// </summary>
        object Retarget(Type target);

        /// <summary>
        /// Adds each member the node writes on <paramref name="target"/>, as the instance written and its property, those its branches reach included.<br/>
        /// Read through reflection, so it leaves nothing behind for the transitions running.
        /// </summary>
        void ListWrites(object target, ICollection<(object Instance, PropertyInfo Member)> writes);
    }

    /// <summary>
    /// What the Editor reaches on a list of nodes without knowing their target type: that of a target, a branch, or an unbound target.
    /// </summary>
    internal interface IUntypedNodes {
        /// <summary>The type of the objects the nodes write on.</summary>
        Type NodeTarget { get; }

        IEnumerable Nodes { get; }

        /// <summary>Adds <paramref name="node"/> when it is a node of <see cref="NodeTarget"/>, or null for an empty slot; false when not.</summary>
        bool TryAdd(object node);
    }
}
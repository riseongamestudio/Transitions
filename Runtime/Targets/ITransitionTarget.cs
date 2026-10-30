using System;
using RiseOn.Utils;

namespace RiseOn.Transitions {
    /// <summary>
    /// One object of a <see cref="Transition"/> and the nodes written on it.<br/>
    /// The Replace Type of Utils, which copies fields over by path, is left out: the object and the nodes are set up for its type argument.<br/>
    /// The Inspector has a Replace Type of its own instead, offering only the types every node keeps working on.
    /// </summary>
    [HideReplaceType]
    public interface ITransitionTarget {
        string Label { get; }

        /// <summary>
        /// The type the nodes were set up for; the object may be of this type or derive from it.
        /// </summary>
        Type TargetType { get; }

        void Apply(float t);
    }
}
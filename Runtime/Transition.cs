using System;
using System.Collections.Generic;
using RiseOn.Serializables;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// One animation from hidden (<c>t</c> = 0) to shown (<c>t</c> = 1) over some objects, set up in the Inspector or in code.<br/>
    /// <see cref="Apply"/> is a pure function of <c>t</c>, so a clock can run it either way and turn around anywhere.<br/>
    /// The shown state of each member is the one it had before any transition first wrote it, unless its property types one in.
    /// </summary>
    [Serializable]
    public sealed class Transition {
        [SerializeField]
        internal ListSerRef<ITransitionTarget> targets = new();

        public IList<ITransitionTarget> Targets => targets;

        /// <summary>
        /// Adds <paramref name="target"/> to set properties on, such as <c>transition.Add(group).Add(g => g.alpha, modifier)</c>.
        /// </summary>
        public TransitionTarget<T> Add<T>(T target, string label = null) where T : class {
            var entry = new TransitionTarget<T>(target, label);
            targets.Add(entry);
            return entry;
        }

        public void Apply(float t) {
            for (var i = 0; i < targets.Count; i++) targets[i]?.Apply(t);
        }
    }
}
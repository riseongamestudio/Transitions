using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// The properties of one target kept for reuse, set up for a type; it applies to a target of that type or of one derived from it.<br/>
    /// Applying it replaces the properties of the target, which keeps its object and label.
    /// </summary>
    public sealed class TransitionTargetPreset : ScriptableObject {
        [SerializeReference]
        internal ITransitionTarget target;
    }
}
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// A whole transition kept for reuse: its targets as roles, each with a label, a type and properties, but no object.<br/>
    /// Applying it to a transition replaces every target with these roles; drag an object into each.
    /// </summary>
    public sealed class TransitionPreset : ScriptableObject {
        [SerializeField]
        internal Transition transition = new();
    }
}
using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Replaces the value with its source: <c>x' = source</c>.<br/>
    /// The source decides where it ends, so end it at rest yourself, or place this before a modifier that brings it back.
    /// </summary>
    [Serializable, TypeRegistryItem("Override")]
    public sealed class OverrideModifier<T, V> : ITransitionModifier<T, V> where T : class {
        [SerializeReference, Required]
        private ITransitionSource<T, V> source;

        public OverrideModifier() { }

        public OverrideModifier(ITransitionSource<T, V> source) {
            this.source = source;
        }

        public V Apply(V input, float t, T target) {
            return source == null ? input : source.Evaluate(t, target);
        }
    }
}
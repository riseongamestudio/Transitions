using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The same value at every point of the clock.
    /// </summary>
    [Serializable, TypeRegistryItem("Constant", "Value")]
    public sealed class ConstantSource<V> : ITransitionSource<object, V> {
        [SerializeField]
        private V value;

        public ConstantSource() { }

        public ConstantSource(V value) {
            this.value = value;
        }

        public V Evaluate(float t, object target) {
            return value;
        }
    }
}
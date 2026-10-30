using System;
using RiseOn.Serializables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Runs several modifiers in order, each taking what the one before it gave.
    /// </summary>
    [Serializable, TypeRegistryItem("Chain")]
    public sealed class ChainModifier<T, V> : ITransitionModifier<T, V> where T : class {
        [SerializeField]
        private ListSerRef<ITransitionModifier<T, V>> modifiers = new();

        public ChainModifier() { }

        public ChainModifier(params ITransitionModifier<T, V>[] modifiers) {
            foreach (var modifier in modifiers) this.modifiers.Add(modifier);
        }

        public V Apply(V input, float t, T target) {
            for (var i = 0; i < modifiers.Count; i++) {
                if (modifiers[i] != null) input = modifiers[i].Apply(input, t, target);
            }

            return input;
        }
    }
}
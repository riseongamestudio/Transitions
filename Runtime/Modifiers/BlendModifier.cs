using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Blends the value toward its source by a weight: <c>x' = (1 − weight) · x + weight · source</c>; end the weight at zero to end at rest.<br/>
    /// Needs an <see cref="IInterpolation&lt;V&gt;"/> for <typeparamref name="V"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Blend")]
    [RequiresOperation(typeof(IInterpolation<>))]
    public sealed class BlendModifier<T, V> : ITransitionModifier<T, V> where T : class {
        [SerializeReference, Required]
        private ITransitionSource<T, V> source;

        [SerializeReference, Required]
        private ITransitionSource<T, float> weight;

        [NonSerialized] private bool isReported;

        public BlendModifier() { }

        public BlendModifier(ITransitionSource<T, V> source, ITransitionSource<T, float> weight) {
            this.source = source;
            this.weight = weight;
        }

        public V Apply(V input, float t, T target) {
            if (source == null || weight == null) return input;

            var interpolation = Operation<IInterpolation<V>>.Instance;
            if (interpolation == null) {
                TransitionLog.Once(ref isReported, $"No {nameof(IInterpolation<V>)}<{typeof(V).Name}> is implemented; the blend is skipped.");
                return input;
            }

            return interpolation.Apply(input, source.Evaluate(t, target), weight.Evaluate(t, target));
        }
    }
}
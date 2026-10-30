using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Multiplies the value by its source: <c>x' = x · source</c>; end the source at one to end at rest.<br/>
    /// Needs an <see cref="IMultiplication&lt;V&gt;"/> for <typeparamref name="V"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Multiplication")]
    [RequiresOperation(typeof(IMultiplication<>))]
    public sealed class MultiplicationModifier<T, V> : ITransitionModifier<T, V> where T : class {
        [SerializeReference, Required]
        private ITransitionSource<T, V> source;

        [NonSerialized] private bool isReported;

        public MultiplicationModifier() { }

        public MultiplicationModifier(ITransitionSource<T, V> source) {
            this.source = source;
        }

        public V Apply(V input, float t, T target) {
            if (source == null) return input;

            var multiplication = Operation<IMultiplication<V>>.Instance;
            if (multiplication == null) {
                TransitionLog.Once(ref isReported, $"No {nameof(IMultiplication<V>)}<{typeof(V).Name}> is implemented; the multiplication is skipped.");
                return input;
            }

            return multiplication.Apply(input, source.Evaluate(t, target));
        }
    }
}
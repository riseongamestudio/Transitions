using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Adds its source to the value: <c>x' = x + source</c>; end the source at zero to end at rest.<br/>
    /// Needs an <see cref="IAddition&lt;V&gt;"/> for <typeparamref name="V"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Addition")]
    [RequiresOperation(typeof(IAddition<>))]
    public sealed class AdditionModifier<T, V> : ITransitionModifier<T, V> where T : class {
        [SerializeReference, Required]
        private ITransitionSource<T, V> source;

        [NonSerialized] private bool isReported;

        public AdditionModifier() { }

        public AdditionModifier(ITransitionSource<T, V> source) {
            this.source = source;
        }

        public V Apply(V input, float t, T target) {
            if (source == null) return input;

            var addition = Operation<IAddition<V>>.Instance;
            if (addition == null) {
                TransitionLog.Once(ref isReported, $"No {nameof(IAddition<V>)}<{typeof(V).Name}> is implemented; the addition is skipped.");
                return input;
            }

            return addition.Apply(input, source.Evaluate(t, target));
        }
    }
}
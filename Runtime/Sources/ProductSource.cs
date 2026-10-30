using System;
using RiseOn.Serializables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The product of several sources; nothing when empty. Needs an <see cref="IMultiplication&lt;V&gt;"/> for <typeparamref name="V"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Product", "Combine")]
    [RequiresOperation(typeof(IMultiplication<>))]
    public sealed class ProductSource<T, V> : ITransitionSource<T, V> where T : class {
        [SerializeField]
        private ListSerRef<ITransitionSource<T, V>> sources = new();

        [NonSerialized] private bool isReported;

        public ProductSource() { }

        public ProductSource(params ITransitionSource<T, V>[] sources) {
            foreach (var source in sources) this.sources.Add(source);
        }

        public V Evaluate(float t, T target) {
            var multiplication = Operation<IMultiplication<V>>.Instance;
            if (multiplication == null) {
                TransitionLog.Once(ref isReported, $"No {nameof(IMultiplication<V>)}<{typeof(V).Name}> is implemented; the product is the first source alone.");
            }

            var product = default(V);
            var isFirst = true;
            for (var i = 0; i < sources.Count; i++) {
                if (sources[i] == null) continue;

                var value = sources[i].Evaluate(t, target);
                if (isFirst) {
                    product = value;
                    isFirst = false;
                } else if (multiplication != null) {
                    product = multiplication.Apply(product, value);
                }
            }

            return product;
        }
    }
}
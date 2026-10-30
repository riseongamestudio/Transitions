using System;
using RiseOn.Serializables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The sum of several sources; nothing when empty. Needs an <see cref="IAddition&lt;V&gt;"/> for <typeparamref name="V"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Sum", "Combine")]
    [RequiresOperation(typeof(IAddition<>))]
    public sealed class SumSource<T, V> : ITransitionSource<T, V> where T : class {
        [SerializeField]
        private ListSerRef<ITransitionSource<T, V>> sources = new();

        [NonSerialized] private bool isReported;

        public SumSource() { }

        public SumSource(params ITransitionSource<T, V>[] sources) {
            foreach (var source in sources) this.sources.Add(source);
        }

        public V Evaluate(float t, T target) {
            var addition = Operation<IAddition<V>>.Instance;
            if (addition == null) {
                TransitionLog.Once(ref isReported, $"No {nameof(IAddition<V>)}<{typeof(V).Name}> is implemented; the sum is the first source alone.");
            }

            var sum = default(V);
            var isFirst = true;
            for (var i = 0; i < sources.Count; i++) {
                if (sources[i] == null) continue;

                var value = sources[i].Evaluate(t, target);
                if (isFirst) {
                    sum = value;
                    isFirst = false;
                } else if (addition != null) {
                    sum = addition.Apply(sum, value);
                }
            }

            return sum;
        }
    }
}
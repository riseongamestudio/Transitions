using System;
using RiseOn.Serializables;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Builds a struct from one number source per axis, in the order <see cref="ValueAxes"/> registers them (<c>x</c>, <c>y</c>, …);<br/>
    /// an axis with no source stays zero.
    /// </summary>
    [Serializable, TypeRegistryItem("Vector", "Vector")]
    public sealed class VectorSource<T, [Axes] TVector> : ITransitionSource<T, TVector> where T : class where TVector : struct {
        [SerializeField]
        private ListSerRef<ITransitionSource<T, float>> axes = new();

        public VectorSource() { }

        public VectorSource(params ITransitionSource<T, float>[] axes) {
            foreach (var axis in axes) this.axes.Add(axis);
        }

        public TVector Evaluate(float t, T target) {
            var vector = default(TVector);
            var count = Math.Min(axes.Count, ValueAxes.GetNames(typeof(TVector)).Count);
            for (var i = 0; i < count; i++) {
                if (axes[i] != null) vector = ValueAxes<TVector>.Write(vector, i, axes[i].Evaluate(t, target));
            }

            return vector;
        }
    }
}
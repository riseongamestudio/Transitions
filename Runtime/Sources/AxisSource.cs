using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// One axis of a struct source, such as <c>y</c> of a <see cref="Vector2"/>; the axes come from <see cref="ValueAxes"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Axis", "Vector")]
    public sealed class AxisSource<T, [Axes] TVector> : ITransitionSource<T, float> where T : class where TVector : struct {
        [SerializeReference, Required]
        private ITransitionSource<T, TVector> source;

        [SerializeField, ValueDropdown(nameof(ListAxes))]
        private string axis;

        [NonSerialized] private bool isReported;

        public AxisSource() { }

        public AxisSource(ITransitionSource<T, TVector> source, string axis) {
            this.source = source;
            this.axis = axis;
        }

        public float Evaluate(float t, T target) {
            if (source == null) return 0f;

            var index = ValueAxes<TVector>.IndexOf(axis);
            if (index < 0) {
                TransitionLog.Once(ref isReported, $"{typeof(TVector).Name} has no registered axis \"{axis}\"; it reads as zero.");
                return 0f;
            }

            return ValueAxes<TVector>.Read(source.Evaluate(t, target), index);
        }

        private IReadOnlyList<string> ListAxes() {
            return ValueAxes.GetNames(typeof(TVector));
        }
    }
}
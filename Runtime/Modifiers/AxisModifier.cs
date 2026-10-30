using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Runs a number modifier on one axis of a struct, such as <c>y</c> of a <see cref="Vector2"/>, leaving the other axes as they are;<br/>
    /// the axes come from <see cref="ValueAxes"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Axis")]
    public sealed class AxisModifier<T, [Axes] TVector> : ITransitionModifier<T, TVector> where T : class where TVector : struct {
        [SerializeField, ValueDropdown(nameof(ListAxes))]
        private string axis;

        [SerializeReference, Required]
        private ITransitionModifier<T, float> modifier;

        [NonSerialized] private bool isReported;

        public AxisModifier() { }

        public AxisModifier(string axis, ITransitionModifier<T, float> modifier) {
            this.axis = axis;
            this.modifier = modifier;
        }

        public TVector Apply(TVector input, float t, T target) {
            if (modifier == null) return input;

            var index = ValueAxes<TVector>.IndexOf(axis);
            if (index < 0) {
                TransitionLog.Once(ref isReported, $"{typeof(TVector).Name} has no registered axis \"{axis}\"; the modifier is skipped.");
                return input;
            }

            return ValueAxes<TVector>.Write(input, index, modifier.Apply(ValueAxes<TVector>.Read(input, index), t, target));
        }

        private IReadOnlyList<string> ListAxes() {
            return ValueAxes.GetNames(typeof(TVector));
        }
    }
}
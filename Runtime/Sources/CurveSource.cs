using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// A number drawn by a curve over a part of the clock, such as a bump 0 → 1 → 0; the curve's time runs from 0 to 1 over that part.
    /// </summary>
    [Serializable, TypeRegistryItem("Curve", "Value")]
    public sealed class CurveSource : ITransitionSource<object, float> {
        [SerializeField]
        private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField, MinMaxSlider(0f, 1f, true)]
        private Vector2 range = new(0f, 1f);

        public CurveSource() { }

        public CurveSource(AnimationCurve curve, float start = 0f, float end = 1f) {
            this.curve = curve;
            range = new Vector2(start, end);
        }

        public float Evaluate(float t, object target) {
            return curve.Evaluate(TransitionUtils.Remap(t, range.x, range.y));
        }
    }
}
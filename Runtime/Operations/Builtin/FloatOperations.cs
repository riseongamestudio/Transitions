using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Floats.
    /// </summary>
    public sealed class FloatOperations : IAddition<float>, IMultiplication<float>, IInterpolation<float> {
        float IAddition<float>.Apply(float a, float b) {
            return a + b;
        }

        float IMultiplication<float>.Apply(float a, float b) {
            return a * b;
        }

        float IInterpolation<float>.Apply(float a, float b, float t) {
            return Mathf.LerpUnclamped(a, b, t);
        }
    }
}
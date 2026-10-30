using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Vector4s, multiplied per component.
    /// </summary>
    public sealed class Vector4Operations : IAddition<Vector4>, IMultiplication<Vector4>, IInterpolation<Vector4> {
        Vector4 IAddition<Vector4>.Apply(Vector4 a, Vector4 b) {
            return a + b;
        }

        Vector4 IMultiplication<Vector4>.Apply(Vector4 a, Vector4 b) {
            return Vector4.Scale(a, b);
        }

        Vector4 IInterpolation<Vector4>.Apply(Vector4 a, Vector4 b, float t) {
            return Vector4.LerpUnclamped(a, b, t);
        }
    }
}
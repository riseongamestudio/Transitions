using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Vector3s, multiplied per component.
    /// </summary>
    public sealed class Vector3Operations : IAddition<Vector3>, IMultiplication<Vector3>, IInterpolation<Vector3> {
        Vector3 IAddition<Vector3>.Apply(Vector3 a, Vector3 b) {
            return a + b;
        }

        Vector3 IMultiplication<Vector3>.Apply(Vector3 a, Vector3 b) {
            return Vector3.Scale(a, b);
        }

        Vector3 IInterpolation<Vector3>.Apply(Vector3 a, Vector3 b, float t) {
            return Vector3.LerpUnclamped(a, b, t);
        }
    }
}
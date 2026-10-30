using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Vector2s, multiplied per component.
    /// </summary>
    public sealed class Vector2Operations : IAddition<Vector2>, IMultiplication<Vector2>, IInterpolation<Vector2> {
        Vector2 IAddition<Vector2>.Apply(Vector2 a, Vector2 b) {
            return a + b;
        }

        Vector2 IMultiplication<Vector2>.Apply(Vector2 a, Vector2 b) {
            return Vector2.Scale(a, b);
        }

        Vector2 IInterpolation<Vector2>.Apply(Vector2 a, Vector2 b, float t) {
            return Vector2.LerpUnclamped(a, b, t);
        }
    }
}
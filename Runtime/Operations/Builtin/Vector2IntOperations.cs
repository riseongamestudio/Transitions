using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Vector2Ints, multiplied per component; interpolation rounds to the nearest.
    /// </summary>
    public sealed class Vector2IntOperations : IAddition<Vector2Int>, IMultiplication<Vector2Int>, IInterpolation<Vector2Int> {
        Vector2Int IAddition<Vector2Int>.Apply(Vector2Int a, Vector2Int b) {
            return a + b;
        }

        Vector2Int IMultiplication<Vector2Int>.Apply(Vector2Int a, Vector2Int b) {
            return Vector2Int.Scale(a, b);
        }

        Vector2Int IInterpolation<Vector2Int>.Apply(Vector2Int a, Vector2Int b, float t) {
            return new(Mathf.RoundToInt(Mathf.LerpUnclamped(a.x, b.x, t)), Mathf.RoundToInt(Mathf.LerpUnclamped(a.y, b.y, t)));
        }
    }
}
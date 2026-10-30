using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Vector3Ints, multiplied per component; interpolation rounds to the nearest.
    /// </summary>
    public sealed class Vector3IntOperations : IAddition<Vector3Int>, IMultiplication<Vector3Int>, IInterpolation<Vector3Int> {
        Vector3Int IAddition<Vector3Int>.Apply(Vector3Int a, Vector3Int b) {
            return a + b;
        }

        Vector3Int IMultiplication<Vector3Int>.Apply(Vector3Int a, Vector3Int b) {
            return Vector3Int.Scale(a, b);
        }

        Vector3Int IInterpolation<Vector3Int>.Apply(Vector3Int a, Vector3Int b, float t) {
            return new(Mathf.RoundToInt(Mathf.LerpUnclamped(a.x, b.x, t)), Mathf.RoundToInt(Mathf.LerpUnclamped(a.y, b.y, t)), Mathf.RoundToInt(Mathf.LerpUnclamped(a.z, b.z, t)));
        }
    }
}
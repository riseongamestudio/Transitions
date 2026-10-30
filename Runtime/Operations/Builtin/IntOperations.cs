using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Ints; interpolation rounds to the nearest.
    /// </summary>
    public sealed class IntOperations : IAddition<int>, IMultiplication<int>, IInterpolation<int> {
        int IAddition<int>.Apply(int a, int b) {
            return a + b;
        }

        int IMultiplication<int>.Apply(int a, int b) {
            return a * b;
        }

        int IInterpolation<int>.Apply(int a, int b, float t) {
            return Mathf.RoundToInt(Mathf.LerpUnclamped(a, b, t));
        }
    }
}
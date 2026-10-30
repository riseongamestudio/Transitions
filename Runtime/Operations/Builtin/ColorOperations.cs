using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Colors, per channel.
    /// </summary>
    public sealed class ColorOperations : IAddition<Color>, IMultiplication<Color>, IInterpolation<Color> {
        Color IAddition<Color>.Apply(Color a, Color b) {
            return a + b;
        }

        Color IMultiplication<Color>.Apply(Color a, Color b) {
            return a * b;
        }

        Color IInterpolation<Color>.Apply(Color a, Color b, float t) {
            return Color.LerpUnclamped(a, b, t);
        }
    }
}
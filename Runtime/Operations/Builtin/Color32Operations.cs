using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Color32s, per channel, clamped to 0-255.
    /// </summary>
    public sealed class Color32Operations : IAddition<Color32>, IMultiplication<Color32>, IInterpolation<Color32> {
        Color32 IAddition<Color32>.Apply(Color32 a, Color32 b) {
            return (Color)a + (Color)b;
        }

        Color32 IMultiplication<Color32>.Apply(Color32 a, Color32 b) {
            return (Color)a * (Color)b;
        }

        Color32 IInterpolation<Color32>.Apply(Color32 a, Color32 b, float t) {
            return Color32.LerpUnclamped(a, b, t);
        }
    }
}
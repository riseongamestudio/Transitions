using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on RectInts, per x, y, width and height; interpolation rounds to the nearest.
    /// </summary>
    public sealed class RectIntOperations : IAddition<RectInt>, IMultiplication<RectInt>, IInterpolation<RectInt> {
        RectInt IAddition<RectInt>.Apply(RectInt a, RectInt b) {
            return new(a.x + b.x, a.y + b.y, a.width + b.width, a.height + b.height);
        }

        RectInt IMultiplication<RectInt>.Apply(RectInt a, RectInt b) {
            return new(a.x * b.x, a.y * b.y, a.width * b.width, a.height * b.height);
        }

        RectInt IInterpolation<RectInt>.Apply(RectInt a, RectInt b, float t) {
            return new(Round(a.x, b.x, t), Round(a.y, b.y, t), Round(a.width, b.width, t), Round(a.height, b.height, t));

            static int Round(int from, int to, float t) {
                return Mathf.RoundToInt(Mathf.LerpUnclamped(from, to, t));
            }
        }
    }
}
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Rects, per x, y, width and height.
    /// </summary>
    public sealed class RectOperations : IAddition<Rect>, IMultiplication<Rect>, IInterpolation<Rect> {
        Rect IAddition<Rect>.Apply(Rect a, Rect b) {
            return new(a.x + b.x, a.y + b.y, a.width + b.width, a.height + b.height);
        }

        Rect IMultiplication<Rect>.Apply(Rect a, Rect b) {
            return new(a.x * b.x, a.y * b.y, a.width * b.width, a.height * b.height);
        }

        Rect IInterpolation<Rect>.Apply(Rect a, Rect b, float t) {
            return new(Mathf.LerpUnclamped(a.x, b.x, t), Mathf.LerpUnclamped(a.y, b.y, t), Mathf.LerpUnclamped(a.width, b.width, t), Mathf.LerpUnclamped(a.height, b.height, t));
        }
    }
}
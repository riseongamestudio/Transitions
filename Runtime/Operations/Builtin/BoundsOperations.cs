using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Bounds, per center and size.
    /// </summary>
    public sealed class BoundsOperations : IAddition<Bounds>, IMultiplication<Bounds>, IInterpolation<Bounds> {
        Bounds IAddition<Bounds>.Apply(Bounds a, Bounds b) {
            return new(a.center + b.center, a.size + b.size);
        }

        Bounds IMultiplication<Bounds>.Apply(Bounds a, Bounds b) {
            return new(Vector3.Scale(a.center, b.center), Vector3.Scale(a.size, b.size));
        }

        Bounds IInterpolation<Bounds>.Apply(Bounds a, Bounds b, float t) {
            return new(Vector3.LerpUnclamped(a.center, b.center, t), Vector3.LerpUnclamped(a.size, b.size, t));
        }
    }
}
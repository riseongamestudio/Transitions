using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Quaternions: multiplication composes rotations, interpolation is spherical; there is no addition.
    /// </summary>
    public sealed class QuaternionOperations : IMultiplication<Quaternion>, IInterpolation<Quaternion> {
        Quaternion IMultiplication<Quaternion>.Apply(Quaternion a, Quaternion b) {
            return a * b;
        }

        Quaternion IInterpolation<Quaternion>.Apply(Quaternion a, Quaternion b, float t) {
            return Quaternion.SlerpUnclamped(a, b, t);
        }
    }
}
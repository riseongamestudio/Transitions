using System;

namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Longs; interpolation rounds to the nearest.
    /// </summary>
    public sealed class LongOperations : IAddition<long>, IMultiplication<long>, IInterpolation<long> {
        long IAddition<long>.Apply(long a, long b) {
            return a + b;
        }

        long IMultiplication<long>.Apply(long a, long b) {
            return a * b;
        }

        long IInterpolation<long>.Apply(long a, long b, float t) {
            return (long)Math.Round(a + (b - a) * (double)t);
        }
    }
}
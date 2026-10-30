namespace RiseOn.Transitions {
    /// <summary>
    /// The pack's operations on Doubles.
    /// </summary>
    public sealed class DoubleOperations : IAddition<double>, IMultiplication<double>, IInterpolation<double> {
        double IAddition<double>.Apply(double a, double b) {
            return a + b;
        }

        double IMultiplication<double>.Apply(double a, double b) {
            return a * b;
        }

        double IInterpolation<double>.Apply(double a, double b, float t) {
            return a + (b - a) * t;
        }
    }
}
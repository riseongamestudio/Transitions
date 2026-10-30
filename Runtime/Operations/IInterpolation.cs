namespace RiseOn.Transitions {
    /// <summary>
    /// Goes from <c>a</c> at 0 to <c>b</c> at 1 between two values of <typeparamref name="V"/>.
    /// </summary>
    public interface IInterpolation<V> : IOperation {
        V Apply(V a, V b, float t);
    }
}
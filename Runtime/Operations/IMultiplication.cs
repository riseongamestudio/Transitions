namespace RiseOn.Transitions {
    /// <summary>
    /// Multiplies two values of <typeparamref name="V"/>; one is its neutral value. Vectors and colors multiply per component.
    /// </summary>
    public interface IMultiplication<V> : IOperation {
        V Apply(V a, V b);
    }
}
namespace RiseOn.Transitions {
    /// <summary>
    /// Adds two values of <typeparamref name="V"/>; zero is its neutral value.
    /// </summary>
    public interface IAddition<V> : IOperation {
        V Apply(V a, V b);
    }
}
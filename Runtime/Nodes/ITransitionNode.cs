namespace RiseOn.Transitions {
    /// <summary>
    /// One part of what a transition writes on an object of <typeparamref name="T"/>:<br/>
    /// a property writing one of its members, or a branch reaching another object through its members and writing on that one.
    /// </summary>
    public interface ITransitionNode<in T> {
        void Apply(T target, float t);
    }
}
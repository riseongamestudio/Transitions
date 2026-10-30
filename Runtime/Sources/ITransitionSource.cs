namespace RiseOn.Transitions {
    /// <summary>
    /// A value that changes with the clock and takes no input, such as a tween or a measured size: what modifiers work with.<br/>
    /// It must be a pure function of <c>t</c> and <c>target</c>, and write nothing.
    /// </summary>
    /// <typeparam name="T">What it reads off the target; declare the widest type that still works, <see cref="object"/> when it reads nothing.</typeparam>
    [TransitionBlock]
    public interface ITransitionSource<in T, out V> {
        V Evaluate(float t, T target);
    }
}
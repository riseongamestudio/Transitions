namespace RiseOn.Transitions {
    /// <summary>
    /// Changes a value at a point on the clock: a property hands its rest value in, and writes back what comes out.<br/>
    /// It must be a pure function of its arguments, write nothing, and hand <c>input</c> back unchanged at <c>t</c> = 1<br/>
    /// so the target ends exactly at rest.
    /// </summary>
    /// <typeparam name="T">What it reads off the target; declare the widest type that still works, <see cref="object"/> when it reads nothing.</typeparam>
    [TransitionBlock]
    public interface ITransitionModifier<in T, V> {
        V Apply(V input, float t, T target);
    }
}
namespace RiseOn.Transitions {
    /// <summary>
    /// Reads and writes one property of one object; the object is passed on each call, so an access that does not hold it can be kept for it.
    /// </summary>
    internal interface IMemberAccess<T> {
        T Get(object target);
        void Set(object target, T value);
    }
}
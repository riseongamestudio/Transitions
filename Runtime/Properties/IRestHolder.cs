namespace RiseOn.Transitions {
    /// <summary>
    /// The rest value of a property of values <typeparamref name="V"/>, whatever its target type, for <see cref="IUntypedNode.Retarget"/> to carry it over.
    /// </summary>
    internal interface IRestHolder<V> {
        bool CustomRest { get; set; }

        V Rest { get; set; }
    }
}
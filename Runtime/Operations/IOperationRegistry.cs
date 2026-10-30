namespace RiseOn.Transitions {
    /// <summary>
    /// Hands every operation implemented in one assembly to <see cref="Operation&lt;TOperation&gt;"/>.<br/>
    /// The pack's source generator writes one per assembly and names it in <see cref="OperationRegistryAttribute"/>; there is no need to write one by hand.
    /// </summary>
    public interface IOperationRegistry {
        void Register(OperationRegistration registration);
    }
}
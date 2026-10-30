namespace RiseOn.Transitions {
    /// <summary>
    /// The implementation of <typeparamref name="TOperation"/> in use, such as <c>Operation&lt;IAddition&lt;Vector2&gt;&gt;.Instance</c>, or null when there is none.<br/>
    /// It is chosen once, on first use, among every implementation of the loaded assemblies:<br/>
    /// an assembly that references another wins over it, so a game overrides the pack by implementing the same operation.
    /// </summary>
    public static class Operation<TOperation> where TOperation : class, IOperation {
        private static TOperation instance;
        private static bool isResolved;

        public static TOperation Instance {
            get {
                if (isResolved) return instance;

                instance = OperationRegistry.Resolve(typeof(TOperation)) as TOperation;
                isResolved = true;
                return instance;
            }
        }
    }
}
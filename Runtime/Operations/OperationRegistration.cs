using System.Collections.Generic;

namespace RiseOn.Transitions {
    /// <summary>
    /// What one <see cref="IOperationRegistry"/> fills: the operation instances of its assembly.
    /// </summary>
    public sealed class OperationRegistration {
        internal readonly List<IOperation> operations = new();

        internal OperationRegistration() { }

        public void Add(IOperation operation) {
            if (operation != null) operations.Add(operation);
        }
    }
}
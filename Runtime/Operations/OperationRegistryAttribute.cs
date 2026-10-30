using System;

namespace RiseOn.Transitions {
    /// <summary>
    /// Marks an assembly whose operations are handed over by <see cref="Registry"/>, so they are found without scanning the assembly.<br/>
    /// The pack's source generator adds it to every assembly that references the pack.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class OperationRegistryAttribute : Attribute {
        public OperationRegistryAttribute(Type registry) {
            Registry = registry;
        }

        /// <summary>
        /// A class implementing <see cref="IOperationRegistry"/> with a parameterless constructor.
        /// </summary>
        public Type Registry { get; }
    }
}
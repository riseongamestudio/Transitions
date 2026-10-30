using System;

namespace RiseOn.Transitions {
    /// <summary>
    /// Declares an operation a modifier or source needs on its value type, such as <c>[RequiresOperation(typeof(IAddition&lt;&gt;))]</c> on a block of <c>V</c><br/>
    /// that adds values of <c>V</c>. The Inspector grays the block out for a value type with no implementation of that operation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequiresOperationAttribute : Attribute {
        /// <param name="operation">The open contract, such as <c>typeof(IAddition&lt;&gt;)</c>; it is closed on the block's value type.</param>
        public RequiresOperationAttribute(Type operation) {
            Operation = operation;
        }

        public Type Operation { get; }
    }
}
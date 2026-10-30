using System;

namespace RiseOn.Transitions {
    /// <summary>
    /// Marks a type parameter that only takes structs with axes registered in <see cref="ValueAxes"/>, so the Inspector offers no other.
    /// </summary>
    [AttributeUsage(AttributeTargets.GenericParameter)]
    internal sealed class AxesAttribute : Attribute { }
}
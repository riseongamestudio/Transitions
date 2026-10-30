using UnityEngine.Scripting;

namespace RiseOn.Transitions {
    /// <summary>
    /// Base of every operation contract, such as <see cref="IAddition&lt;V&gt;"/>; an operation is a pure function on values of one type.<br/>
    /// The linker keeps every class that implements it, in any assembly of the build, so implementations need no <see cref="PreserveAttribute"/>.
    /// </summary>
    [RequireImplementors]
    public interface IOperation { }
}
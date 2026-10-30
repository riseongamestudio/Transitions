using System.Collections.Generic;
using System.Reflection;

namespace RiseOn.Transitions {
    /// <summary>
    /// What the Editor reaches on a target without knowing its type: presets name the role, clear the object and copy the nodes,<br/>
    /// a change of type carries the object over, and a preview saves and restores the members written.
    /// </summary>
    internal interface IUntypedTarget : IUntypedNodes {
        string Label { get; set; }

        UnityEngine.Object Object { get; set; }

        System.Type TargetType { get; }

        /// <summary>Adds each member the nodes write on the object, those its branches reach included; none without an object.</summary>
        void ListWrites(ICollection<(object Instance, PropertyInfo Member)> writes);
    }
}
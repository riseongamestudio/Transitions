using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The size of the rect of the target's parent, measured live.
    /// </summary>
    [Serializable, TypeRegistryItem("Parent Size", "Measure")]
    public sealed class ParentSizeSource : ITransitionSource<Component, Vector2> {
        public Vector2 Evaluate(float t, Component target) {
            return target.transform.parent is RectTransform parent ? parent.rect.size : Vector2.zero;
        }
    }
}
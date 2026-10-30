using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The size of the target's own rect, measured live.
    /// </summary>
    [Serializable, TypeRegistryItem("Self Size", "Measure")]
    public sealed class SelfSizeSource : ITransitionSource<Component, Vector2> {
        public Vector2 Evaluate(float t, Component target) {
            return target.transform is RectTransform rect ? rect.rect.size : Vector2.zero;
        }
    }
}
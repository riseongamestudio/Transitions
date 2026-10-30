using System;
using RiseOn.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// How far a UI element moves away in a direction, for an <c>anchoredPosition</c> offset: <c>Addition ← Tween (Offset → 0)</c> slides it in.<br/>
    /// Distances are measured on the rects of the target and its parent, so the target must sit on a <see cref="RectTransform"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Offset", "Measure")]
    public sealed class OffsetSource : ITransitionSource<Component, Vector2> {
        /// <summary>
        /// <see cref="Self"/>: the target's own size along the direction. <see cref="Parent"/>: the parent's size.<br/>
        /// <see cref="OutOfParent"/>: far enough to leave the parent wholly from anywhere inside it, the parent's size plus the target's own.<br/>
        /// <see cref="Units"/>: a fixed number of units.
        /// </summary>
        public enum Distance {
            Self
          , Parent
          , OutOfParent
          , Units
        }

        [SerializeField]
        private DirectionExt from = new(Direction.Right);

        [SerializeField]
        private Distance distance = Distance.OutOfParent;

        [SerializeField, ShowIf(nameof(distance), Distance.Units), Indent]
        private float units = 100f;

        [NonSerialized] private bool isReported;

        public OffsetSource() { }

        public OffsetSource(Direction from, Distance distance = Distance.OutOfParent, float units = 100f) {
            this.from = new DirectionExt(from);
            this.distance = distance;
            this.units = units;
        }

        public Vector2 Evaluate(float t, Component target) {
            if (target.transform is not RectTransform rect) {
                TransitionLog.Once(ref isReported, $"{target.name} is not on a {nameof(RectTransform)}; its offset is zero.");
                return Vector2.zero;
            }

            // Scaled so its longer axis is 1: a size times it reaches the edge of that size at any angle.
            var unit = from.ToVector2();
            var direction = unit / Mathf.Max(Mathf.Abs(unit.x), Mathf.Abs(unit.y));
            var parent = rect.parent as RectTransform;
            var parentSize = parent ? parent.rect.size : Vector2.zero;

            return distance switch {
                Distance.Self => direction * rect.rect.size,
                Distance.Parent => direction * parentSize,
                Distance.Units => unit * units,
                _ => direction * Stretch(direction, parentSize + rect.rect.size)
            };

            // The shortest stretch along the direction that covers the span on every axis the direction moves along.
            static float Stretch(Vector2 direction, Vector2 span) {
                var stretch = float.PositiveInfinity;
                if (direction.x != 0f) stretch = Mathf.Min(stretch, span.x / Mathf.Abs(direction.x));
                if (direction.y != 0f) stretch = Mathf.Min(stretch, span.y / Mathf.Abs(direction.y));
                return float.IsPositiveInfinity(stretch) ? 0f : stretch;
            }
        }
    }
}
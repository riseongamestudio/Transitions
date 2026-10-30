using System;
using LitMotion;
using RiseOn.Utils.LitMotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Runs another source on its own clock: the part <see cref="range"/> of the clock stretched over 0 to 1,<br/>
    /// then repeated, mirrored (there and back), eased and reversed, in that order.
    /// </summary>
    [Serializable, TypeRegistryItem("Retiming", "Combine")]
    public sealed class RetimingSource<T, V> : ITransitionSource<T, V> where T : class {
        [SerializeReference, Required]
        private ITransitionSource<T, V> source;

        [SerializeField, MinMaxSlider(0f, 1f, true)]
        private Vector2 range = new(0f, 1f);

        [SerializeField, Min(1)]
        private int repeat = 1;

        [SerializeField]
        private bool mirror;

        [SerializeField]
        private EaseExt ease = new(Ease.Linear);

        [SerializeField]
        private bool reverse;

        public RetimingSource() { }

        public RetimingSource(ITransitionSource<T, V> source) {
            this.source = source;
        }

        public V Evaluate(float t, T target) {
            if (source == null) return default;

            var local = TransitionUtils.Remap(t, range.x, range.y);
            if (repeat > 1) local = TransitionUtils.Repeat(local, repeat);
            if (mirror) local = TransitionUtils.Mirror(local);
            local = ease.Evaluate(local);
            if (reverse) local = 1f - local;

            return source.Evaluate(local, target);
        }
    }
}
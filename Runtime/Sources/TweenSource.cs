using System;
using LitMotion;
using RiseOn.Utils.LitMotion;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Goes from <see cref="From"/> to <see cref="To"/> over a part of the clock, eased, holding <see cref="From"/> before it and <see cref="To"/> after it.<br/>
    /// Needs an <see cref="IInterpolation&lt;V&gt;"/> for <typeparamref name="V"/>.
    /// </summary>
    [Serializable, TypeRegistryItem("Tween", "Value")]
    [RequiresOperation(typeof(IInterpolation<>))]
    public sealed class TweenSource<T, V> : ITransitionSource<T, V> where T : class {
        [SerializeReference, Required]
        private ITransitionSource<T, V> from = new ConstantSource<V>();

        [SerializeReference, Required]
        private ITransitionSource<T, V> to = new ConstantSource<V>();

        [SerializeField, MinMaxSlider(0f, 1f, true)]
        private Vector2 range = new(0f, 1f);

        [SerializeField]
        private EaseExt ease = new(Ease.OutCubic);

        [NonSerialized] private bool isReported;

        public TweenSource() { }

        public TweenSource(ITransitionSource<T, V> from, ITransitionSource<T, V> to, Ease ease = Ease.OutCubic, float start = 0f, float end = 1f) {
            this.from = from;
            this.to = to;
            this.ease = new EaseExt(ease);
            range = new Vector2(start, end);
        }

        public ITransitionSource<T, V> From => from;

        public ITransitionSource<T, V> To => to;

        public V Evaluate(float t, T target) {
            var end = to == null ? default : to.Evaluate(t, target);
            var interpolation = Operation<IInterpolation<V>>.Instance;
            if (interpolation == null) {
                TransitionLog.Once(ref isReported, $"No {nameof(IInterpolation<V>)}<{typeof(V).Name}> is implemented; the tween holds its end value.");
                return end;
            }

            var start = from == null ? default : from.Evaluate(t, target);
            return interpolation.Apply(start, end, ease.Evaluate(TransitionUtils.Remap(t, range.x, range.y)));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// One public property of the target (<see cref="Member"/>), its rest value, and the modifier that changes it along the clock.<br/>
    /// On apply it hands the rest value to the modifier and writes what comes out; it is the only place that writes the member.<br/>
    /// The rest value is the object's own: the value the member had before any transition first wrote it, kept until the object is destroyed;<br/>
    /// or, with <see cref="CustomRest"/>, the one typed in.
    /// </summary>
    [Serializable]
    public sealed class TransitionProperty<T, V> : ITransitionNode<T>, IUntypedNode, IRestHolder<V> where T : class {
        // Picked when the property is added, and named by the Inspector's header.
        [SerializeField, HideInInspector]
        internal string member;

        [Tooltip("Type the rest value in instead of taking the object's own: the value the member had before any transition first wrote it.")]
        [SerializeField]
        internal bool customRest;

        [SerializeField, ShowIf(nameof(customRest)), Indent]
        internal V rest;

        [SerializeReference, Required]
        private ITransitionModifier<T, V> modifier;

        [NonSerialized] private string foundMember;
        [NonSerialized] private PropertyInfo found;
        [NonSerialized] private bool isFound;
        [NonSerialized] private bool isReported;

        public TransitionProperty() { }

        public TransitionProperty(string member, ITransitionModifier<T, V> modifier) {
            this.member = member;
            this.modifier = modifier;
        }

        public string Member => member;

        public ITransitionModifier<T, V> Modifier {
            get => modifier;
            set => modifier = value;
        }

        /// <summary>
        /// Whether <see cref="Rest"/> is used as typed; when false, the rest value is the object's own.
        /// </summary>
        public bool CustomRest {
            get => customRest;
            set => customRest = value;
        }

        public V Rest {
            get => rest;
            set => rest = value;
        }

        string IUntypedNode.Name => member;

        // The modifier is kept as it is: one set up for T works on any type deriving from T, its target type being contravariant.
        object IUntypedNode.Retarget(Type target) {
            var property = MemberAccess.Find(target, member);
            if (property == null || property.PropertyType != typeof(V)) return null;

            var modifierType = typeof(ITransitionModifier<,>).MakeGenericType(target, typeof(V));
            if (modifier != null && !modifierType.IsInstanceOfType(modifier)) return null;

            var copy = Activator.CreateInstance(typeof(TransitionProperty<,>).MakeGenericType(target, typeof(V)), member, modifier);
            var rested = (IRestHolder<V>)copy;
            rested.CustomRest = customRest;
            rested.Rest = rest;
            return copy;
        }

        void IUntypedNode.ListWrites(object target, ICollection<(object Instance, PropertyInfo Member)> writes) {
            var property = MemberAccess.Find(typeof(T), member);
            if (property != null && property.PropertyType == typeof(V)) writes.Add((target, property));
        }

        // The slot is made on the first write even with a rest typed in, as it also keeps the access bound to the object.
        public void Apply(T target, float t) {
            var property = Find();
            if (property == null) return;

            var slot = TransitionRestHub.GetProperty<V>(target, property);
            var start = customRest ? rest : slot.Rest;
            slot.Access.Set(target, modifier == null ? start : modifier.Apply(start, t, target));
        }

        // The member is looked up on T, not on the object's own type, so it is the one listed in the Inspector and kept by the linker.
        private PropertyInfo Find() {
            if (isFound && member == foundMember) return found;

            isFound = true;
            foundMember = member;
            found = MemberAccess.Find(typeof(T), member);
            if (found == null) {
                TransitionLog.Once(ref isReported, $"{typeof(T).Name} has no public property \"{member}\" with a public get and set; it is skipped.");
            } else if (found.PropertyType != typeof(V)) {
                TransitionLog.Once(ref isReported, $"{typeof(T).Name}.{member} is {found.PropertyType.Name}, not {typeof(V).Name}; it is skipped.");
                found = null;
            }

            return found;
        }
    }
}
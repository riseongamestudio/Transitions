using System;
using System.Reflection;

namespace RiseOn.Transitions {
    /// <summary>
    /// Reads and writes one property of one Unity object through delegates bound to it once, so each call costs a plain call with no allocation.<br/>
    /// The delegates hold the object, which is fine for a Unity object: it goes when it is destroyed, whoever holds it.
    /// </summary>
    internal sealed class PropertyAccess<T> : IMemberAccess<T> {
        private readonly Func<T> getter;
        private readonly Action<T> setter;

        public PropertyAccess(object target, PropertyInfo property) {
            getter = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), target, property.GetGetMethod());
            setter = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), target, property.GetSetMethod());
        }

        // The target is the one the delegates are bound to.
        public T Get(object target) {
            return getter();
        }

        public void Set(object target, T value) {
            setter(value);
        }
    }
}
using System;
using System.Reflection;

namespace RiseOn.Transitions {
    /// <summary>
    /// Reads and writes one property of an object that is not a Unity object through delegates taking the object on each call, holding nothing of it, so the object can be collected while its rest value is kept; each call costs a plain call with no allocation.
    /// </summary>
    internal sealed class OpenAccess<T, V> : IMemberAccess<V> where T : class {
        private readonly Func<T, V> getter;
        private readonly Action<T, V> setter;

        public OpenAccess(PropertyInfo property) {
            getter = (Func<T, V>)Delegate.CreateDelegate(typeof(Func<T, V>), property.GetGetMethod());
            setter = (Action<T, V>)Delegate.CreateDelegate(typeof(Action<T, V>), property.GetSetMethod());
        }

        public V Get(object target) {
            return getter((T)target);
        }

        public void Set(object target, V value) {
            setter((T)target, value);
        }
    }
}
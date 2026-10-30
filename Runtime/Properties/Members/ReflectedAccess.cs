using System.Reflection;

namespace RiseOn.Transitions {
    /// <summary>
    /// Reads and writes one property of an object that is not a Unity object through reflection, holding nothing of the object,<br/>
    /// so the object can be collected while its rest value is kept. A struct value is boxed on each call: such objects are rare.
    /// </summary>
    internal sealed class ReflectedAccess<T> : IMemberAccess<T> {
        private readonly PropertyInfo property;

        public ReflectedAccess(PropertyInfo property) {
            this.property = property;
        }

        public T Get(object target) {
            return (T)property.GetValue(target);
        }

        public void Set(object target, T value) {
            property.SetValue(target, value);
        }
    }
}
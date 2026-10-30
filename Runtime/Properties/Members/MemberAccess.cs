using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.Scripting;

namespace RiseOn.Transitions {
    /// <summary>
    /// Finds the public properties transitions reach by name, and binds their access to one object:<br/>
    /// those a property writes, with a public get and set, and those a branch follows, with a public get and an object as value.<br/>
    /// A property hidden with <c>new</c> resolves to the most derived one, an override of only one accessor to the property it overrides,<br/>
    /// and an interface also searches the interfaces it extends.
    /// </summary>
    internal static class MemberAccess {
        private const BindingFlags PROPERTY_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        /// <summary>The separator of the members of a path, such as <c>Root.Group</c>.</summary>
        public const char PATH_SEPARATOR = '.';

        /// <summary>
        /// The property of <paramref name="type"/> named <paramref name="name"/>, or null when there is none with a public get and set.
        /// </summary>
        public static PropertyInfo Find(Type type, string name) {
            return Find(List(type), name);
        }

        /// <summary>
        /// The property of <paramref name="type"/> named <paramref name="name"/> a branch can follow, or null when there is none.
        /// </summary>
        public static PropertyInfo FindReadable(Type type, string name) {
            return Find(ListReadable(type), name);
        }

        /// <summary>
        /// The properties a branch follows from <paramref name="type"/> along <paramref name="path"/>, one per member, or null when one is missing.
        /// </summary>
        public static PropertyInfo[] FindPath(Type type, string path) {
            if (string.IsNullOrEmpty(path)) return null;

            var names = path.Split(PATH_SEPARATOR);
            var hops = new PropertyInfo[names.Length];
            for (var i = 0; i < names.Length; i++) {
                hops[i] = FindReadable(type, names[i]);
                if (hops[i] == null) return null;

                type = hops[i].PropertyType;
            }

            return hops;
        }

        /// <summary>
        /// The access to <paramref name="property"/> on <paramref name="target"/>: delegates bound to a Unity object,<br/>
        /// reflection holding nothing of any other object, so it can still be collected.
        /// </summary>
        public static IMemberAccess<V> Bind<V>(object target, PropertyInfo property) {
            return target is UnityEngine.Object ? new PropertyAccess<V>(target, property) : new ReflectedAccess<V>(property);
        }

        /// <summary>
        /// Reads <paramref name="property"/>, one a branch follows, on the object passed: through a delegate bound to <paramref name="target"/> once<br/>
        /// when it is a Unity object, through reflection holding nothing of it otherwise.
        /// </summary>
        public static Func<object, object> BindReadable(object target, PropertyInfo property) {
            if (target is not UnityEngine.Object) return property.GetValue;

            var getter = (IGetter)Activator.CreateInstance(typeof(Getter<>).MakeGenericType(property.PropertyType), target, property);
            return getter.Get;
        }

        /// <summary>
        /// Every property of <paramref name="type"/> with a public get and set, one per name.
        /// </summary>
        public static IEnumerable<PropertyInfo> List(Type type) {
            return List(type, IsWritable);
        }

        /// <summary>
        /// Every property of <paramref name="type"/> a branch can follow, one per name:<br/>
        /// a public get and an object as value, not a string, an array or a delegate, which hold nothing to write.
        /// </summary>
        public static IEnumerable<PropertyInfo> ListReadable(Type type) {
            return List(type, IsReadable);
        }

        /// <summary>
        /// The name of the one property <paramref name="member"/> reads on its parameter, such as <c>group => group.alpha</c>.
        /// </summary>
        public static string GetName(LambdaExpression member) {
            var path = GetPath(member);
            if (path.IndexOf(PATH_SEPARATOR) < 0) return path;

            throw new ArgumentException($"Expected one property of the target, such as target => target.alpha, but got {member}; reach another object with a branch.", nameof(member));
        }

        /// <summary>
        /// The path of the properties <paramref name="path"/> reads from its parameter, such as <c>Root.Group</c> for <c>page => page.Root.Group</c>.
        /// </summary>
        public static string GetPath(LambdaExpression path) {
            var names = new List<string>();
            var body = Unwrap(path.Body);
            while (body is MemberExpression { Member: PropertyInfo property } access) {
                names.Add(property.Name);
                body = Unwrap(access.Expression);
            }

            if (names.Count is 0 || body is not ParameterExpression) {
                throw new ArgumentException($"Expected properties of the target, such as target => target.alpha, but got {path}.", nameof(path));
            }

            names.Reverse();
            return string.Join(PATH_SEPARATOR.ToString(), names);

            static Expression Unwrap(Expression expression) {
                return expression is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : expression;
            }
        }

        private static PropertyInfo Find(IEnumerable<PropertyInfo> properties, string name) {
            if (string.IsNullOrEmpty(name)) return null;

            foreach (var property in properties) {
                if (property.Name == name) return property;
            }

            return null;
        }

        private static IEnumerable<PropertyInfo> List(Type type, Func<PropertyInfo, bool> isUsable) {
            var names = new HashSet<string>();
            foreach (var declaring in ListDeclaringTypes(type)) {
                foreach (var property in declaring.GetProperties(PROPERTY_FLAGS)) {
                    if (names.Contains(property.Name)) continue;

                    if (isUsable(property)) {
                        names.Add(property.Name);
                        yield return property;
                    } else if (!IsOverride(property)) {
                        names.Add(property.Name);
                    }
                }
            }

            static IEnumerable<Type> ListDeclaringTypes(Type type) {
                if (type.IsInterface) {
                    yield return type;
                    foreach (var parent in type.GetInterfaces()) yield return parent;
                    yield break;
                }

                for (var current = type; current != null; current = current.BaseType) yield return current;
            }

            static bool IsOverride(PropertyInfo property) {
                var accessor = property.GetGetMethod() ?? property.GetSetMethod();
                return accessor != null && accessor.GetBaseDefinition().DeclaringType != accessor.DeclaringType;
            }
        }

        private static bool IsWritable(PropertyInfo property) {
            return IsPlain(property) && property.GetGetMethod() != null && property.GetSetMethod() != null;
        }

        private static bool IsReadable(PropertyInfo property) {
            if (!IsPlain(property) || property.GetGetMethod() == null) return false;

            var type = property.PropertyType;
            return !type.IsValueType && !type.IsPointer && !type.IsArray && type != typeof(string) && !typeof(Delegate).IsAssignableFrom(type);
        }

        private static bool IsPlain(PropertyInfo property) {
            return property.GetIndexParameters().Length is 0 && !property.IsDefined(typeof(ObsoleteAttribute), true);
        }

        private interface IGetter {
            object Get(object target);
        }

        // Made for the property's own type, so the delegate matches its getter exactly, as PropertyAccess binds its own.
        // Only made through reflection, so the constructor is kept from stripping by hand.
        private sealed class Getter<V> : IGetter where V : class {
            private readonly Func<V> getter;

            [Preserve]
            public Getter(object target, PropertyInfo property) {
                getter = (Func<V>)Delegate.CreateDelegate(typeof(Func<V>), target, property.GetGetMethod());
            }

            // The target is the one the delegate is bound to.
            public object Get(object target) {
                return getter();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.Scripting;

namespace RiseOn.Transitions {
    /// <summary>
    /// Finds the public properties transitions reach by name, and binds their access to one object:<br/>
    /// those a property writes, with a public get and set, and those a branch follows, with a public get and an object as value.<br/>
    /// A name resolves as it does in C# on the type declared: a property hidden with <c>new</c> gives way to the most derived one,<br/>
    /// an override of only one accessor to the property it overrides, and two interfaces declaring the name, neither hiding the other, reach neither.<br/>
    /// What a name alone does not reach, a cast does in C#; here it is named with the type declaring it, as <c>IA/Value</c>.
    /// </summary>
    internal static class MemberAccess {
        private const BindingFlags PROPERTY_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        /// <summary>The separator of the members of a path, such as <c>Root.Group</c>.</summary>
        public const char PATH_SEPARATOR = '.';

        /// <summary>The separator between the type declaring a property and its name, such as <c>IA/Value</c>.</summary>
        public const char DECLARING_SEPARATOR = '/';

        /// <summary>
        /// The property of <paramref name="type"/> named <paramref name="id"/>, or null when not exactly one has a public get and set.
        /// </summary>
        public static PropertyInfo Find(Type type, string id) {
            return Find(type, id, IsWritable);
        }

        /// <summary>
        /// The property of <paramref name="type"/> named <paramref name="id"/> a branch can follow, or null when not exactly one can.
        /// </summary>
        public static PropertyInfo FindReadable(Type type, string id) {
            return Find(type, id, IsReadable);
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
        /// How a node names <paramref name="property"/> on <paramref name="type"/>: its name when that alone reaches it, as in C#, or else with the type declaring it.
        /// </summary>
        public static string GetId(Type type, PropertyInfo property) {
            var reached = Find(type, property.Name, IsPlain);
            if (reached != null && IsSameMember(reached, property)) return property.Name;

            return $"{GetDeclaringName(property.DeclaringType)}{DECLARING_SEPARATOR}{property.Name}";

            static bool IsSameMember(PropertyInfo a, PropertyInfo b) {
                return IsSameMethod(a.GetGetMethod(), b.GetGetMethod()) || IsSameMethod(a.GetSetMethod(), b.GetSetMethod());
            }

            static bool IsSameMethod(MethodInfo a, MethodInfo b) {
                return a != null && b != null && a.GetBaseDefinition() == b.GetBaseDefinition();
            }
        }

        /// <summary>
        /// How a branch names the properties <paramref name="hops"/> it follows from <paramref name="type"/>, such as <c>Root.Group</c>.
        /// </summary>
        public static string GetPathId(Type type, IEnumerable<PropertyInfo> hops) {
            var ids = new List<string>();
            foreach (var hop in hops) {
                ids.Add(GetId(type, hop));
                type = hop.PropertyType;
            }

            return string.Join(PATH_SEPARATOR.ToString(), ids);
        }

        /// <summary>
        /// The method that runs when <paramref name="property"/> is written on an object of <paramref name="type"/>, one for an override and what it overrides,<br/>
        /// and for an interface's property and what implements it: two properties write the same member when they run the same method.
        /// </summary>
        public static MethodInfo FindWriter(Type type, PropertyInfo property) {
            var setter = property.GetSetMethod();
            if (property.DeclaringType.IsInterface) {
                var map = type.GetInterfaceMap(property.DeclaringType);
                setter = map.TargetMethods[Array.IndexOf(map.InterfaceMethods, setter)];
            }

            return setter.GetBaseDefinition();
        }

        /// <summary>
        /// The access to <paramref name="property"/> on <paramref name="target"/>: delegates bound to a Unity object,<br/>
        /// delegates taking the object on each call for any other, holding nothing of it, so it can still be collected.
        /// </summary>
        public static IMemberAccess<V> Bind<T, V>(T target, PropertyInfo property) where T : class {
            return target is UnityEngine.Object ? new PropertyAccess<V>(target, property) : new OpenAccess<T, V>(property);
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
        /// Every property of <paramref name="type"/> with a public get and set, one per name, or per interface declaring it.
        /// </summary>
        public static IEnumerable<PropertyInfo> List(Type type) {
            return List(type, IsWritable);
        }

        /// <summary>
        /// Every property of <paramref name="type"/> a branch can follow, one per name, or per interface declaring it:<br/>
        /// a public get and an object as value, not a string, an array or a delegate, which hold nothing to write.
        /// </summary>
        public static IEnumerable<PropertyInfo> ListReadable(Type type) {
            return List(type, IsReadable);
        }

        /// <summary>
        /// How a node names the one property <paramref name="member"/> reads on its parameter, such as <c>group => group.alpha</c>,<br/>
        /// or <c>IA/Value</c> for <c>target => ((IA)target).Value</c> when <c>Value</c> alone does not reach that one.
        /// </summary>
        public static string GetName(LambdaExpression member) {
            var path = GetPath(member);
            if (path.IndexOf(PATH_SEPARATOR) < 0) return path;

            throw new ArgumentException($"Expected one property of the target, such as target => target.alpha, but got {member}; reach another object with a branch.", nameof(member));
        }

        /// <summary>
        /// How a branch names the properties <paramref name="path"/> reads from its parameter, such as <c>Root.Group</c> for <c>page => page.Root.Group</c>.
        /// </summary>
        public static string GetPath(LambdaExpression path) {
            var names = new List<string>();
            var body = Unwrap(path.Body);
            while (body is MemberExpression { Member: PropertyInfo property } access) {
                body = Unwrap(access.Expression);
                names.Add(body == null ? property.Name : GetId(body.Type, property));
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

        private static PropertyInfo Find(Type type, string id, Func<PropertyInfo, bool> isUsable) {
            if (string.IsNullOrEmpty(id)) return null;

            var separator = id.IndexOf(DECLARING_SEPARATOR);
            var name = id.Substring(separator + 1);
            if (separator >= 0) {
                var declaring = id.Substring(0, separator);
                return ListCastTypes(type)
                    .Where(candidate => GetDeclaringName(candidate) == declaring)
                    .SelectMany(candidate => candidate.GetProperties(PROPERTY_FLAGS))
                    .FirstOrDefault(property => property.Name == name && isUsable(property));
            }

            if (type.IsInterface && List(type, IsPlain).Count(property => property.Name == name) > 1) return null;

            return List(type, isUsable).FirstOrDefault(property => property.Name == name);

            // What a cast reaches from type: itself, its base classes, and its interfaces.
            static IEnumerable<Type> ListCastTypes(Type type) {
                for (var current = type; current != null; current = current.BaseType) yield return current;
                foreach (var contract in type.GetInterfaces()) yield return contract;
            }
        }

        // The short name, its type arguments included: IBadge<Int32>. It holds neither separator, so it fits in a path.
        private static string GetDeclaringName(Type type) {
            if (!type.IsGenericType) return type.Name;

            var name = type.Name.Substring(0, type.Name.IndexOf('`'));
            return $"{name}<{string.Join(",", type.GetGenericArguments().Select(GetDeclaringName))}>";
        }

        private static IEnumerable<PropertyInfo> List(Type type, Func<PropertyInfo, bool> isUsable) {
            return type.IsInterface ? ListOfInterface(type, isUsable) : ListOfClass(type, isUsable);

            // A property is hidden only by an interface extending the one declaring it; two parents apart both stay.
            static IEnumerable<PropertyInfo> ListOfInterface(Type type, Func<PropertyInfo, bool> isUsable) {
                var declarings = type.GetInterfaces().Prepend(type).ToList();
                return declarings
                    .SelectMany(declaring => declaring.GetProperties(PROPERTY_FLAGS))
                    .Where(property => isUsable(property) && !declarings.Any(other => IsHiddenBy(property, other)));
            }

            static bool IsHiddenBy(PropertyInfo property, Type other) {
                return other != property.DeclaringType && property.DeclaringType.IsAssignableFrom(other) && other.GetProperties(PROPERTY_FLAGS).Any(hiding => hiding.Name == property.Name);
            }
        }

        private static IEnumerable<PropertyInfo> ListOfClass(Type type, Func<PropertyInfo, bool> isUsable) {
            var names = new HashSet<string>();
            for (var declaring = type; declaring != null; declaring = declaring.BaseType) {
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Collects every operation implementation once, then picks one per contract for <see cref="Operation&lt;TOperation&gt;"/>.<br/>
    /// An assembly marked with <see cref="OperationRegistryAttribute"/> hands its operations over itself; any other assembly that references the pack is scanned.<br/>
    /// When several assemblies implement one contract, the one that references every other wins;<br/>
    /// when none does, the first by assembly name, then type name, is used and the Editor warns.
    /// </summary>
    internal static class OperationRegistry {
        private static Dictionary<Type, List<IOperation>> candidates;

        public static IOperation Resolve(Type contract) {
            candidates ??= Collect();
            if (!candidates.TryGetValue(contract, out var found)) return null;

            return found.Count is 1 ? found[0] : Arbitrate(contract, found);
        }

        private static Dictionary<Type, List<IOperation>> Collect() {
            var result = new Dictionary<Type, List<IOperation>>();
            var own = typeof(IOperation).Assembly;
            var ownName = own.GetName().Name;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assembly.IsDynamic || (assembly != own && !References(assembly, ownName))) continue;

                var registration = new OperationRegistration();
                var registries = assembly.GetCustomAttributes(typeof(OperationRegistryAttribute), false);
                if (registries.Length > 0) {
                    foreach (OperationRegistryAttribute registry in registries) Register(registry.Registry, registration);
                } else {
                    Scan(assembly, registration);
                }

                foreach (var operation in registration.operations) Index(result, operation);
            }

            return result;

            static bool References(Assembly assembly, string name) {
                foreach (var reference in assembly.GetReferencedAssemblies()) {
                    if (reference.Name == name) return true;
                }

                return false;
            }

            static void Register(Type registry, OperationRegistration registration) {
                try {
                    ((IOperationRegistry)Activator.CreateInstance(registry)).Register(registration);
                } catch (Exception exception) {
                    Debug.LogException(exception);
                }
            }

            // The path for an assembly built without the pack's generator, such as a DLL compiled outside Unity.
            static void Scan(Assembly assembly, OperationRegistration registration) {
                Type[] types;
                try {
                    types = assembly.GetTypes();
                } catch (ReflectionTypeLoadException exception) {
                    types = exception.Types;
                }

                foreach (var type in types) {
                    if (type == null || type.IsAbstract || type.IsInterface || type.ContainsGenericParameters) continue;
                    if (!typeof(IOperation).IsAssignableFrom(type)) continue;
                    if (!type.IsValueType && type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) == null) continue;

                    registration.Add((IOperation)Activator.CreateInstance(type, true));
                }
            }

            static void Index(Dictionary<Type, List<IOperation>> result, IOperation operation) {
                foreach (var contract in operation.GetType().GetInterfaces()) {
                    if (contract == typeof(IOperation) || !typeof(IOperation).IsAssignableFrom(contract)) continue;

                    if (!result.TryGetValue(contract, out var list)) result[contract] = list = new List<IOperation>();
                    list.Add(operation);
                }
            }
        }

        private static IOperation Arbitrate(Type contract, List<IOperation> found) {
            var ordered = found
                .OrderBy(operation => operation.GetType().Assembly.GetName().Name, StringComparer.Ordinal)
                .ThenBy(operation => operation.GetType().FullName, StringComparer.Ordinal)
                .ToList();

            foreach (var candidate in ordered) {
                var assembly = candidate.GetType().Assembly;
                if (found.All(other => other.GetType().Assembly == assembly || Reaches(assembly, other.GetType().Assembly))) return candidate;
            }

            var chosen = ordered[0];
            if (Application.isEditor) {
                var names = string.Join(", ", ordered.Select(operation => $"{operation.GetType().FullName} ({operation.GetType().Assembly.GetName().Name})"));
                var named = contract.IsGenericType
                    ? $"{contract.Name.Substring(0, contract.Name.IndexOf('`'))}<{string.Join(", ", contract.GetGenericArguments().Select(argument => argument.Name))}>"
                    : contract.Name;
                Debug.LogWarning($"{named} is implemented by {names}, and no assembly references the others. Using {chosen.GetType().FullName}. "
                    + "To choose, implement the operation in an assembly that references them all.");
            }

            return chosen;

            // Whether `from` references `to`, directly or through other assemblies.
            static bool Reaches(Assembly from, Assembly to) {
                var target = to.GetName().Name;
                var loaded = AppDomain.CurrentDomain.GetAssemblies().GroupBy(assembly => assembly.GetName().Name).ToDictionary(group => group.Key, group => group.First());
                var visited = new HashSet<string>();
                var pending = new Stack<Assembly>();
                pending.Push(from);

                while (pending.Count > 0) {
                    foreach (var reference in pending.Pop().GetReferencedAssemblies()) {
                        if (reference.Name == target) return true;
                        if (visited.Add(reference.Name) && loaded.TryGetValue(reference.Name, out var next)) pending.Push(next);
                    }
                }

                return false;
            }
        }
    }
}
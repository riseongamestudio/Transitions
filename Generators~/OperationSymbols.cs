using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace RiseOn.Transitions.Generators {
    /// <summary>
    /// Finds operation implementations in a compilation: classes and structs that implement a contract derived from <c>IOperation</c>.
    /// </summary>
    internal static class OperationSymbols {
        public const string OPERATION = "RiseOn.Transitions.IOperation";
        public const string PACK_ASSEMBLY = "RiseOn.Transitions";

        /// <summary>
        /// The contracts <paramref name="type"/> implements, such as <c>IAddition&lt;Vector2&gt;</c>; empty when it is not an implementation.
        /// </summary>
        public static IReadOnlyList<INamedTypeSymbol> ListContracts(INamedTypeSymbol type, INamedTypeSymbol operation) {
            if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.IsAbstract || type.IsStatic) return new INamedTypeSymbol[0];

            return type.AllInterfaces
                .Where(contract => !SymbolEqualityComparer.Default.Equals(contract, operation)
                                   && contract.AllInterfaces.Contains(operation, SymbolEqualityComparer.Default))
                .ToList();
        }

        /// <summary>
        /// Every class and struct declared in <paramref name="assembly"/>, nested ones included.
        /// </summary>
        public static IEnumerable<INamedTypeSymbol> ListTypes(IAssemblySymbol assembly) {
            var pending = new Stack<INamespaceOrTypeSymbol>();
            pending.Push(assembly.GlobalNamespace);

            while (pending.Count > 0) {
                var current = pending.Pop();
                foreach (var member in current.GetMembers()) {
                    if (member is INamespaceSymbol space) {
                        pending.Push(space);
                    } else if (member is INamedTypeSymbol type) {
                        yield return type;
                        pending.Push(type);
                    }
                }
            }
        }

        /// <summary>
        /// Why the generated registry cannot create <paramref name="type"/>, or null when it can.
        /// </summary>
        public static string FindBlocker(INamedTypeSymbol type, Compilation compilation) {
            for (var current = type; current != null; current = current.ContainingType) {
                if (current.TypeParameters.Length > 0) return "it is generic, so there is no single instance to register; implement each closed contract in its own class";
            }

            if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)) return "it is nested in a private or protected type; make it reachable from its assembly";
            if (type.TypeKind is TypeKind.Struct) return null;

            var constructor = type.InstanceConstructors.FirstOrDefault(candidate => candidate.Parameters.Length == 0);
            if (constructor == null) return "it has no parameterless constructor";
            if (!compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)) return "its parameterless constructor is private or protected";

            return null;
        }

        /// <summary>
        /// Whether <paramref name="type"/> and every type it is nested in are public.
        /// </summary>
        public static bool IsPublic(INamedTypeSymbol type) {
            for (var current = type; current != null; current = current.ContainingType) {
                if (current.DeclaredAccessibility is not Accessibility.Public) return false;
            }

            return true;
        }
    }
}
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RiseOn.Transitions.Generators {
    /// <summary>
    /// Checks operation implementations where they are written: errors for what cannot run, warnings for what runs but could surprise.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class OperationAnalyzer : DiagnosticAnalyzer {
        private const string CATEGORY = "RiseOn.Transitions";

        public static readonly DiagnosticDescriptor Duplicate = new(
            "RTR001", "Operation implemented twice in one assembly",
            "{0} implements {1}, and so does {2} in the same assembly; keep one",
            CATEGORY, DiagnosticSeverity.Error, true, customTags: WellKnownDiagnosticTags.CompilationEnd);

        public static readonly DiagnosticDescriptor Blocked = new(
            "RTR002", "Operation cannot be created",
            "{0} implements {1} but cannot be created: {2}",
            CATEGORY, DiagnosticSeverity.Error, true);

        public static readonly DiagnosticDescriptor NotPublic = new(
            "RTR003", "Operation is not public",
            "{0} implements {1} but is not public; an assembly that wants to choose it over another can only forward to it through reflection",
            CATEGORY, DiagnosticSeverity.Warning, true);

        public static readonly DiagnosticDescriptor Ambiguous = new(
            "RTR004", "Operation implemented by assemblies that do not know each other",
            "{0} is implemented by {1}, and none of these assemblies references the others, so the first by name is used; implement {0} in {2} to choose",
            CATEGORY, DiagnosticSeverity.Warning, true, customTags: WellKnownDiagnosticTags.CompilationEnd);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Duplicate, Blocked, NotPublic, Ambiguous);

        public override void Initialize(AnalysisContext context) {
            // Generated code is not analyzed, but RTR004 is reported on the generated registry.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(Start);
        }

        private static void Start(CompilationStartAnalysisContext context) {
            var operation = context.Compilation.GetTypeByMetadataName(OperationSymbols.OPERATION);
            if (operation == null) return;

            var local = new ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

            context.RegisterSymbolAction(symbolContext => {
                var type = (INamedTypeSymbol)symbolContext.Symbol;
                var contracts = OperationSymbols.ListContracts(type, operation);
                if (contracts.Count is 0) return;

                var location = type.Locations.FirstOrDefault();
                var named = string.Join(", ", contracts.Select(Display));
                var blocker = OperationSymbols.FindBlocker(type, symbolContext.Compilation);
                if (blocker != null) {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(Blocked, location, Display(type), named, blocker));
                    return;
                }

                if (!OperationSymbols.IsPublic(type)) symbolContext.ReportDiagnostic(Diagnostic.Create(NotPublic, location, Display(type), named));

                foreach (var contract in contracts) local.GetOrAdd(contract, _ => new ConcurrentBag<INamedTypeSymbol>()).Add(type);
            }, SymbolKind.NamedType);

            context.RegisterCompilationEndAction(endContext => {
                foreach (var pair in local) {
                    var types = pair.Value.OrderBy(Display, System.StringComparer.Ordinal).ToList();
                    if (types.Count < 2) continue;

                    foreach (var type in types) {
                        var others = string.Join(", ", types.Where(other => !SymbolEqualityComparer.Default.Equals(other, type)).Select(Display));
                        endContext.ReportDiagnostic(Diagnostic.Create(Duplicate, type.Locations.FirstOrDefault(), Display(type), Display(pair.Key), others));
                    }
                }

                ReportAmbiguity(endContext, operation, local);
            });
        }

        // Only public implementations of referenced assemblies are visible here; the Editor warns at run time for the rest.
        private static void ReportAmbiguity(CompilationAnalysisContext context, INamedTypeSymbol operation, ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<INamedTypeSymbol>> local) {
            var byContract = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
            foreach (var assembly in context.Compilation.SourceModule.ReferencedAssemblySymbols) {
                if (assembly.Name != OperationSymbols.PACK_ASSEMBLY && !References(assembly, OperationSymbols.PACK_ASSEMBLY)) continue;

                foreach (var type in OperationSymbols.ListTypes(assembly)) {
                    if (!OperationSymbols.IsPublic(type)) continue;

                    foreach (var contract in OperationSymbols.ListContracts(type, operation)) {
                        if (local.ContainsKey(contract)) continue;

                        if (!byContract.TryGetValue(contract, out var list)) byContract[contract] = list = new List<INamedTypeSymbol>();
                        list.Add(type);
                    }
                }
            }

            foreach (var pair in byContract) {
                var assemblies = pair.Value.Select(type => type.ContainingAssembly).Distinct<IAssemblySymbol>(SymbolEqualityComparer.Default).ToList();
                if (assemblies.Count < 2) continue;
                if (assemblies.Any(candidate => assemblies.All(other => SymbolEqualityComparer.Default.Equals(candidate, other) || References(candidate, other.Name)))) continue;

                var names = string.Join(", ", pair.Value.OrderBy(Display, System.StringComparer.Ordinal).Select(type => $"{Display(type)} ({type.ContainingAssembly.Name})"));
                context.ReportDiagnostic(Diagnostic.Create(Ambiguous, FindRegistry(context.Compilation), Display(pair.Key), names, context.Compilation.AssemblyName));
            }

            // Unity drops a diagnostic with no file from the Console, so it points at this assembly's generated registry.
            static Location FindRegistry(Compilation compilation) {
                var tree = compilation.SyntaxTrees.FirstOrDefault(candidate => candidate.FilePath.EndsWith(OperationRegistryGenerator.FILE_NAME));
                return tree == null ? Location.None : Location.Create(tree, default);
            }
        }

        // Whether `assembly` references `name`, directly or through other assemblies.
        private static bool References(IAssemblySymbol assembly, string name) {
            var visited = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
            var pending = new Stack<IAssemblySymbol>();
            pending.Push(assembly);

            while (pending.Count > 0) {
                foreach (var module in pending.Pop().Modules) {
                    foreach (var reference in module.ReferencedAssemblySymbols) {
                        if (reference.Name == name) return true;
                        if (visited.Add(reference)) pending.Push(reference);
                    }
                }
            }

            return false;
        }

        private static string Display(ISymbol symbol) {
            return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        }
    }
}
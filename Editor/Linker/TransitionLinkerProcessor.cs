using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Keeps what transitions reach by name through code stripping, with a link.xml written for each build:<br/>
    /// - every property a transition set up in an asset writes or a branch follows, since nothing in code names it, found by reading every scene, prefab and asset of the project;<br/>
    /// - <c>System.Type.GetType</c>, which the engine calls natively to load a generic type stored with <c>[SerializeReference]</c>, and which IL2CPP strips at High.<br/>
    /// Transitions built in code name their properties through expressions, so the linker sees those already.
    /// </summary>
    internal sealed class TransitionLinkerProcessor : IUnityLinkerProcessor {
        private static readonly string[] extensions = { ".unity", ".prefab", ".asset" };
        private static readonly string ownType = $"ns: {typeof(TransitionProperty<,>).Namespace}, asm: {typeof(TransitionProperty<,>).Assembly.GetName().Name}}}";

        // Each serialized type naming members, the field of its own that names them, and the properties it names: a property's one member,
        // or every member of a branch's path, as it reads each on its way.
        private static readonly (string Marker, Regex Pattern, Func<Type, string, PropertyInfo[]> Resolve)[] namers = {
            (Mark(typeof(TransitionProperty<,>)), Name(nameof(TransitionProperty<object, object>.member)), (type, member) => new[] { MemberAccess.Find(type, member) }),
            (Mark(typeof(TransitionBranch<,>)), Name(nameof(TransitionBranch<object, object>.path)), MemberAccess.FindPath)
        };

        public int callbackOrder => 0;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data) {
            var path = Path.GetFullPath("Library/RiseOn.Transitions/link.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, Write(ListProperties()));
            return path;
        }

        // Each written property once, by the type that declares it.
        private static IEnumerable<(string Assembly, string Type, string Property)> ListProperties() {
            var found = new HashSet<(string, string, string)>();
            foreach (var asset in AssetDatabase.GetAllAssetPaths()) {
                if (!extensions.Contains(Path.GetExtension(asset), StringComparer.OrdinalIgnoreCase)) continue;

                // Presets only feed the Editor; what is built from them sits in the scenes and prefabs read here.
                var main = AssetDatabase.GetMainAssetTypeAtPath(asset);
                if (main == typeof(TransitionPreset) || main == typeof(TransitionTargetPreset)) continue;

                string text;
                try {
                    text = File.ReadAllText(asset);
                } catch (Exception) {
                    continue;
                }

                foreach (var (marker, pattern, resolve) in namers) {
                    foreach (var (target, member) in Parse(text, marker, pattern)) {
                        var properties = Type.GetType(target) is { } type ? resolve(type, member) : null;
                        if (properties == null) continue;

                        foreach (var property in properties) {
                            if (property == null) continue;

                            var declaring = property.DeclaringType;
                            var name = (declaring.IsGenericType ? declaring.GetGenericTypeDefinition() : declaring).FullName.Replace('+', '/');
                            found.Add((declaring.Assembly.GetName().Name, name, property.Name));
                        }
                    }
                }
            }

            return found;
        }

        private static string Mark(Type definition) {
            return $"class: '{definition.Name}[";
        }

        private static Regex Name(string field) {
            return new Regex($@"\G\s*data:\s*\n\s*{field}: *([A-Za-z_][A-Za-z0-9_.]*)?", RegexOptions.Compiled);
        }

        // The target type and the member, or the path of members, of every value of the type marked stored in a text-serialized asset.
        private static IEnumerable<(string Target, string Member)> Parse(string text, string marker, Regex memberPattern) {
            for (var start = text.IndexOf(marker, StringComparison.Ordinal); start >= 0; start = text.IndexOf(marker, start + 1, StringComparison.Ordinal)) {
                var open = start + marker.Length - 1;
                var close = FindClose(text, open);
                if (close < 0 || text[open + 1] != '[') continue;

                var target = text.Substring(open + 2, FindClose(text, open + 1) - open - 2);
                var end = text.IndexOf('}', close);
                if (end < 0 || string.CompareOrdinal(text, end - ownType.Length + 1, ownType, 0, ownType.Length) != 0) continue;

                var member = memberPattern.Match(text, end + 1);
                if (member.Success && member.Groups[1].Success) yield return (target, member.Groups[1].Value);
            }

            static int FindClose(string text, int open) {
                var depth = 0;
                for (var i = open; i < text.Length; i++) {
                    if (text[i] is '[') depth++;
                    else if (text[i] is ']' && --depth is 0) return i;
                    else if (text[i] is '\n') return -1;
                }

                return -1;
            }
        }

        private static string Write(IEnumerable<(string Assembly, string Type, string Property)> properties) {
            var xml = new StringBuilder();
            xml.AppendLine("<linker>");
            xml.AppendLine("  <assembly fullname=\"mscorlib\">");
            xml.AppendLine("    <type fullname=\"System.Type\">");
            xml.AppendLine("      <method name=\"GetType\"/>");
            xml.AppendLine("    </type>");
            xml.AppendLine("  </assembly>");

            foreach (var assembly in properties.GroupBy(property => property.Assembly).OrderBy(group => group.Key, StringComparer.Ordinal)) {
                xml.AppendLine($"  <assembly fullname=\"{assembly.Key}\">");
                foreach (var type in assembly.GroupBy(property => property.Type).OrderBy(group => group.Key, StringComparer.Ordinal)) {
                    xml.AppendLine($"    <type fullname=\"{Escape(type.Key)}\">");
                    foreach (var property in type.OrderBy(property => property.Property, StringComparer.Ordinal)) xml.AppendLine($"      <property name=\"{property.Property}\"/>");
                    xml.AppendLine("    </type>");
                }

                xml.AppendLine("  </assembly>");
            }

            xml.Append("</linker>");
            return xml.ToString();

            static string Escape(string name) {
                return name.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
            }
        }
    }
}
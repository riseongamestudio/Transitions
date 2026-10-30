using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Saves and finds presets, and copies what they hold, so a preset and what it is applied to never share a part.<br/>
    /// Copies go through Unity's serializer, the same that stores them, so they keep exactly what an asset would.
    /// </summary>
    internal static class PresetUtils {
        public static TValue Copy<TValue>(TValue value) where TValue : class {
            return value == null ? null : (TValue)JsonUtility.FromJson(JsonUtility.ToJson(value), value.GetType());
        }

        /// <summary>
        /// Every preset of type <typeparamref name="TPreset"/> in the project, by name.
        /// </summary>
        public static List<TPreset> Find<TPreset>() where TPreset : ScriptableObject {
            return AssetDatabase.FindAssets($"t:{typeof(TPreset).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TPreset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(preset => preset != null)
                .OrderBy(preset => preset.name, System.StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Asks where to save <paramref name="preset"/>, then saves it there; nothing happens when the dialog is canceled.
        /// </summary>
        public static void Save(ScriptableObject preset, string name) {
            var path = EditorUtility.SaveFilePanelInProject($"Save {ObjectNames.NicifyVariableName(preset.GetType().Name)}", name, "asset", "Save the preset as an asset.");
            if (string.IsNullOrEmpty(path)) {
                Object.DestroyImmediate(preset);
                return;
            }

            preset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(preset);
        }

        /// <summary>
        /// A copy of <paramref name="target"/> without its object, as a preset keeps it;<br/>
        /// a target named after its object takes the object's name as its label, so the role keeps a name.
        /// </summary>
        public static ITransitionTarget CopyRole(ITransitionTarget target) {
            var copy = Copy(target);
            if (copy is not IUntypedTarget untyped) return copy;

            if (string.IsNullOrEmpty(untyped.Label) && untyped.Object != null) untyped.Label = untyped.Object.name;
            untyped.Object = null;
            return copy;
        }
    }
}
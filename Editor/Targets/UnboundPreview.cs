using Sirenix.OdinInspector.Editor;
using UnityEditor;
using Object = UnityEngine.Object;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// The object an unbound target is previewed on and its Capture buttons read, picked in its Inspector row.<br/>
    /// Kept for the Editor session only, by the object inspected and the field's path, as entity IDs, which last through script reloads,<br/>
    /// Prefab Mode's objects included; it is never saved: it is the Inspector's own state, not the asset's, and another project would hold a reference to nothing.
    /// </summary>
    internal static class UnboundPreview {
        private const string KEY_PREFIX = "RiseOn.Transitions.UnboundPreview/";

        public static Object Get(InspectorProperty property) {
            var key = GetKey(property);
            if (key == null) return null;

            var id = SessionState.GetInt(key, 0);
            return id is 0 ? null : EditorUtility.EntityIdToObject(id);
        }

        public static void Set(InspectorProperty property, Object value) {
            var key = GetKey(property);
            if (key == null) return;

            if (value == null) SessionState.EraseInt(key);
            else SessionState.SetInt(key, value.GetEntityId());
        }

        // Null for a field drawn on nothing that lasts, such as a plain C# object shown by a window.
        private static string GetKey(InspectorProperty property) {
            if (property.Tree.WeakTargets.Count is 0 || property.Tree.WeakTargets[0] is not Object owner || owner == null) return null;

            return $"{KEY_PREFIX}{(int)owner.GetEntityId()}/{property.Path}";
        }
    }
}
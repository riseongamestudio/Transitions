using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Odin's object field for an object targeted as <c>T</c>, a target's or an unbound target's preview object:<br/>
    /// it takes a <c>T</c> or what holds one, a GameObject or a component with a <c>T</c> component beside it, as <c>T</c> may be an interface,<br/>
    /// and names <c>T</c> in place of the object's own type, which Odin names.
    /// </summary>
    internal static class TargetFieldUtils {
        // The room Odin's object field keeps for its button opening the object in an inspector of its own, the pencil.
        private const float OPEN_BUTTON_WIDTH = 20f;

        /// <summary>
        /// Draws the field holding <paramref name="current"/>; true when a pick changed it, with the <typeparamref name="T"/> picked, null for None.<br/>
        /// A pick holding no <typeparamref name="T"/> changes nothing.
        /// </summary>
        /// <param name="prefix">Put before the object's name, such as <c>Preview </c> for a field that only previews.</param>
        public static bool Draw<T>(Rect rect, Object current, out T picked, string prefix = null) where T : class {
            var fieldType = typeof(Object).IsAssignableFrom(typeof(T)) ? typeof(T) : typeof(Object);

            var isRetitled = Event.current.type is EventType.Repaint && !EditorGUI.showMixedValue;
            var contentColor = GUI.contentColor;
            if (isRetitled) GUI.contentColor = Color.clear;

            EditorGUI.BeginChangeCheck();
            var field = SirenixEditorFields.UnityObjectField(rect, current, fieldType, true);
            GUI.contentColor = contentColor;
            if (isRetitled) DrawTitle<T>(rect, current, prefix);

            picked = null;
            if (!EditorGUI.EndChangeCheck()) return false;

            picked = field switch {
                T itself => itself,
                GameObject gameObject => gameObject.GetComponent(typeof(T)) as T,
                Component component => component.GetComponent(typeof(T)) as T,
                _ => null
            };
            return field == null || picked != null;
        }

        // The object's icon and name, then T's name, where and as Odin draws its object field's text.
        private static void DrawTitle<T>(Rect field, Object current, string prefix) {
            var text = $"{prefix}{(current != null ? current.name : "None")} ({(typeof(T).IsGenericType ? typeof(T).GetNiceName() : ObjectNames.NicifyVariableName(typeof(T).Name))})";
            var icon = current != null ? EditorGUIUtility.ObjectContent(current, current.GetType()).image : null;

            var title = field;
            title.width -= field.height + OPEN_BUTTON_WIDTH;
            title = title.Padding(0f, 1.5f);
            title.position += new Vector2(.5f, .5f);
            if (icon == null) title.x += 2f;
            title = title.AddXMin(2f);

            var iconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(12f, 12f));
            if (SirenixGUIStyles.Label.CalcWidth(text) > title.width) {
                GUI.Label(title.TakeFromRight(12f), "...", SirenixGUIStyles.Label);
            }

            GUI.Label(title, new GUIContent(text, icon), SirenixGUIStyles.Label);
            EditorGUIUtility.SetIconSize(iconSize);
        }
    }
}
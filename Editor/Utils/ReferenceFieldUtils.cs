using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// The field Odin draws for a <c>[SerializeReference]</c> value, picked with a window of the pack in place of Odin's selector.
    /// </summary>
    internal static class ReferenceFieldUtils {
        /// <summary>
        /// Draws <paramref name="property"/>'s value in Odin's reference field, titled <paramref name="title"/>, the full type as its tooltip.<br/>
        /// A click on it calls <paramref name="pick"/> with its rect. A value dragged onto it from another reference field is assigned on the next repaint.
        /// </summary>
        public static void Draw(InspectorProperty property, Rect field, int id, string title, Action<Rect> pick) {
            var current = Event.current;
            if (current.type is EventType.MouseDown && current.button is 0 && field.Contains(current.mousePosition)) {
                pick(field);
                current.Use();
            }

            var entry = property.ValueEntry;
            var value = entry.WeakSmartValue;
            EditorGUI.showMixedValue = entry.ValueState is PropertyValueState.ReferenceValueConflict;
            var dropped = SirenixEditorFields.PolymorphicObjectField(field, value, entry.BaseValueType, title, false, GUIUtility.keyboardControl == id, id);
            EditorGUI.showMixedValue = false;

            if (!ReferenceEquals(dropped, value)) property.Tree.DelayActionUntilRepaint(() => entry.WeakSmartValue = dropped);
            if (value != null) GUI.Label(field, new GUIContent(string.Empty, value.GetType().GetNiceName()));
        }
    }
}
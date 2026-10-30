using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Draws a None node of a list of nodes of <typeparamref name="T"/>, which the + button leaves when its pick is dropped, as Odin draws an empty reference field,<br/>
    /// whose click picks the node the same way, the members the list already writes grayed out.
    /// </summary>
    [DrawerPriority(0, 0, 2001)]
    public sealed class NoneNodeDrawer<T> : OdinValueDrawer<ITransitionNode<T>> where T : class {
        protected override bool CanDrawValueProperty(InspectorProperty property) {
            return property.ValueEntry.WeakSmartValue == null;
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var row = ItemList.MarkFirstRow(EditorGUILayout.GetControlRect());
            var field = SirenixEditorGUI.PrefixRect(row, label, out var prefix);
            var id = GUIUtility.GetControlID(FocusType.Keyboard, row);
            var serialized = Property.Tree.UnitySerializedObject?.FindProperty(Property.UnityPropertyPath);
            if (serialized != null) EditorGUI.BeginProperty(row, GUIContent.none, serialized);

            if (label != null) GUI.Label(prefix, label);
            ReferenceFieldUtils.Draw(Property, field, id, "None", ShowPicker);

            if (serialized != null) EditorGUI.EndProperty();
        }

        private void ShowPicker(Rect field) {
            NodeUtils.ShowAdd(field, typeof(T), NodeUtils.ListWritten(ListSiblings()), make => NodeUtils.Run(Property, () => Property.ValueEntry.WeakSmartValue = make()));
        }

        // The nodes of the list holding this one.
        private System.Collections.IEnumerable ListSiblings() {
            for (var parent = Property.Parent; parent != null; parent = parent.Parent) {
                if (parent.ValueEntry?.WeakSmartValue is IUntypedNodes nodes) return nodes.Nodes;
            }

            return System.Array.Empty<object>();
        }
    }
}
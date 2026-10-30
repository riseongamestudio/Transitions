using System;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Draws a None target, which the + button leaves when its pick is dropped, as Odin draws an empty reference field,<br/>
    /// whose click picks it the same way: a GameObject beside the field, then the type to target it as.<br/>
    /// Odin's own picker for it cannot build a <see cref="TransitionTarget{T}"/>.
    /// </summary>
    [DrawerPriority(0, 0, 2001)]
    public sealed class NoneTargetDrawer : OdinValueDrawer<ITransitionTarget> {
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
            ChoiceSearchWindow.Open(field, "Target", TargetTypeUtils.ListHierarchyChoices(Property.Tree.WeakTargets[0] as Object), ((Object Holder, Type Type) choice) => {
                Property.Tree.DelayActionUntilRepaint(() => Property.ValueEntry.WeakSmartValue = TargetTypeUtils.Create(choice.Type, choice.Holder));
                InternalEditorUtility.RepaintAllViews();
            });
        }
    }
}
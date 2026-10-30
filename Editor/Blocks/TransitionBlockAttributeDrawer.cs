using System;
using System.Reflection;
using RiseOn.Utils.Editor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Draws a modifier or source field as Odin draws any <c>[SerializeReference]</c> field, with Odin's own field and foldout,<br/>
    /// but titled with the block's short name, such as <c>Multiplication</c>, by its type registry name: the target and value types are left out,<br/>
    /// as the field already fixes them, and the full type shows as the tooltip. A click on the field opens <see cref="GenericTypePicker"/><br/>
    /// with the field's rules in place of Odin's selector.<br/>
    /// A block holding a single field drawn on one line, such as <c>Constant</c>, takes one row: the value column split in two halves,<br/>
    /// the block's field on the left, as any block field, and its one field on the right.
    /// </summary>
    [DrawerPriority(0, 0, 3000.5)]
    internal sealed class TransitionBlockAttributeDrawer : OdinAttributeDrawer<TransitionBlockAttribute> {
        protected override bool CanDrawAttributeProperty(InspectorProperty property) {
            return property.ValueEntry != null && property.ValueEntry.BaseValueType.IsInterface;
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var single = FindSingleLineField();
            if (single != null) {
                DrawInline(label, single);
                return;
            }

            var row = EditorGUILayout.GetControlRect();
            var field = SirenixEditorGUI.PrefixRect(row, label, out var prefix);
            var id = GUIUtility.GetControlID(FocusType.Keyboard, row);
            var value = Property.ValueEntry.WeakSmartValue;
            var hasChildren = value != null && Property.Children.Count > 0;

            var serialized = Property.Tree.UnitySerializedObject?.FindProperty(Property.UnityPropertyPath);
            if (serialized != null) EditorGUI.BeginProperty(row, GUIContent.none, serialized);

            GUIHelper.PushIndentLevel(0);
            if (hasChildren) {
                Property.State.Expanded = label != null
                    ? SirenixEditorGUI.Foldout(ref prefix, id, label, Property.State.Expanded, true)
                    : SirenixEditorGUI.Foldout(ref field, id, GUIContent.none, Property.State.Expanded, false);
            } else if (label != null) {
                GUI.Label(prefix, label);
            }

            ReferenceFieldUtils.Draw(Property, field, id, GetName(value), Pick);
            GUIHelper.PopIndentLevel();

            if (serialized != null) EditorGUI.EndProperty();

            if (SirenixEditorGUI.BeginFadeGroup(this, hasChildren && Property.State.Expanded)) {
                GUIHelper.PushIndentLevel(EditorGUI.indentLevel + 1);
                foreach (var child in Property.Children) child.Draw();
                GUIHelper.PopIndentLevel();
            }

            SirenixEditorGUI.EndFadeGroup();
        }

        // The block's one field when it is its only one and Unity draws it on one line; null otherwise, the block drawn as any reference field.
        private InspectorProperty FindSingleLineField() {
            if (Property.ValueEntry.WeakSmartValue == null || Property.Children.Count != 1) return null;

            var field = Property.Children[0];
            var type = field.ValueEntry?.TypeOfValue;
            if (type == null) return null;

            return RectFieldUtils.CanDraw(type) ? field : null;
        }

        // The label, then the value column in two halves: the block, then its one field.
        // Drawn in rects worked out here rather than by the layout, as the Ease row is, so the halves line up to the pixel from row to row.
        private void DrawInline(GUIContent label, InspectorProperty field) {
            var row = EditorGUILayout.GetControlRect();
            var value = SirenixEditorGUI.PrefixRect(row, label, out var prefix);
            if (label != null) GUI.Label(prefix, label);

            RectFieldUtils.Split(value, out var left, out var right);
            var id = GUIUtility.GetControlID(FocusType.Keyboard, left);
            GUIHelper.PushIndentLevel(0);
            ReferenceFieldUtils.Draw(Property, left, id, GetName(Property.ValueEntry.WeakSmartValue), Pick);
            RectFieldUtils.Draw(field, right);
            GUIHelper.PopIndentLevel();
        }

        // The block's registry name, or its class name without the generic arguments.
        private static string GetName(object value) {
            if (value == null) return "None";

            var type = value.GetType();
            var name = type.GetCustomAttribute<TypeRegistryItemAttribute>()?.Name;
            if (!string.IsNullOrEmpty(name)) return name;

            return type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;
        }

        // The picker, under the field, untitled: the field's name above it already says what is picked.
        private void Pick(Rect under) {
            GenericTypePicker.Pick(under, Property.ValueEntry.BaseValueType, Attribute, Assign, string.Empty);
        }

        // The value changes on the Inspector's next repaint, which is made to happen now.
        private void Assign(Type type) {
            Property.Tree.DelayActionUntilRepaint(() => Property.ValueEntry.WeakSmartValue = type == null ? null : Activator.CreateInstance(type));
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
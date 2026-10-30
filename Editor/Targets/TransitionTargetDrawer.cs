using System;
using System.Linq;
using RiseOn.Serializables;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Draws a <see cref="TransitionTarget&lt;T&gt;"/> in place of Odin's reference header: one row laid out as any field row,<br/>
    /// a foldout and the label in the label column, the label showing the object's name grayed out while empty, then the object in the value column<br/>
    /// and a + button that adds a node: a property writing one of the members of <typeparamref name="T"/> worth writing, those already written grayed out,<br/>
    /// or a branch reaching another object from it, as <see cref="NodeUtils.ShowAdd"/> picks it.<br/>
    /// The nodes follow as an <see cref="ItemList"/>, with no header of their own: the row is theirs.<br/>
    /// Its context menu changes <typeparamref name="T"/>, which drops the nodes, as they were set up for the type it had, and saves or applies a preset.
    /// </summary>
    [DrawerPriority(0, 0, 2001)]
    public sealed class TransitionTargetDrawer<T> : OdinValueDrawer<TransitionTarget<T>>, IDefinesGenericMenuItems where T : class {
        private const float ADD_WIDTH = 18f;

        private static GUIStyle placeholderStyle;

        private readonly ItemList list = new();

        private Rect headerRect;
        private Action<Rect> pendingWindow;

        protected override void DrawPropertyLayout(GUIContent label) {
            DrawHeader();
            OpenPendingWindow();

            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded)) {
                // The row keeps a margin of 2 below it; this makes it the item's padding, so the nodes sit as far from the row as from the item's edges.
                GUILayout.Space(1f);
                list.Draw(Property.Children[nameof(TransitionTarget<T>.nodes)].Children[0], "Press + to add a property or a branch.", Move, Remove, false);
                NodeUtils.DrawDuplicates(ValueEntry.SmartValue.nodes);
            }

            SirenixEditorGUI.EndFadeGroup();
        }

        // Laid out as any field row: the foldout and the label in the label column, the object and the + button in the value column.
        // The label and the object are drawn by hand into their rects, inside Unity's property scope, which draws their prefab override marks.
        private void DrawHeader() {
            var row = EditorGUILayout.GetControlRect();
            var value = ItemList.MarkFirstRow(row, out var prefix);
            var add = value.TakeFromRight(ADD_WIDTH);
            if (Event.current.type is EventType.Repaint) headerRect = row;

            GUIHelper.PushIndentLevel(0);
            Property.State.Expanded = SirenixEditorGUI.Foldout(ref prefix, GUIUtility.GetControlID(FocusType.Passive, prefix), GUIContent.none, Property.State.Expanded, false);
            DrawLabel(prefix);
            DrawObject(value.SubXMax(2f));
            if (ButtonUtils.IconButton(add, SdfIconType.Plus)) ShowAdd(add);
            GUIHelper.PopIndentLevel();
        }

        private void DrawLabel(Rect rect) {
            var label = Property.Children[nameof(TransitionTarget<T>.label)];
            var serialized = FindSerialized(label);
            if (serialized != null) EditorGUI.BeginProperty(rect, GUIContent.none, serialized);

            // Named after the property, so the placeholder knows when the field is being typed in.
            var controlName = label.Path;
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(controlName);
            var text = EditorGUI.TextField(rect, (string)label.ValueEntry.WeakSmartValue);
            if (EditorGUI.EndChangeCheck()) label.ValueEntry.WeakSmartValue = text;

            if (serialized != null) EditorGUI.EndProperty();
            if (!string.IsNullOrEmpty(text) || Event.current.type is not EventType.Repaint || GUI.GetNameOfFocusedControl() == controlName) return;

            // The object's name, grayed out in the empty field while it is not typed in: an empty label leaves the target named after its object.
            var source = ((IUntypedTarget)ValueEntry.SmartValue).Object;
            placeholderStyle ??= new GUIStyle(EditorStyles.textField) { normal = { background = null, textColor = SirenixGUIStyles.LeftAlignedGreyLabel.normal.textColor } };
            placeholderStyle.Draw(rect, new GUIContent(source != null ? source.name : "None"), false, false, false, false);
        }

        // The object, named as T, in Unity's property scope, which draws its prefab override mark.
        private void DrawObject(Rect rect) {
            var target = Property.Children[nameof(TransitionTarget<T>.target)];
            var serialized = FindSerialized(target);
            if (serialized != null) EditorGUI.BeginProperty(rect, GUIContent.none, serialized);

            if (TargetFieldUtils.Draw(rect, ((IUntypedTarget)ValueEntry.SmartValue).Object, out T picked)) target.ValueEntry.WeakSmartValue = new SerObject<T>(picked);

            if (serialized != null) EditorGUI.EndProperty();
        }

        private static SerializedProperty FindSerialized(InspectorProperty property) {
            return property.Tree.UnitySerializedObject?.FindProperty(property.UnityPropertyPath);
        }

        // The node to add, or None first, for a node to set up later; closing the window adds nothing.
        private void ShowAdd(Rect button) {
            NodeUtils.ShowAdd(button, typeof(T), NodeUtils.ListWritten(ValueEntry.SmartValue.nodes), make => NodeUtils.Run(Property, () => NodeUtils.Add(Property, Nodes, make)));
        }

        private void Move(int from, int to) {
            NodeUtils.Move(Property, value => ((TransitionTarget<T>)value).nodes, from, to);
        }

        private void Remove(int index) {
            NodeUtils.Remove(Property, value => ((TransitionTarget<T>)value).nodes, index);
        }

        private static IUntypedNodes Nodes(object value) {
            return (TransitionTarget<T>)value;
        }

        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu) {
            // Both pick among the types of the object, so they need one, and one target to work them out from.
            var source = ((IUntypedTarget)ValueEntry.SmartValue).Object;
            var canPick = source != null && ValueEntry.ValueCount is 1;
            AddItem("Change Type", canPick, () => Request(ShowChangeType));
            AddItem("Replace Type", canPick, () => Request(ShowReplaceType));

            genericMenu.AddItem(new GUIContent("Save as Preset"), false, () => {
                var preset = ScriptableObject.CreateInstance<TransitionTargetPreset>();
                preset.target = PresetUtils.CopyRole(ValueEntry.SmartValue);
                PresetUtils.Save(preset, string.IsNullOrEmpty(preset.target.Label) ? typeof(T).Name : preset.target.Label);
            });

            genericMenu.AddItem(new GUIContent("Apply Preset"), false, () => Request(ShowPresets));

            void AddItem(string text, bool isEnabled, GenericMenu.MenuFunction run) {
                if (isEnabled) genericMenu.AddItem(new GUIContent(text), false, run);
                else genericMenu.AddDisabledItem(new GUIContent(text));
            }
        }

        // A window asked for by a menu item, which runs outside any drawing, opens on the next layout under the row.
        private void Request(Action<Rect> window) {
            pendingWindow = window;
            InternalEditorUtility.RepaintAllViews();
        }

        private void OpenPendingWindow() {
            if (pendingWindow == null || Event.current.type is not EventType.Layout) return;

            var open = pendingWindow;
            pendingWindow = null;
            open(headerRect);
        }

        // The types of the object's GameObject, so the target can move to a sibling component, the one in use grayed out:
        // the target starts over on the type picked, keeping nothing.
        private void ShowChangeType(Rect under) {
            var source = ((IUntypedTarget)ValueEntry.SmartValue).Object;
            if (source == null) return;

            var choices = TargetTypeUtils.ListChoices(source is Component component ? component.gameObject : source, string.Empty)
                .Select(choice => new SearchChoice<(UnityEngine.Object Holder, Type Type)>(choice.Path, choice.Value,
                    choice.Value.Type == typeof(T) && choice.Value.Holder == source ? "current" : null, choice.Icon, choice.LabelSearch, choice.FolderIcon));
            ChoiceSearchWindow.Open(under, "Change Type", choices, ((UnityEngine.Object Holder, Type Type) choice) => {
                NodeUtils.Run(Property, () => Property.ValueEntry.WeakSmartValue = TargetTypeUtils.Create(choice.Type, choice.Holder));
            });
        }

        // The types of the object's GameObject that keep every node working, the others grayed out with the members they would lose:
        // the target moves to one keeping its label and every node, such as to a type deriving from T, to add properties only it has.
        private void ShowReplaceType(Rect under) {
            if (((IUntypedTarget)ValueEntry.SmartValue).Object == null) return;

            ChoiceSearchWindow.Open(under, "Replace Type", TargetTypeUtils.ListReplaceChoices(ValueEntry.SmartValue), ((UnityEngine.Object Holder, Type Type) choice) => {
                NodeUtils.Run(Property, () => Property.ValueEntry.WeakSmartValue = TargetTypeUtils.Retarget(ValueEntry.SmartValue, choice.Type, choice.Holder, out _));
            });
        }

        // The presets set up for T or a type T derives from, of the project and its packages; empty when there is none.
        private void ShowPresets(Rect under) {
            var choices = PresetUtils.Find<TransitionTargetPreset>()
                .Where(preset => preset.target != null && preset.target.TargetType.IsAssignableFrom(typeof(T)))
                .Select(preset => new SearchChoice<TransitionTargetPreset>(preset.name, preset));
            ChoiceSearchWindow.Open(under, "Preset", choices, (TransitionTargetPreset preset) => NodeUtils.Run(Property, () => Apply(preset)));
        }

        // The preset's nodes replace the target's; set up for T or a base of it, they take a T as they are.
        private void Apply(TransitionTargetPreset preset) {
            Property.RecordForUndo($"Apply {preset.name}", true);
            for (var i = 0; i < ValueEntry.ValueCount; i++) {
                var target = ValueEntry.Values[i];
                target.nodes.Clear();
                foreach (var node in ((IUntypedTarget)PresetUtils.Copy(preset.target)).Nodes) target.nodes.Add((ITransitionNode<T>)node);
            }

            ValueEntry.Values.ForceMarkDirty();
        }
    }
}
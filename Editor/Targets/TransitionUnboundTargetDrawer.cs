using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Draws a <see cref="TransitionUnboundTarget&lt;T&gt;"/> as <see cref="TransitionDrawer"/> draws a transition: a title bar with the field's name,<br/>
    /// then over the value column the object to preview it on, the preview slider and the + button, then its nodes as an <see cref="ItemList"/>.<br/>
    /// The preview object takes a <typeparamref name="T"/> or what holds one, as a target's object does; it is kept by <see cref="UnboundPreview"/>, never saved.<br/>
    /// The slider shows only while there is a preview object and a node, and previews as a transition's does; without it, the object takes the whole column. Its context menu saves or applies a target preset.
    /// </summary>
    public sealed class TransitionUnboundTargetDrawer<T> : OdinValueDrawer<TransitionUnboundTarget<T>>, IDefinesGenericMenuItems where T : class {
        private const float GAP = 6f;
        private const float ADD_WIDTH = 18f;

        // The room the layout keeps on each side of the +, about as much as above and below it in Odin's toolbar.
        private const float ADD_INSET = 2f;

        private static readonly GUIContent PREVIEW_TOOLTIP = new(string.Empty, "Hold and drag to preview t from 0 (hidden) to 1 (shown) on the object beside. Letting go puts it back as it was.");

        private readonly ItemList list = new();

        private bool isPreviewing;
        private float preview = 1f;

        private Rect headerRect;
        private Action<Rect> pendingWindow;

        protected override void DrawPropertyLayout(GUIContent label) {
            var items = Property.Children[nameof(TransitionUnboundTarget<T>.nodes)].Children[0];

            SirenixEditorGUI.BeginIndentedVertical(SirenixGUIStyles.PropertyMargin);
            DrawHeader(label, items.Children.Count > 0);
            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded)) {
                list.Draw(items, "Press + to add a property or a branch.", Move, Remove, true);
                NodeUtils.DrawDuplicates(ValueEntry.SmartValue.nodes);
            }

            SirenixEditorGUI.EndFadeGroup();
            SirenixEditorGUI.EndIndentedVertical();

            OpenPendingWindow();
        }

        // The name over the label column; over the value column, the preview object and the slider sharing it, then the + button.
        private void DrawHeader(GUIContent label, bool hasNodes) {
            SirenixEditorGUI.BeginHorizontalToolbar();
            var title = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.labelWidth));
            Property.State.Expanded = SirenixEditorGUI.Foldout(title, Property.State.Expanded, label ?? GUIContent.none);

            // Placed from the header, which is only known outside layout, as the transition's header places its slider.
            var bar = Event.current.type is EventType.Layout ? default : GUIHelper.GetCurrentLayoutRect();
            var value = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            GUILayout.Space(GAP);
            GUILayoutUtility.GetRect(ADD_WIDTH + 2f * ADD_INSET, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(false));
            var add = new Rect(0f, 0f, ADD_WIDTH, EditorGUIUtility.singleLineHeight);
            if (Event.current.type is not EventType.Layout) {
                var inside = new Rect(bar.x, bar.y, bar.width - 1f, bar.height - 1f);
                var inset = (inside.height - add.height) / 2f;
                add.x = inside.xMax - inset - add.width;
                add.y = inside.y + inset;
                value.xMin = bar.x + EditorGUIUtility.labelWidth + 2f;
                value.xMax = add.x - GAP;
                value.y = add.y;
            }

            // The object shares the value column with the slider while there is one, and takes it all while there is none.
            var hasSlider = hasNodes && UnboundPreview.Get(Property) != null && ValueEntry.ValueCount is 1;
            var field = value;
            if (hasSlider) field.width = Mathf.Floor((value.width - GAP) / 2f);
            var slider = value;
            slider.xMin = field.xMax + GAP;

            var target = DrawObject(field);
            DrawPreview(slider, hasSlider && target != null, target);
            if (ButtonUtils.IconButton(add, SdfIconType.Plus)) ShowAdd(add);
            SirenixEditorGUI.EndHorizontalToolbar();

            if (Event.current.type is EventType.Repaint) headerRect = GUILayoutUtility.GetLastRect();
        }

        // The preview object, named as T; picking another ends a preview running on the one before.
        private T DrawObject(Rect rect) {
            var current = UnboundPreview.Get(Property);
            if (TargetFieldUtils.Draw(rect, current, out T picked, "Preview ")) {
                EndPreview();
                current = picked as Object;
                UnboundPreview.Set(Property, current);
            }

            if (current == null && Event.current.type is EventType.Repaint) GUI.Label(rect, new GUIContent(string.Empty, "An object to preview the nodes on and to capture rest values from; kept for this Editor session only."));
            return current as T;
        }

        // The control ID is taken whether the slider shows or not, so a field drawn after the header keeps its own ID.
        private void DrawPreview(Rect rect, bool canPreview, T target) {
            var id = GUIUtility.GetControlID(FocusType.Passive, rect);
            var current = Event.current;
            if (!canPreview) {
                if (isPreviewing) EndPreview();
                return;
            }

            switch (current.GetTypeForControl(id)) {
                case EventType.MouseDown when current.button is 0 && rect.Contains(current.mousePosition):
                    GUIUtility.hotControl = id;
                    isPreviewing = true;
                    BeginPreview(target);
                    Apply(ToT(current.mousePosition.x));
                    current.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    Apply(ToT(current.mousePosition.x));
                    current.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    EndPreview();
                    current.Use();
                    break;
                case EventType.Layout or EventType.Repaint when isPreviewing && GUIUtility.hotControl != id:
                    // The slider lost the pointer without a release reaching it, such as when another window took the focus.
                    EndPreview();
                    break;
            }

            GUI.HorizontalSlider(rect, isPreviewing ? preview : 1f, 0f, 1f);
            if (current.type is EventType.Repaint) GUI.Label(rect, PREVIEW_TOOLTIP);

            float ToT(float x) {
                return Mathf.Clamp01((x - rect.x) / rect.width);
            }
        }

        private void BeginPreview(T target) {
            var unbound = ValueEntry.SmartValue;
            var writes = new List<(object Instance, PropertyInfo Member)>();
            foreach (var node in unbound.nodes.OfType<IUntypedNode>()) node.ListWrites(target, writes);

            TransitionPreview.Begin(this, t => unbound.Apply(target, t), writes);
        }

        private void Apply(float t) {
            preview = t;
            TransitionPreview.Apply(this, t);
        }

        private void EndPreview() {
            isPreviewing = false;
            TransitionPreview.End(this);
        }

        private void ShowAdd(Rect button) {
            NodeUtils.ShowAdd(button, typeof(T), NodeUtils.ListWritten(ValueEntry.SmartValue.nodes), make => NodeUtils.Run(Property, () => NodeUtils.Add(Property, value => (TransitionUnboundTarget<T>)value, make)));
        }

        private void Move(int from, int to) {
            NodeUtils.Move(Property, value => ((TransitionUnboundTarget<T>)value).nodes, from, to);
        }

        private void Remove(int index) {
            NodeUtils.Remove(Property, value => ((TransitionUnboundTarget<T>)value).nodes, index);
        }

        // Into Odin's context menu of the field: a target preset holds the nodes of a target, so it carries them both ways.
        public void PopulateGenericMenu(InspectorProperty property, GenericMenu menu) {
            menu.AddItem(new GUIContent("Save as Preset"), false, () => {
                var preset = ScriptableObject.CreateInstance<TransitionTargetPreset>();
                var target = new TransitionTarget<T>(null, Property.NiceName);
                foreach (var node in PresetUtils.Copy(ValueEntry.SmartValue).nodes) target.nodes.Add(node);
                preset.target = target;
                PresetUtils.Save(preset, Property.NiceName);
            });

            // The presets set up for T or a type T derives from, in a window under the header, empty when there is none.
            menu.AddItem(new GUIContent("Apply Preset"), false, () => {
                pendingWindow = rect => {
                    var choices = PresetUtils.Find<TransitionTargetPreset>()
                        .Where(preset => preset.target != null && preset.target.TargetType.IsAssignableFrom(typeof(T)))
                        .Select(preset => new SearchChoice<TransitionTargetPreset>(preset.name, preset));
                    ChoiceSearchWindow.Open(rect, "Preset", choices, (TransitionTargetPreset preset) => NodeUtils.Run(Property, () => Apply(preset)));
                };
                InternalEditorUtility.RepaintAllViews();
            });
        }

        // A window asked for outside any drawing, such as from a menu, opens on the next layout under the header.
        private void OpenPendingWindow() {
            if (pendingWindow == null || Event.current.type is not EventType.Layout) return;

            var open = pendingWindow;
            pendingWindow = null;
            open(headerRect);
        }

        // The preset's nodes replace the unbound target's; set up for T or a base of it, they take a T as they are.
        private void Apply(TransitionTargetPreset preset) {
            Property.RecordForUndo($"Apply {preset.name}", true);
            for (var i = 0; i < ValueEntry.ValueCount; i++) {
                var unbound = ValueEntry.Values[i];
                unbound.nodes.Clear();
                foreach (var node in ((IUntypedTarget)PresetUtils.Copy(preset.target)).Nodes) unbound.nodes.Add((ITransitionNode<T>)node);
            }

            ValueEntry.Values.ForceMarkDirty();
        }
    }
}
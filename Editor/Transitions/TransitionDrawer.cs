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
    /// Draws a <see cref="Transition"/> as Odin draws a list: a title bar with the field's name, a preview slider over the value column and the + button,<br/>
    /// then its targets as an <see cref="ItemList"/>. Its context menu, Odin's, saves or applies a preset.<br/>
    /// The slider shows only while a target has an object, and acts only while held: pressing it saves every member the transition writes,<br/>
    /// dragging applies <c>t</c>, and letting go sets every saved member back, as <see cref="TransitionPreview"/> does. Nothing is recorded for Undo.<br/>
    /// A target is added by dropping an object on the box, which asks which type to target it as: its own, a base class or an interface,<br/>
    /// or one of its components for a GameObject. The + button picks one in one drop-down: a GameObject beside the field,<br/>
    /// then the type to target it as; closing it with nothing picked adds a None target, as Odin's + does.
    /// </summary>
    public sealed class TransitionDrawer : OdinValueDrawer<Transition>, IDefinesGenericMenuItems {
        private const float PREVIEW_GAP = 6f;
        private const float ADD_WIDTH = 18f;

        // The room the layout keeps on each side of the +, about as much as above and below it in Odin's toolbar.
        private const float ADD_INSET = 2f;

        private static readonly GUIContent PREVIEW_TOOLTIP = new(string.Empty, "Hold and drag to preview t from 0 (hidden) to 1 (shown). Letting go puts every object back as it was.");
        private static readonly Color DROP_FACE = new(.3f, .6f, 1f, .1f);
        private static readonly Color DROP_OUTLINE = new(.3f, .6f, 1f, 1f);

        private readonly ItemList list = new();

        private bool isPreviewing;
        private float preview = 1f;
        private bool isDropHighlighted;

        private Rect headerRect;
        private Action<Rect> pendingWindow;

        protected override void DrawPropertyLayout(GUIContent label) {
            var eventType = Event.current.type;
            var items = Property.Children[nameof(Transition.targets)].Children[0];

            // Built as Odin builds its lists: a toolbar for the title, then the framed items right under it, reaching the frame.
            var box = SirenixEditorGUI.BeginIndentedVertical(SirenixGUIStyles.PropertyMargin);
            DrawHeader(label, CanPreview());
            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded)) list.Draw(items, "Drop objects here, or press + to add a target.", Move, Remove, true);
            SirenixEditorGUI.EndFadeGroup();
            SirenixEditorGUI.EndIndentedVertical();

            OpenPendingWindow();
            HandleDrop(eventType, box);
        }

        // Only a target with an object has anything to show.
        private bool CanPreview() {
            for (var i = 0; i < ValueEntry.ValueCount; i++) {
                if (ValueEntry.Values[i]?.targets.OfType<IUntypedTarget>().Any(target => target.Object != null) is true) return true;
            }

            return false;
        }

        // The name over the label column, then the slider over the value column up to the + button.
        private void DrawHeader(GUIContent label, bool canPreview) {
            SirenixEditorGUI.BeginHorizontalToolbar();
            // A control rect, margins included, as Odin takes the one of its list titles.
            var title = EditorGUILayout.GetControlRect(false, GUILayout.Width(EditorGUIUtility.labelWidth));
            Property.State.Expanded = SirenixEditorGUI.Foldout(title, Property.State.Expanded, label ?? GUIContent.none);

            // Split as a field row: the title over the label column, the slider and the + over the value column, the slider from its left edge.
            // The layout only reserves their room: they are placed from the header, which is only known outside layout, where they are drawn and clicked.
            var bar = Event.current.type is EventType.Layout ? default : GUIHelper.GetCurrentLayoutRect();
            var slider = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            GUILayout.Space(PREVIEW_GAP);

            // The same + as a target's row: Odin's inline button, a line high, as far from the toolbar's right border as from its top and bottom ones.
            // Odin draws the top border above the toolbar's rect, and the bottom and right ones on its last row and column, so those are left out.
            GUILayoutUtility.GetRect(ADD_WIDTH + 2f * ADD_INSET, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(false));
            var add = new Rect(0f, 0f, ADD_WIDTH, EditorGUIUtility.singleLineHeight);
            if (Event.current.type is not EventType.Layout) {
                var inside = new Rect(bar.x, bar.y, bar.width - 1f, bar.height - 1f);
                var inset = (inside.height - add.height) / 2f;
                add.x = inside.xMax - inset - add.width;
                add.y = inside.y + inset;
                slider.xMin = bar.x + EditorGUIUtility.labelWidth + 2f;
                slider.xMax = add.x - PREVIEW_GAP;
                slider.y = add.y;
            }

            DrawPreview(slider, canPreview);
            if (ButtonUtils.IconButton(add, SdfIconType.Plus)) ShowAdd(add);
            SirenixEditorGUI.EndHorizontalToolbar();

            if (Event.current.type is EventType.Repaint) headerRect = GUILayoutUtility.GetLastRect();
        }

        // The control ID is taken whether the slider shows or not, so a field drawn after the header keeps its own ID while objects come and go.
        private void DrawPreview(Rect rect, bool canPreview) {
            var id = GUIUtility.GetControlID(FocusType.Passive, rect);
            var current = Event.current;
            if (!canPreview) return;

            switch (current.GetTypeForControl(id)) {
                case EventType.MouseDown when current.button is 0 && rect.Contains(current.mousePosition):
                    GUIUtility.hotControl = id;
                    isPreviewing = true;
                    BeginPreview();
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

            // Drawn in every event, as it takes a control ID: drawn only on repaint, it would shift the ID of every control after it between events,
            // so none of them would keep the focus or the press it took. What it would act on is used up above, so it only shows the value.
            GUI.HorizontalSlider(rect, isPreviewing ? preview : 1f, 0f, 1f);
            if (current.type is EventType.Repaint) GUI.Label(rect, PREVIEW_TOOLTIP);

            float ToT(float x) {
                return Mathf.Clamp01((x - rect.x) / rect.width);
            }
        }

        // Every selected value at once, as each writes its own objects.
        private void BeginPreview() {
            var transitions = Enumerable.Range(0, ValueEntry.ValueCount).Select(i => ValueEntry.Values[i]).Where(transition => transition != null).ToList();
            var writes = new List<(object Instance, PropertyInfo Member)>();
            foreach (var target in transitions.SelectMany(transition => transition.targets).OfType<IUntypedTarget>()) target.ListWrites(writes);

            TransitionPreview.Begin(this, t => {
                foreach (var transition in transitions) transition.Apply(t);
            }, writes);
        }

        private void Apply(float t) {
            preview = t;
            TransitionPreview.Apply(this, t);
        }

        // Every object goes back to exactly how it was before the preview, whatever t = 1 would give.
        private void EndPreview() {
            isPreviewing = false;
            TransitionPreview.End(this);
        }

        // The GameObject and the type the new target targets, or Null first, for a target to set up later; closing the window adds nothing.
        private void ShowAdd(Rect button) {
            var choices = TargetTypeUtils.ListHierarchyChoices(Property.Tree.WeakTargets[0] as Object)
                .Select(choice => new SearchChoice<(Object Holder, Type Type)?>(choice.Path, choice.Value, choice.DisabledReason, choice.Icon, choice.LabelSearch, choice.FolderIcon))
                .Prepend(SearchChoice<(Object Holder, Type Type)?>.None());
            ChoiceSearchWindow.Open(button, "Target", choices, ((Object Holder, Type Type)? choice) => Run(() => Add(choice?.Holder, choice?.Type)));
        }

        private void Move(int from, int to) {
            Property.RecordForUndo("Move Transition Target", true);
            for (var i = 0; i < ValueEntry.ValueCount; i++) {
                var targets = ValueEntry.Values[i].targets;
                if (from >= targets.Count || to >= targets.Count) continue;

                var target = targets[from];
                targets.RemoveAt(from);
                targets.Insert(to, target);
            }

            ValueEntry.Values.ForceMarkDirty();
        }

        private void Remove(int index) {
            Property.RecordForUndo("Remove Transition Target", true);
            for (var i = 0; i < ValueEntry.ValueCount; i++) {
                var targets = ValueEntry.Values[i].targets;
                if (index < targets.Count) targets.RemoveAt(index);
            }

            ValueEntry.Values.ForceMarkDirty();
        }

        // As SerObject lists take drops: only when nothing inside took the drag, over the whole box, outlined while it hovers.
        private void HandleDrop(EventType eventType, Rect rect) {
            if (eventType is EventType.Repaint && isDropHighlighted) {
                isDropHighlighted = false;
                Handles.DrawSolidRectangleWithOutline(rect, DROP_FACE, DROP_OUTLINE);
                return;
            }

            if (eventType is not (EventType.DragUpdated or EventType.DragPerform) || DragAndDrop.visualMode is not DragAndDropVisualMode.None) return;
            if (!rect.Contains(Event.current.mousePosition) || DragAndDrop.objectReferences.Length is 0) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (eventType is EventType.DragPerform) {
                DragAndDrop.AcceptDrag();
                ShowTypes(new Rect(Event.current.mousePosition, Vector2.zero), DragAndDrop.objectReferences);
            } else {
                isDropHighlighted = true;
            }

            Event.current.Use();
        }

        // One window for every object, each in a folder of its own name when there are several; each pick adds one target.
        private void ShowTypes(Rect under, Object[] sources) {
            var choices = sources.Where(source => source != null).SelectMany(source => TargetTypeUtils.ListChoices(source, sources.Length > 1 ? $"{source.name}/" : string.Empty));
            ChoiceSearchWindow.Open(under, "Target As", choices, ((Object Holder, Type Type) choice) => Run(() => Add(choice.Holder, choice.Type)));
        }

        // A null type adds a None target.
        private void Add(Object holder, Type type) {
            Property.RecordForUndo("Add Transition Target", true);
            for (var i = 0; i < ValueEntry.ValueCount; i++) ValueEntry.Values[i]?.targets.Add(type == null ? null : TargetTypeUtils.Create(type, holder));

            ValueEntry.Values.ForceMarkDirty();
            Property.State.Expanded = true;
        }

        // Into Odin's context menu of the field, which takes the right click before any drawer of it sees one.
        public void PopulateGenericMenu(InspectorProperty property, GenericMenu menu) {
            menu.AddItem(new GUIContent("Save as Preset"), false, () => {
                var preset = ScriptableObject.CreateInstance<TransitionPreset>();
                foreach (var target in ValueEntry.SmartValue.targets) preset.transition.targets.Add(PresetUtils.CopyRole(target));
                PresetUtils.Save(preset, Property.NiceName);
            });

            // The presets of the project and its packages, in a window under the header, empty when there is none.
            menu.AddItem(new GUIContent("Apply Preset"), false, () => {
                pendingWindow = rect => {
                    var choices = PresetUtils.Find<TransitionPreset>().Select(preset => new SearchChoice<TransitionPreset>(preset.name, preset));
                    ChoiceSearchWindow.Open(rect, "Preset", choices, (TransitionPreset preset) => Run(() => Apply(preset)));
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

        // The preset's roles replace every target, each without an object until one is dropped in.
        private void Apply(TransitionPreset preset) {
            Property.RecordForUndo($"Apply {preset.name}", true);
            for (var i = 0; i < ValueEntry.ValueCount; i++) {
                var transition = ValueEntry.Values[i];
                transition.targets.Clear();
                foreach (var target in preset.transition.targets) transition.targets.Add(PresetUtils.Copy(target));
            }

            ValueEntry.Values.ForceMarkDirty();
        }

        // Selections and menu items run outside any drawing; the change waits for the Inspector's next repaint, made to happen now.
        private void Run(Action action) {
            Property.Tree.DelayActionUntilRepaint(action);
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
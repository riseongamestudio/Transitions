using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RiseOn.Utils.Editor;
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
    /// Draws a <see cref="Transition"/> as a <see cref="Section"/> that folds: the field's name on the bar over the label column, a preview slider over the value column and the + button at its end, then its targets as <see cref="SectionRows"/> whose rows meet the border.<br/>
    /// Its context menu, Odin's, saves or applies a preset.<br/>
    /// The slider shows only while a target has an object, and acts only while held: pressing it saves every member the transition writes, dragging applies <c>t</c>, and letting go sets every saved member back, as <see cref="TransitionPreview"/> does.<br/>
    /// Nothing is recorded for Undo.<br/>
    /// A target is added by dropping an object on the box, which asks which type to target it as: its own, a base class or an interface, or one of its components for a GameObject.<br/>
    /// The + button picks one in one drop-down: a GameObject beside the field, then the type to target it as; closing it with nothing picked adds a None target, as Odin's + does.
    /// </summary>
    public sealed class TransitionDrawer : OdinValueDrawer<Transition>, IDefinesGenericMenuItems {
        private static readonly GUIContent previewTooltip = new(string.Empty, "Hold and drag to preview t from 0 (hidden) to 1 (shown). Letting go puts every object back as it was.");
        private static readonly Color dropFace = new(.3f, .6f, 1f, .1f);
        private static readonly Color dropOutline = new(.3f, .6f, 1f, 1f);

        private readonly SectionRows list = new();

        private bool isPreviewing;
        private float preview = 1f;
        private bool isDropHighlighted;

        private Rect headerRect;
        private Action<Rect> pendingWindow;

        protected override void Initialize() {
            Section.Restore(Property, Property.State.Expanded);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var eventType = Event.current.type;
            var items = Property.Children[nameof(Transition.targets)].Children[0];

            // The bar's slider and + first, then the targets in the section's fade.
            using (var section = Section.Foldout(Property, label?.text ?? string.Empty, label?.tooltip, buttons: 1, padded: false, barValue: true)) {
                DrawPreview(section.BarValue, CanPreview());
                if (section.Button(0, SdfIconType.Plus, "Add a target")) ShowAdd(section.ButtonRect(0));
                if (section.Open) DrawTargets(items);
            }

            var box = GUILayoutUtility.GetLastRect();
            if (eventType is EventType.Repaint) headerRect = new Rect(box.x, box.y, box.width, Section.Height);

            OpenPendingWindow();
            HandleDrop(eventType, box);
        }

        private void DrawTargets(InspectorProperty items) {
            list.Draw(items.Children.Count, i => items.Children[i].Draw(GUIContent.none), "Drop objects here, or press + to add a target.", Move, Remove, marksFirstRow: true);
        }

        // Only a target with an object has anything to show.
        private bool CanPreview() {
            for (var i = 0; i < ValueEntry.ValueCount; i++) {
                if (ValueEntry.Values[i]?.targets.OfType<IUntypedTarget>().Any(target => target.Object != null) is true) return true;
            }

            return false;
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
            if (current.type is EventType.Repaint) GUI.Label(rect, previewTooltip);

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
                // Rounded as the section's box, on the pixel grid as its border is.
                isDropHighlighted = false;
                var shape = PixelUtils.Snap(rect);
                GUI.DrawTexture(shape, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, dropFace, 0f, Section.Radius);
                GUI.DrawTexture(shape, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, dropOutline, 1f, Section.Radius);
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
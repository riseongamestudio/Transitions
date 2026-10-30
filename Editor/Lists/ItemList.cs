using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Draws the items of a list with the measures of Odin's list items: the same drag handle texture, the same remove button, at the same places.<br/>
    /// What differs is what the lists of this pack need: no title bar of its own, as the value holding the list draws it;<br/>
    /// the handle and the remove button level with the item's first row, not centered on a tall item, and only that row kept clear of them:<br/>
    /// the rows below start under the middle of the handle, so nested values do not drift inward level by level, and keep the item's one padding on the other sides;<br/>
    /// a hint while the list is empty; and a drag that moves: the dragged item follows the pointer above the others, which glide aside to open the gap it will land in.<br/>
    /// The item's drawer lays out its first row with <see cref="MarkFirstRow(Rect)"/>, which also keeps it clear of the handle and the button.
    /// </summary>
    internal sealed class ItemList {
        // Odin's list items: the handle 20 wide at 4 from the left, the remove button 14 wide at 18 from the right, in columns 25 and 20 wide.
        private const float HANDLE_SIZE = 20f;
        private const float HANDLE_INSET = 4f;
        private const float HANDLE_COLUMN = 25f;
        private const float REMOVE_SIZE = 14f;
        private const float REMOVE_INSET = 18f;
        private const float REMOVE_COLUMN = 20f;
        private const int ITEM_PADDING = 3;

        // Where the rows below an item's first start, from the item's left edge: about under the middle of the handle;
        // in a list inside another's item, where the first row starts, so a foldout below lines up with the item's own instead of standing out to its left.
        private const int BODY_INSET = 14;
        private const int NESTED_BODY_INSET = 20;

        // How fast the other items glide to their place while one is dragged: the share of the way left covered per second, exponentially.
        private const float GLIDE_RATE = 18f;

        // How faint Odin draws its drag handle.
        private static readonly Color HANDLE_TINT = new(1f, 1f, 1f, .4f);
        private static readonly Color DRAG_OUTLINE = new(.3f, .6f, 1f, 1f);

        // An item of a list inside another's item: its handle at its very edge, as the outer item already sets it apart.
        private const float NESTED_HANDLE_INSET = 0f;

        // An item, edge to edge, so what Odin draws along an item's edge, such as the bar of a right click, sits where it does on Odin's own items.
        private static GUIStyle itemStyle;

        // Holds what an item draws, its body's inset on the left and the item's one padding elsewhere, which a field's own margin merges into instead of adding to.
        private static GUIStyle contentStyle;
        private static GUIStyle nestedContentStyle;

        // The items being drawn, the innermost list's on top; an item's drawer marks its first row on the top one.
        private static readonly Stack<Slot> slots = new();

        private readonly List<Rect> itemRects = new();
        private readonly List<float> offsets = new();

        // The whole drag belongs to one control of the list, so it holds while the dragged item is drawn out of its place.
        private int listId;
        private int dragIndex = -1;
        private int landIndex;
        private float pressY;
        private float dragOffset;
        private double glideTime;

        // Set for each draw from whether the list sits in another's item: where the handles sit, how wide their column is, and where the body starts.
        private float handleInset;
        private float handleColumn;
        private int bodyInset;
        private GUIStyle content;

        /// <param name="items">The list's own property, whose children are its items.</param>
        /// <param name="emptyHint">Shown grayed out while the list has no item.</param>
        /// <param name="move">Moves the item at the first index to the second, in every selected value; run on the next repaint.</param>
        /// <param name="remove">Removes the item at the index, in every selected value; run on the next repaint.</param>
        /// <param name="isFramed">
        /// Draws the list's dark background and its border, as under Odin's list title bar, with the items reaching the border,<br/>
        /// but no top border: the title bar's bottom one is it, where Odin draws a second one over it. False for a list inside an item of another.
        /// </param>
        public void Draw(InspectorProperty items, string emptyHint, Action<int, int> move, Action<int> remove, bool isFramed) {
            // The rows below the first start at the body's inset; above, below and on the right, one padding all around.
            // The padding is the content's, where a field's own margin of 2 or 3 merges into it instead of adding to it,
            // so a field and a list inside start at the same place, and a field's value column lines up with one's outside the list.
            itemStyle ??= new GUIStyle(GUIStyle.none);
            contentStyle ??= new GUIStyle { padding = new RectOffset(BODY_INSET, ITEM_PADDING, ITEM_PADDING, ITEM_PADDING) };
            nestedContentStyle ??= new GUIStyle { padding = new RectOffset(NESTED_BODY_INSET, ITEM_PADDING, ITEM_PADDING, ITEM_PADDING) };

            var isNested = slots.Count > 0;
            handleInset = isNested ? NESTED_HANDLE_INSET : HANDLE_INSET;
            handleColumn = isNested ? NESTED_HANDLE_INSET + HANDLE_SIZE : HANDLE_COLUMN;
            bodyInset = isNested ? NESTED_BODY_INSET : BODY_INSET;
            content = isNested ? nestedContentStyle : contentStyle;

            var count = items.Children.Count;
            listId = GUIUtility.GetControlID(FocusType.Passive);
            if (dragIndex >= count) dragIndex = -1;
            HandleDrag(items, count, move);

            // The background, the items' and the border are drawn on whole pixels, edge to edge: as Odin draws them, a row of pixels may be left between two items.
            var frame = SirenixEditorGUI.BeginVerticalList(false, false);
            if (isFramed && Event.current.type is EventType.Repaint) SirenixEditorGUI.DrawSolidRect(PixelUtils.Snap(frame), SirenixGUIStyles.ListItemDragBgColor);

            if (count == 0) {
                // Where an item's first row would start.
                var empty = BeginItem(0);
                GUILayout.BeginVertical(content);
                var hint = EditorGUILayout.GetControlRect();
                hint.xMin = empty.x + handleColumn;
                GUI.Label(hint, emptyHint, SirenixGUIStyles.LeftAlignedGreyMiniLabel);
                GUILayout.EndVertical();
                SirenixEditorGUI.EndListItem();
                EndFrame();
                return;
            }

            while (itemRects.Count < count) itemRects.Add(default);
            while (offsets.Count < count) offsets.Add(0f);
            Glide(count);

            for (var i = 0; i < count; i++) {
                if (i == dragIndex) {
                    // The dragged item's place stays laid out but empty, its own colors cleared; the item is drawn over the list below.
                    SirenixEditorGUI.BeginListItem(false, itemStyle, Color.clear, Color.clear, Color.clear, Color.clear);
                    GUILayoutUtility.GetRect(0f, itemRects[i].height);
                    SirenixEditorGUI.EndListItem();
                    continue;
                }

                var matrix = GUI.matrix;
                if (dragIndex >= 0) GUI.matrix = Matrix4x4.Translate(new Vector3(0f, offsets[i], 0f)) * matrix;

                var itemRect = BeginItem(i);
                DrawItem(items, new Slot(this, items, itemRect, i, move, remove));
                SirenixEditorGUI.EndListItem();

                GUI.matrix = matrix;
                if (Event.current.type is EventType.Repaint && dragIndex < 0) itemRects[i] = itemRect;
            }

            EndFrame();
            DrawDragged(items, move, remove);

            static Rect BeginItem(int index) {
                var rect = SirenixEditorGUI.BeginListItem(false, itemStyle, Color.clear, Color.clear, Color.clear, Color.clear);
                if (Event.current.type is EventType.Repaint) {
                    SirenixEditorGUI.DrawSolidRect(PixelUtils.Snap(rect), index % 2 is 0 ? SirenixGUIStyles.ListItemColorEven : SirenixGUIStyles.ListItemColorOdd);
                }

                return rect;
            }

            void EndFrame() {
                SirenixEditorGUI.EndVerticalList();
                if (isFramed && Event.current.type is EventType.Repaint) SirenixEditorGUI.DrawBorders(PixelUtils.Snap(frame), 1, 1, 0, 1);
            }
        }

        /// <summary>
        /// Puts the drag handle and the remove button of the item being drawn level with <paramref name="row"/>, its first row,<br/>
        /// and returns the row clear of them; an item's drawer calls it with its first row. Outside a list, or for any later row, it returns the row as it is.<br/>
        /// The row is placed across by the item, not by the layout: Unity's groups with no style, such as Odin's around each property, take the smallest margin<br/>
        /// of what they hold, so a field row would sit a margin further in once its item opens on a list, which has none.
        /// </summary>
        public static Rect MarkFirstRow(Rect row) {
            if (slots.Count is 0 || slots.Peek().isMarked) return row;

            var slot = slots.Peek();
            row = slot.Span(row);
            slot.Mark(row);
            return slot.Clear(row);
        }

        /// <summary>
        /// <see cref="MarkFirstRow(Rect)"/> for a row laid out as a field: split into the label column, returned as <paramref name="prefix"/>, and the value column, returned;<br/>
        /// split where the rows below split, so the value columns line up, then each kept clear of the handle and the button.
        /// </summary>
        public static Rect MarkFirstRow(Rect row, out Rect prefix) {
            if (slots.Count is 0 || slots.Peek().isMarked) return SirenixEditorGUI.PrefixRect(row, out prefix);

            var slot = slots.Peek();
            row = slot.Span(row);
            var value = SirenixEditorGUI.PrefixRect(row, out prefix);
            slot.Mark(row);
            prefix = slot.Clear(prefix);
            return slot.Clear(value);
        }

        // As Odin draws its items: out of hierarchy mode, so a foldout's arrow stays inside the item instead of in the margin.
        private void DrawItem(InspectorProperty items, Slot slot) {
            slots.Push(slot);
            GUIHelper.PushHierarchyMode(false);
            GUIHelper.PushLabelWidth(EditorGUIUtility.labelWidth - bodyInset);
            GUILayout.BeginVertical(content);
            items.Children[slot.index].Draw(GUIContent.none);
            GUILayout.EndVertical();
            GUIHelper.PopLabelWidth();
            GUIHelper.PopHierarchyMode();
            slots.Pop();

            if (!slot.isMarked) slot.Mark(new Rect(slot.itemRect.x, slot.itemRect.y + ITEM_PADDING, slot.itemRect.width, EditorGUIUtility.singleLineHeight));
        }

        // The dragged item over the list, where the pointer has taken it, in an area of its own drawn after every other item.
        private void DrawDragged(InspectorProperty items, Action<int, int> move, Action<int> remove) {
            if (dragIndex < 0) return;

            var place = itemRects[dragIndex];
            var rect = new Rect(place.x, place.y + dragOffset, place.width, place.height);
            GUILayout.BeginArea(rect);
            SirenixEditorGUI.BeginVerticalList(false, true);
            var itemRect = SirenixEditorGUI.BeginListItem(false, itemStyle);
            DrawItem(items, new Slot(this, items, itemRect, dragIndex, move, remove));
            SirenixEditorGUI.EndListItem();
            SirenixEditorGUI.EndVerticalList();
            GUILayout.EndArea();

            if (Event.current.type is EventType.Repaint) SirenixEditorGUI.DrawBorders(rect, 1, DRAG_OUTLINE);
        }

        // Pressing a handle starts the drag; dragging moves the item with the pointer and picks where it lands; letting go moves it there.
        private void HandleDrag(InspectorProperty items, int count, Action<int, int> move) {
            var current = Event.current;
            switch (current.GetTypeForControl(listId)) {
                case EventType.MouseDrag when GUIUtility.hotControl == listId && dragIndex >= 0:
                    // Kept within the list: past its ends, the item would be drawn over the fields around it, or cut off by them.
                    var place = itemRects[dragIndex];
                    dragOffset = Mathf.Clamp(current.mousePosition.y - pressY, itemRects[0].yMin - place.yMin, itemRects[count - 1].yMax - place.yMax);
                    landIndex = FindLanding(count);
                    current.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == listId:
                    GUIUtility.hotControl = 0;
                    var from = dragIndex;
                    var to = landIndex;
                    dragIndex = -1;
                    for (var i = 0; i < offsets.Count; i++) offsets[i] = 0f;
                    if (from >= 0 && to != from) Run(items, () => move(from, to));
                    current.Use();
                    break;
            }
        }

        private void StartDrag(int index) {
            GUIUtility.hotControl = listId;
            dragIndex = landIndex = index;
            pressY = Event.current.mousePosition.y;
            dragOffset = 0f;
            glideTime = EditorApplication.timeSinceStartup;
            for (var i = 0; i < offsets.Count; i++) offsets[i] = 0f;
            Event.current.Use();
        }

        // Where the dragged item lands: after every other item whose middle its own middle has passed.
        private int FindLanding(int count) {
            var center = itemRects[dragIndex].center.y + dragOffset;
            var index = 0;
            for (var i = 0; i < count; i++) {
                if (i != dragIndex && itemRects[i].center.y < center) index++;
            }

            return index;
        }

        // Each other item eases toward where it sits once the dragged item lands: a step up or down by the dragged item's height, or back home.
        private void Glide(int count) {
            if (dragIndex < 0 || Event.current.type is not EventType.Repaint) return;

            var now = EditorApplication.timeSinceStartup;
            var step = 1f - Mathf.Exp(-GLIDE_RATE * (float)(now - glideTime));
            glideTime = now;

            var height = itemRects[dragIndex].height;
            var isSettled = true;
            for (var i = 0; i < count; i++) {
                if (i == dragIndex) continue;

                var target = i > dragIndex && i <= landIndex ? -height : i < dragIndex && i >= landIndex ? height : 0f;
                offsets[i] = Mathf.Abs(target - offsets[i]) < .5f ? target : Mathf.Lerp(offsets[i], target, step);
                isSettled &= Mathf.Approximately(offsets[i], target);
            }

            if (!isSettled) InternalEditorUtility.RepaintAllViews();
        }

        // The list changes on the next repaint, after it is drawn, so its items never change midway through drawing.
        private static void Run(InspectorProperty items, Action action) {
            items.Tree.DelayActionUntilRepaint(action);
            InternalEditorUtility.RepaintAllViews();
        }

        private sealed class Slot {
            public readonly ItemList list;
            public readonly InspectorProperty items;
            public readonly Rect itemRect;
            public readonly int index;
            public readonly Action<int, int> move;
            public readonly Action<int> remove;
            public bool isMarked;

            public Slot(ItemList list, InspectorProperty items, Rect itemRect, int index, Action<int, int> move, Action<int> remove) {
                this.list = list;
                this.items = items;
                this.itemRect = itemRect;
                this.index = index;
                this.move = move;
                this.remove = remove;
            }

            // The first row across the item, from where its body starts to its padding on the right, as a field row sits in it with nothing else beside.
            public Rect Span(Rect row) {
                row.xMin = itemRect.x + list.bodyInset;
                row.xMax = itemRect.xMax - ITEM_PADDING;
                return row;
            }

            // A part of the first row, kept clear of the handle's column and the button's.
            public Rect Clear(Rect part) {
                part.xMin = Mathf.Max(part.xMin, itemRect.x + list.handleColumn);
                part.xMax = Mathf.Min(part.xMax, itemRect.xMax - REMOVE_COLUMN);
                return part;
            }

            // The handle and the button sit in the item's side padding, centered on the row.
            public void Mark(Rect row) {
                isMarked = true;

                var handle = new Rect(itemRect.x + list.handleInset, row.center.y - HANDLE_SIZE / 2f, HANDLE_SIZE, HANDLE_SIZE);
                var current = Event.current;
                if (current.type is EventType.MouseDown && current.button is 0 && handle.Contains(current.mousePosition)) list.StartDrag(index);
                if (current.type is EventType.Repaint) {
                    EditorGUIUtility.AddCursorRect(handle, MouseCursor.Pan);
                    GUIHelper.PushColor(GUI.color * HANDLE_TINT);
                    GUI.DrawTexture(handle, EditorIcons.List.Inactive);
                    GUIHelper.PopColor();
                }

                var button = new Rect(itemRect.xMax - REMOVE_INSET, row.center.y - REMOVE_SIZE / 2f, REMOVE_SIZE, REMOVE_SIZE);
                if (SirenixEditorGUI.SDFIconButton(button, null, SdfIconType.X, IconAlignment.LeftOfText, SirenixGUIStyles.IconButton)) {
                    Run(items, () => remove(index));
                }
            }
        }
    }
}
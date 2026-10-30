using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Draws a <see cref="TransitionBranch&lt;T, U&gt;"/> in place of Odin's reference header: a foldout titled with the path it follows,<br/>
    /// <typeparamref name="U"/> grayed out, and a + button adding a node on the object it reaches, as a target's row adds one;<br/>
    /// the path is picked when the branch is added, so it has no field of its own. The nodes follow as an <see cref="ItemList"/>.<br/>
    /// When the path cannot be followed, the title is red and an error says why: none picked, a member missing on the way (renamed or removed since),<br/>
    /// or an end of another type.
    /// </summary>
    [DrawerPriority(0, 0, 2001)]
    public sealed class TransitionBranchDrawer<T, U> : OdinValueDrawer<TransitionBranch<T, U>> where T : class where U : class {
        private const float ADD_WIDTH = 18f;
        private const float TYPE_GAP = 4f;

        private static readonly GUIContent TYPE_TEXT = new(typeof(U).GetNiceName());
        private static readonly Color ERROR_COLOR = new(1f, .4f, .35f);

        private static GUIStyle typeStyle;

        private readonly ItemList list = new();

        protected override void DrawPropertyLayout(GUIContent label) {
            var path = ValueEntry.SmartValue.path;
            var error = FindError(path);

            DrawHeader(new GUIContent(string.IsNullOrEmpty(path) ? "None" : path), error != null);

            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded)) {
                if (error != null) SirenixEditorGUI.ErrorMessageBox(error);

                list.Draw(Property.Children[nameof(TransitionBranch<T, U>.nodes)].Children[0], "Press + to add a property or a branch.", Move, Remove, false);
                NodeUtils.DrawDuplicates(ValueEntry.SmartValue.nodes);
            }

            SirenixEditorGUI.EndFadeGroup();
        }

        private void DrawHeader(GUIContent title, bool isBroken) {
            typeStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, normal = { textColor = SirenixGUIStyles.LeftAlignedGreyLabel.normal.textColor } };

            var row = ItemList.MarkFirstRow(EditorGUILayout.GetControlRect());
            var add = row.TakeFromRight(ADD_WIDTH);
            row.xMax -= TYPE_GAP;
            var type = row.TakeFromRight(typeStyle.CalcSize(TYPE_TEXT).x);
            GUI.Label(type, TYPE_TEXT, typeStyle);
            if (ButtonUtils.IconButton(add, SdfIconType.Plus)) ShowAdd(add);

            if (isBroken) GUIHelper.PushContentColor(ERROR_COLOR);
            Property.State.Expanded = SirenixEditorGUI.Foldout(ref row, GUIUtility.GetControlID(FocusType.Passive, row), title, Property.State.Expanded, true);
            if (isBroken) GUIHelper.PopContentColor();
        }

        private static string FindError(string path) {
            if (string.IsNullOrEmpty(path)) return "No path is picked.";

            var hops = MemberAccess.FindPath(typeof(T), path);
            if (hops == null) return $"{typeof(T).GetNiceName()} has no path \"{path}\" of public properties with a public get; the branch is skipped.";
            if (hops[^1].PropertyType != typeof(U)) return $"{typeof(T).GetNiceName()}.{path} is {hops[^1].PropertyType.GetNiceName()}, not {typeof(U).GetNiceName()}; the branch is skipped.";

            return null;
        }

        private void ShowAdd(Rect button) {
            NodeUtils.ShowAdd(button, typeof(U), NodeUtils.ListWritten(ValueEntry.SmartValue.nodes), make => NodeUtils.Run(Property, () => NodeUtils.Add(Property, value => (TransitionBranch<T, U>)value, make)));
        }

        private void Move(int from, int to) {
            NodeUtils.Move(Property, value => ((TransitionBranch<T, U>)value).nodes, from, to);
        }

        private void Remove(int index) {
            NodeUtils.Remove(Property, value => ((TransitionBranch<T, U>)value).nodes, index);
        }
    }
}
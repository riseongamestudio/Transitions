using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Draws a <see cref="TransitionProperty&lt;T, V&gt;"/> in place of Odin's reference header: a foldout titled with the member it writes,<br/>
    /// <typeparamref name="V"/> grayed out at the end of the row, then its fields; the member is picked when the property is added, so it has no field of its own.<br/>
    /// A rest value typed in has a Capture button beside it, filling in the member's value on the object the property writes now:<br/>
    /// its target's object, an unbound target's preview object, or what a branch reaches; in play mode, the rest value a transition keeps for it, once one wrote it.<br/>
    /// With no such object, the button is left out.<br/>
    /// When the member cannot be written, the title is red and an error says why: none picked, none of that name on <typeparamref name="T"/><br/>
    /// (renamed or removed since), or one of another type.
    /// </summary>
    [DrawerPriority(0, 0, 2001)]
    public sealed class TransitionPropertyDrawer<T, V> : OdinValueDrawer<TransitionProperty<T, V>> where T : class {
        private static readonly GUIContent TYPE_TEXT = new(typeof(V).GetNiceName());
        private static readonly Color ERROR_COLOR = new(1f, .4f, .35f);
        private static readonly GUIContent CAPTURE_TEXT = new("Capture", "Fill in the value the member has now on the object, or the rest value a transition keeps for it in play mode.");

        private static GUIStyle typeStyle;

        protected override void DrawPropertyLayout(GUIContent label) {
            var member = ValueEntry.SmartValue.member;
            var error = FindError(member);

            DrawHeader(new GUIContent(string.IsNullOrEmpty(member) ? "None" : member), error != null);

            // Not indented: the item already sets its fields apart, starting them under the middle of its handle.
            if (SirenixEditorGUI.BeginFadeGroup(this, Property.State.Expanded)) {
                if (error != null) SirenixEditorGUI.ErrorMessageBox(error);

                foreach (var child in Property.Children) {
                    if (child.Name == nameof(TransitionProperty<T, V>.rest)) DrawRest(child);
                    else child.Draw();
                }
            }

            SirenixEditorGUI.EndFadeGroup();
        }

        private void DrawHeader(GUIContent title, bool isBroken) {
            typeStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight, normal = { textColor = SirenixGUIStyles.LeftAlignedGreyLabel.normal.textColor } };

            var row = ItemList.MarkFirstRow(EditorGUILayout.GetControlRect());
            var type = row.TakeFromRight(typeStyle.CalcSize(TYPE_TEXT).x);
            GUI.Label(type, TYPE_TEXT, typeStyle);

            if (isBroken) GUIHelper.PushContentColor(ERROR_COLOR);
            Property.State.Expanded = SirenixEditorGUI.Foldout(ref row, GUIUtility.GetControlID(FocusType.Passive, row), title, Property.State.Expanded, true);
            if (isBroken) GUIHelper.PopContentColor();
        }

        private void DrawRest(InspectorProperty rest) {
            var source = ValueEntry.ValueCount is 1 && ValueEntry.SmartValue.customRest ? NodeUtils.FindObject(Property) : null;
            if (source == null) {
                rest.Draw();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            rest.Draw();
            EditorGUILayout.EndVertical();
            if (GUILayout.Button(CAPTURE_TEXT, SirenixGUIStyles.Button, GUILayout.ExpandWidth(false), GUILayout.Height(EditorGUIUtility.singleLineHeight))) Capture(rest, source);
            EditorGUILayout.EndHorizontal();
        }

        private void Capture(InspectorProperty rest, object source) {
            var member = MemberAccess.Find(typeof(T), ValueEntry.SmartValue.member);
            if (member == null || member.PropertyType != typeof(V)) return;

            rest.ValueEntry.WeakSmartValue = Application.isPlaying && TransitionRestHub.TryGetRest<V>(source, member.Name, out var kept) ? kept : (V)member.GetValue(source);
        }

        private static string FindError(string member) {
            if (string.IsNullOrEmpty(member)) return "No member is picked.";

            var found = MemberAccess.Find(typeof(T), member);
            if (found == null) return $"{typeof(T).GetNiceName()} has no public property \"{member}\" with a public get and set; it is skipped.";
            if (found.PropertyType != typeof(V)) return $"{typeof(T).GetNiceName()}.{member} is {found.PropertyType.GetNiceName()}, not {typeof(V).GetNiceName()}; it is skipped.";

            return null;
        }
    }
}
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Odin's button with only an icon, framed as <see cref="SirenixEditorGUI.SDFIconButton(Rect, SdfIconType, GUIStyle)"/> frames it, but with the icon in its very center:<br/>
    /// Odin lays it out as if beside a label, which leaves it a pixel or so off. The icon is colored as the text of the fields around it, lit up under the pointer.
    /// </summary>
    internal static class ButtonUtils {
        // The share of the button's height the icon takes.
        private const float ICON_SCALE = .6f;

        // The button under the pointer, by its rect on the screen; none when the pointer is over no button.
        private static Rect hovered;

        public static bool IconButton(Rect rect, SdfIconType icon) {
            // Worked out in the screen's pixels, as the scroll and the groups around may leave the GUI's origin between two of them:
            // the frame on whole pixels, and the icon on whole pixels too, as many pixels in from each side, so its middle is the frame's on any row.
            var scale = EditorGUIUtility.pixelsPerPoint;
            var origin = GUIUtility.GUIToScreenPoint(Vector2.zero) * scale;
            var x = Mathf.Round(rect.x * scale + origin.x);
            var y = Mathf.Round(rect.y * scale + origin.y);
            var width = Mathf.Round(rect.width * scale);
            var height = Mathf.Round(rect.height * scale);
            var size = Mathf.Round(height * ICON_SCALE);
            if ((width - size) % 2f != 0f) size--;

            rect = ToGUI(x, y, width, height);
            var isClicked = GUI.Button(rect, GUIContent.none, SirenixGUIStyles.Button);
            var isHovered = TrackHover(rect);
            if (Event.current.type is not EventType.Repaint) return isClicked;

            var color = isHovered ? SirenixGUIStyles.Button.hover.textColor : EditorStyles.label.normal.textColor;
            SdfIcons.DrawIcon(ToGUI(x + (width - size) / 2f, y + Mathf.Floor((height - size) / 2f), size, size), icon, color);
            return isClicked;

            Rect ToGUI(float pixelX, float pixelY, float pixelWidth, float pixelHeight) {
                return new Rect((pixelX - origin.x) / scale, (pixelY - origin.y) / scale, pixelWidth / scale, pixelHeight / scale);
            }
        }

        // Whether the pointer is over the button. A button lit under the pointer only would stay lit, as the Inspector does not repaint
        // on its own as the pointer moves in or out, so it is asked to.
        private static bool TrackHover(Rect rect) {
            var isHovered = rect.Contains(Event.current.mousePosition);
            var screen = GUIUtility.GUIToScreenRect(rect);
            if (Event.current.type is EventType.MouseMove && isHovered != (hovered == screen)) {
                hovered = isHovered ? screen : default;
                GUIHelper.RequestRepaint();
            }

            return isHovered;
        }
    }
}
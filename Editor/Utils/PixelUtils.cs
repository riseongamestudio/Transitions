using UnityEditor;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Rects on the screen's pixels, for what is drawn edge to edge: Unity rounds where a rect starts and how big it is apart,<br/>
    /// so at a display scale such as 125% or 150%, two rects that meet can leave a row of pixels between them, or overlap by one.
    /// </summary>
    internal static class PixelUtils {
        /// <summary>
        /// <paramref name="rect"/> with each of its edges rounded to the nearest pixel of the screen, so rects that meet in points meet in pixels too.<br/>
        /// Rounded on the screen, as the scroll and the groups around may leave the GUI's origin between two pixels.
        /// </summary>
        public static Rect Snap(Rect rect) {
            var scale = EditorGUIUtility.pixelsPerPoint;
            var origin = GUIUtility.GUIToScreenPoint(Vector2.zero);
            return Rect.MinMaxRect(Round(rect.xMin, origin.x), Round(rect.yMin, origin.y), Round(rect.xMax, origin.x), Round(rect.yMax, origin.y));

            float Round(float value, float offset) {
                return Mathf.Round((value + offset) * scale) / scale - offset;
            }
        }
    }
}
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The clock arithmetic shared by the sources.
    /// </summary>
    internal static class TransitionUtils {
        /// <summary>
        /// Stretches the part of the clock from <paramref name="start"/> to <paramref name="end"/> over 0 to 1, holding 0 before it and 1 after it.
        /// </summary>
        public static float Remap(float t, float start, float end) {
            if (end <= start) return t < start ? 0f : 1f;

            return Mathf.Clamp01((t - start) / (end - start));
        }

        public static float Mirror(float t) {
            return 1f - Mathf.Abs(2f * t - 1f);
        }

        public static float Repeat(float t, int count) {
            return t >= 1f ? 1f : t * count % 1f;
        }
    }
}
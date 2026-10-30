using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// Reports a configuration error once per owner instead of every frame; a broken part is skipped and the rest keeps running.
    /// </summary>
    internal static class TransitionLog {
        public static void Once(ref bool isReported, string message) {
            if (isReported) return;

            isReported = true;
            Debug.LogError("RiseOn Transitions: " + message);
        }
    }
}
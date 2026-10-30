using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// Plays transitions on the objects they target for the Inspector's preview, and puts every object back exactly as it was when the preview ends:<br/>
    /// each member the transitions write is read before the first frame and set back after, whatever <c>t</c> = 1 gives,<br/>
    /// so a transition that does not end where it began, or a rest value typed in by hand, leaves nothing behind.<br/>
    /// It ends when its owner ends it, and also when anything cuts it short: a script reload, a change of play mode, or a save, before anything is written,<br/>
    /// so no saved scene, prefab or asset ever holds a previewed value. Out of play mode, the rest values the preview read are forgotten too.<br/>
    /// One preview runs at a time, as there is one pointer to hold a slider with; a new one ends the one before.
    /// </summary>
    internal static class TransitionPreview {
        private static readonly List<(object Instance, PropertyInfo Member, object Value)> saved = new();
        private static Action<float> apply;
        private static object owner;

        /// <param name="by">The drawer previewing, which alone can apply or end the preview.</param>
        /// <param name="writes">Every member <paramref name="play"/> writes, read now to be set back.</param>
        public static void Begin(object by, Action<float> play, IEnumerable<(object Instance, PropertyInfo Member)> writes) {
            End();
            owner = by;
            apply = play;
            foreach (var (instance, member) in writes) saved.Add((instance, member, member.GetValue(instance)));

            AssemblyReloadEvents.beforeAssemblyReload += End;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static void Apply(object by, float t) {
            if (by != owner) return;

            apply(t);
            Repaint();
        }

        /// <summary>Ends the preview <paramref name="by"/> began; one begun by another stays.</summary>
        public static void End(object by) {
            if (by == owner) End();
        }

        // Set back in reverse, so a member written by two targets ends at the value read first, before either wrote it.
        // Each on its own, so one that fails, such as a setter that throws, leaves the others set back.
        private static void End() {
            if (owner == null) return;

            for (var i = saved.Count - 1; i >= 0; i--) {
                var (instance, member, value) = saved[i];
                if (instance is UnityEngine.Object unityObject && unityObject == null) continue;

                try {
                    member.SetValue(instance, value);
                } catch (Exception exception) {
                    Debug.LogError($"RiseOn Transitions: the preview could not set {member.DeclaringType?.Name}.{member.Name} back: {exception.GetBaseException().Message}");
                }
            }

            if (!Application.isPlaying) TransitionRestHub.Clear();

            saved.Clear();
            apply = null;
            owner = null;
            AssemblyReloadEvents.beforeAssemblyReload -= End;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Repaint();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change) {
            End();
        }

        private static void Repaint() {
            SceneView.RepaintAll();
            InternalEditorUtility.RepaintAllViews();
        }

        // Before any save, scenes, Prefab Mode's prefab and assets alike, the auto-save of Prefab Mode included.
        private sealed class SaveGuard : AssetModificationProcessor {
            private static string[] OnWillSaveAssets(string[] paths) {
                End();
                return paths;
            }
        }
    }
}
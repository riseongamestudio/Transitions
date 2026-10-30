using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// What the Inspector offers for targets. The types an object can be targeted as: its own type, each base class that declares<br/>
    /// a member worth writing or one a branch can follow, and each interface with one; a GameObject offers itself and, in a folder each, its components.<br/>
    /// The objects a transition can target from where it is: every GameObject of its scene, or of its prefab, a folder each.<br/>
    /// The members worth writing: the writable properties of the type, without those of <see cref="Object"/> and <see cref="Component"/>,<br/>
    /// such as <c>name</c> and <c>hideFlags</c>, which no transition animates.
    /// </summary>
    internal static class TargetTypeUtils {
        private static readonly Type[] ROOTS = { typeof(Object), typeof(Component), typeof(ScriptableObject) };

        /// <summary>
        /// The object to target and the type to target it as, for each type <paramref name="source"/> offers, under <paramref name="prefix"/>.
        /// </summary>
        public static IEnumerable<SearchChoice<(Object Holder, Type Type)>> ListChoices(Object source, string prefix) {
            var choices = ListChain(source, prefix);
            if (source is not GameObject gameObject) return choices;

            return choices.Concat(gameObject.GetComponents<Component>()
                .Where(component => component != null)
                .SelectMany(component => ListChain(component, prefix, component.GetType().GetNiceName())));

            // The chain of a component goes in a folder named after it, showing its icon, as the Hierarchy and the Inspector show it;
            // one offering its own type alone goes without, as the folder would hold that type alone, named as the folder is.
            static IEnumerable<SearchChoice<(Object, Type)>> ListChain(Object holder, string prefix, string folder = null) {
                var types = ListTypes(holder.GetType()).ToList();
                var path = folder == null || types.Count is 1 ? prefix : $"{prefix}{folder}/";
                return types.Select(type => new SearchChoice<(Object, Type)>(path + type.GetNiceName(), (holder, type),
                    icon: () => GetIcon(type), folderIcon: () => GetObjectIcon(holder)));
            }
        }

        /// <summary>
        /// The types the GameObject of <paramref name="target"/>'s object offers, as <see cref="ListChoices"/> lists them, each grayed out unless<br/>
        /// every property of <paramref name="target"/> keeps working on it: the one in use, and those missing a member or not fitting a modifier, with why.
        /// </summary>
        public static IEnumerable<SearchChoice<(Object Holder, Type Type)>> ListReplaceChoices(ITransitionTarget target) {
            var source = ((IUntypedTarget)target).Object;
            return ListChoices(source is Component component ? component.gameObject : source, string.Empty)
                .Select(choice => new SearchChoice<(Object, Type)>(choice.Path, choice.Value, FindBreak(choice.Value.Type, choice.Value.Holder), choice.Icon, choice.LabelSearch, choice.FolderIcon));

            string FindBreak(Type type, Object holder) {
                if (type == target.TargetType && holder == source) return "current";

                var lost = FindLost(target, type);
                return lost.Count is 0 ? null : $"loses {string.Join(", ", lost)}";
            }
        }

        /// <summary>
        /// The names of <paramref name="target"/>'s nodes that would not keep working on <paramref name="type"/>: the members and paths<br/>
        /// <paramref name="type"/> lacks or has of another type, and those whose modifier does not work on <paramref name="type"/>.
        /// </summary>
        public static List<string> FindLost(ITransitionTarget target, Type type) {
            var lost = new List<string>();
            foreach (var node in ((IUntypedTarget)target).Nodes) {
                if (node is IUntypedNode moving && moving.Retarget(type) == null) lost.Add(moving.Name);
            }

            return lost;
        }

        /// <summary>
        /// Every GameObject beside <paramref name="owner"/>, in hierarchy order, a folder each holding the types it offers: the GameObjects<br/>
        /// of the prefab it belongs to (open in Prefab Mode or as an asset), or else of its scene. A name worn by several GameObjects<br/>
        /// is told apart by the path of its parent. The search window shows the folders above a result, such as <c>Counter › CanvasGroup</c>.
        /// </summary>
        public static IEnumerable<SearchChoice<(Object Holder, Type Type)>> ListHierarchyChoices(Object owner) {
            var gameObjects = ListRoots(owner).SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(transform => transform.gameObject).ToList();
            var shared = gameObjects.GroupBy(gameObject => gameObject.name).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();

            foreach (var gameObject in gameObjects) {
                var folder = shared.Contains(gameObject.name) ? $"{gameObject.name} ({GetPath(gameObject.transform.parent)})" : gameObject.name;
                foreach (var choice in ListChoices(gameObject, $"{folder.Replace('/', '∕')}/")) yield return choice;
            }

            static IEnumerable<GameObject> ListRoots(Object owner) {
                var gameObject = owner switch {
                    GameObject itself => itself,
                    Component component => component.gameObject,
                    _ => null
                };
                if (gameObject == null) return Enumerable.Empty<GameObject>();

                var stage = PrefabStageUtility.GetPrefabStage(gameObject);
                if (stage != null) return new[] { stage.prefabContentsRoot };
                if (EditorUtility.IsPersistent(gameObject)) return new[] { gameObject.transform.root.gameObject };

                return gameObject.scene.GetRootGameObjects();
            }

            static string GetPath(Transform transform) {
                return transform == null ? "root" : string.Join(" › ", transform.GetComponentsInParent<Transform>(true).Reverse().Select(parent => parent.name));
            }
        }

        /// <summary>
        /// Unity's own icon for its object types, such as a component's; an interface or a plain class gets the one Unity marks types with,<br/>
        /// as on the Project window's Search by Type button, so every row of a list has an icon.
        /// </summary>
        public static Texture2D GetIcon(Type type) {
            var icon = typeof(Object).IsAssignableFrom(type) ? AssetPreview.GetMiniTypeThumbnail(type) : null;
            return icon != null ? icon : (Texture2D)EditorGUIUtility.IconContent("FilterByType").image;
        }

        // A GameObject's icon as the Hierarchy shows it, a prefab instance's included; any other object's as the Inspector shows it.
        private static Texture2D GetObjectIcon(Object source) {
            return source is GameObject gameObject ? PrefabUtility.GetIconForGameObject(gameObject) : AssetPreview.GetMiniThumbnail(source);
        }

        public static IEnumerable<PropertyInfo> ListMembers(Type type) {
            return MemberAccess.List(type).Where(member => !ROOTS.Contains(member.DeclaringType));
        }

        /// <summary>
        /// A new <see cref="TransitionProperty{T, V}"/> writing <paramref name="member"/> of <paramref name="target"/>, with no modifier yet.
        /// </summary>
        public static object CreateProperty(Type target, PropertyInfo member) {
            return Activator.CreateInstance(typeof(TransitionProperty<,>).MakeGenericType(target, member.PropertyType), member.Name, null);
        }

        /// <summary>
        /// A new <see cref="TransitionBranch{T, U}"/> following <paramref name="path"/> from <paramref name="target"/> to <paramref name="reached"/>, with no node yet.
        /// </summary>
        public static object CreateBranch(Type target, string path, Type reached) {
            return Activator.CreateInstance(typeof(TransitionBranch<,>).MakeGenericType(target, reached), path);
        }

        /// <summary>
        /// A new target of <paramref name="type"/> on <paramref name="holder"/>, named as <paramref name="old"/>, carrying its nodes over as they are:<br/>
        /// each property whose member <paramref name="type"/> has, of the same value type, and whose modifier works on <paramref name="type"/>, as any does on a type deriving<br/>
        /// from the old one, and each branch whose path <paramref name="type"/> has, to the same type. The names of those left out come back in <paramref name="dropped"/>; empty slots are kept.
        /// </summary>
        public static ITransitionTarget Retarget(ITransitionTarget old, Type type, Object holder, out List<string> dropped) {
            var from = (IUntypedTarget)old;
            var copy = Create(type, holder, from.Label);
            var to = (IUntypedTarget)copy;

            dropped = new List<string>();
            foreach (var node in from.Nodes) {
                if (node is not IUntypedNode moving) {
                    to.TryAdd(null);
                    continue;
                }

                var moved = moving.Retarget(type);
                if (moved == null || !to.TryAdd(moved)) dropped.Add(moving.Name);
            }

            return copy;
        }

        /// <param name="label">Null leaves the target named after its object.</param>
        public static ITransitionTarget Create(Type type, Object holder, string label = null) {
            return (ITransitionTarget)Activator.CreateInstance(typeof(TransitionTarget<>).MakeGenericType(type), holder, label);
        }

        private static IEnumerable<Type> ListTypes(Type type) {
            yield return type;

            for (var parent = type.BaseType; parent != null && !ROOTS.Contains(parent); parent = parent.BaseType) {
                if (ListMembers(parent).Concat(MemberAccess.ListReadable(parent)).Any(member => member.DeclaringType == parent)) yield return parent;
            }

            foreach (var contract in type.GetInterfaces().OrderBy(contract => contract.GetNiceName(), StringComparer.Ordinal)) {
                if (ListMembers(contract).Any() || MemberAccess.ListReadable(contract).Any()) yield return contract;
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Transitions.Editor {
    /// <summary>
    /// What the Inspector does the same on every list of nodes: that of a target, a branch, or an unbound target.<br/>
    /// Adding one picks it in one window listing, in a folder per type declaring them, both kinds of node:<br/>
    /// a property writing a member worth writing, those already written grayed out; and a branch following a member to an object,<br/>
    /// then step by step on from the type it reaches until Branch Here. Every row shows the icon of the member's type, named in brackets after it.
    /// </summary>
    internal static class NodeUtils {
        /// <summary>
        /// Opens the window picking a node for a list of nodes of <paramref name="type"/> under <paramref name="button"/>, None first;<br/>
        /// <paramref name="add"/> gets a maker of the node picked, called once for each value the list is drawn for, null for None.
        /// </summary>
        public static void ShowAdd(Rect button, Type type, ICollection<string> written, Action<Func<object>> add) {
            ChoiceSearchWindow window = null;
            var choices = ListChoices(type, written).Prepend(SearchChoice<Pick>.None());

            window = ChoiceSearchWindow.Open(button, "Node", choices, (Pick picked) => {
                if (picked == null) add(() => null);
                else if (picked.IsBranch) Follow(new List<PropertyInfo> { picked.Member });
                else add(() => TargetTypeUtils.CreateProperty(type, picked.Member));
            });

            // Each step lists Branch Here first, ending the path at the type reached, then the members going on from it.
            void Follow(List<PropertyInfo> hops) {
                var reached = hops[^1].PropertyType;
                var path = string.Join(MemberAccess.PATH_SEPARATOR.ToString(), hops.Select(hop => hop.Name));
                var steps = ListChoices(reached, null)
                    .Prepend(new SearchChoice<Pick>($"Branch Here   ({reached.GetNiceName()})", new Pick(null, true), icon: () => TargetTypeUtils.GetIcon(reached), labelSearch: "Branch Here"));

                window.Present($"{type.GetNiceName()}.{path}", steps, (Pick next) => {
                    if (next.Member == null) {
                        add(() => TargetTypeUtils.CreateBranch(type, path, reached));
                        return;
                    }

                    hops.Add(next.Member);
                    Follow(hops);
                });
            }
        }

        // The members of type a node starts from, by name, a property before a branch of the same name; the properties left out when written is null,
        // as on a branch's later steps. A folder per declaring type, named and iconed after it, unless one type declares them all.
        private static IEnumerable<SearchChoice<Pick>> ListChoices(Type type, ICollection<string> written) {
            var properties = written == null ? Enumerable.Empty<Pick>() : TargetTypeUtils.ListMembers(type).Select(member => new Pick(member, false));
            var picks = properties
                .Concat(MemberAccess.ListReadable(type).Select(member => new Pick(member, true)))
                .OrderBy(pick => pick.Member.Name, StringComparer.Ordinal)
                .ThenBy(pick => pick.IsBranch)
                .ToList();
            var isFoldered = picks.Select(pick => pick.Member.DeclaringType).Distinct().Skip(1).Any();

            return picks.Select(pick => {
                var member = pick.Member;
                var folder = isFoldered ? $"{member.DeclaringType.GetNiceName()}/" : string.Empty;
                var disabled = !pick.IsBranch && written.Contains(member.Name) ? "already written" : null;
                return new SearchChoice<Pick>($"{folder}{member.Name}   ({member.PropertyType.GetNiceName()})", pick, disabled,
                    () => TargetTypeUtils.GetIcon(member.PropertyType), folderIcon: () => TargetTypeUtils.GetIcon(member.DeclaringType));
            });
        }

        /// <summary>
        /// The members the properties of <paramref name="nodes"/> write, those of empty slots and branches left out.
        /// </summary>
        public static HashSet<string> ListWritten(IEnumerable nodes) {
            return nodes.OfType<IUntypedNode>().Where(node => node is not IUntypedBranch).Select(node => node.Name).ToHashSet();
        }

        /// <summary>
        /// Warns of the members written by more than one property of <paramref name="nodes"/>: the last one wins.
        /// </summary>
        public static void DrawDuplicates(IEnumerable nodes) {
            var duplicates = nodes
                .OfType<IUntypedNode>()
                .Where(node => node is not IUntypedBranch && !string.IsNullOrEmpty(node.Name))
                .GroupBy(node => node.Name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicates.Count > 0) SirenixEditorGUI.WarningMessageBox($"{string.Join(", ", duplicates)}: written by more than one property, so the last one wins.");
        }

        /// <summary>
        /// The object the nodes of the list holding <paramref name="property"/> write on now, as the Inspector sees it:<br/>
        /// a target's object, an unbound target's preview object, or what a branch reaches from that of the list holding it; null when there is none.
        /// </summary>
        public static object FindObject(InspectorProperty property) {
            for (var parent = property.Parent; parent != null; parent = parent.Parent) {
                switch (parent.ValueEntry?.WeakSmartValue) {
                    case IUntypedTarget target:
                        return target.Object;
                    case IUntypedBranch branch:
                        var from = FindObject(parent);
                        return from == null ? null : branch.Reach(from);
                    case IUntypedNodes:
                        return UnboundPreview.Get(parent);
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a node made by <paramref name="make"/> to the list <paramref name="nodes"/> gives of each value of <paramref name="property"/>.
        /// </summary>
        public static void Add(InspectorProperty property, Func<object, IUntypedNodes> nodes, Func<object> make) {
            property.RecordForUndo("Add Transition Node", true);
            for (var i = 0; i < property.ValueEntry.ValueCount; i++) nodes(property.ValueEntry.WeakValues[i]).TryAdd(make());

            property.ValueEntry.WeakValues.ForceMarkDirty();
            property.State.Expanded = true;
        }

        public static void Move<TNode>(InspectorProperty property, Func<object, IList<TNode>> nodes, int from, int to) {
            property.RecordForUndo("Move Transition Node", true);
            for (var i = 0; i < property.ValueEntry.ValueCount; i++) {
                var list = nodes(property.ValueEntry.WeakValues[i]);
                if (from >= list.Count || to >= list.Count) continue;

                var node = list[from];
                list.RemoveAt(from);
                list.Insert(to, node);
            }

            property.ValueEntry.WeakValues.ForceMarkDirty();
        }

        public static void Remove<TNode>(InspectorProperty property, Func<object, IList<TNode>> nodes, int index) {
            property.RecordForUndo("Remove Transition Node", true);
            for (var i = 0; i < property.ValueEntry.ValueCount; i++) {
                var list = nodes(property.ValueEntry.WeakValues[i]);
                if (index < list.Count) list.RemoveAt(index);
            }

            property.ValueEntry.WeakValues.ForceMarkDirty();
        }

        /// <summary>
        /// Window picks and menu items run outside any drawing; the change waits for the Inspector's next repaint, made to happen now.
        /// </summary>
        public static void Run(InspectorProperty property, Action action) {
            property.Tree.DelayActionUntilRepaint(action);
            InternalEditorUtility.RepaintAllViews();
        }

        // A member picked: one a property writes, or one a branch follows; a branch's step of its own with no member is Branch Here.
        private sealed class Pick {
            public readonly PropertyInfo Member;
            public readonly bool IsBranch;

            public Pick(PropertyInfo member, bool isBranch) {
                Member = member;
                IsBranch = isBranch;
            }
        }
    }
}
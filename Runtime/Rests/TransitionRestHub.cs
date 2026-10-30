using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Transitions {
    /// <summary>
    /// What transitions keep per object and member, made on the first write: the access to it, and for a property, its rest value.<br/>
    /// A member has one rest value, the value it had before any transition first wrote it, however many transitions write it, kept while its object lives;<br/>
    /// the access is kept with it, so one transition written on many objects binds each once.<br/>
    /// Slots are grouped by object, and a sweep on the main thread checks one object a frame, in turn, dropping those gone:<br/>
    /// a Unity object once destroyed; any other object once collected, as the hub holds it weakly, the access included, so it never keeps one alive.
    /// </summary>
    internal static class TransitionRestHub {
        private static readonly List<Row> rows = new();
        private static readonly Dictionary<Key, int> indices = new();

        // The row the sweep checks next.
        private static int next;

        /// <summary>
        /// The access to <paramref name="member"/> on <paramref name="owner"/> and its rest value, read now when nothing wrote it yet.
        /// </summary>
        public static PropertySlot<V> GetProperty<V>(object owner, PropertyInfo member) {
            var slots = GetRow(owner).Slots;
            for (var i = 0; i < slots.Count; i++) {
                if (slots[i] is PropertySlot<V> slot && slot.Member == member.Name) return slot;
            }

            var created = new PropertySlot<V>(owner, member);
            slots.Add(created);
            return created;
        }

        /// <summary>
        /// Reads <paramref name="member"/>, one a branch follows, on <paramref name="owner"/>.
        /// </summary>
        public static object Follow(object owner, PropertyInfo member) {
            var slots = GetRow(owner).Slots;
            for (var i = 0; i < slots.Count; i++) {
                if (slots[i] is HopSlot slot && slot.Member == member.Name) return slot.Get(owner);
            }

            var created = new HopSlot(owner, member);
            slots.Add(created);
            return created.Get(owner);
        }

        /// <summary>
        /// The rest value of <paramref name="member"/> on <paramref name="owner"/>, when a transition has written it.
        /// </summary>
        public static bool TryGetRest<V>(object owner, string member, out V rest) {
            if (owner != null && indices.TryGetValue(Key.Of(owner), out var index)) {
                foreach (var slot in rows[index].Slots) {
                    if (slot is not PropertySlot<V> property || property.Member != member) continue;

                    rest = property.Rest;
                    return true;
                }
            }

            rest = default;
            return false;
        }

        /// <summary>
        /// Forgets every slot, such as after the Editor's preview, whose writes are all set back.
        /// </summary>
        public static void Clear() {
            rows.Clear();
            indices.Clear();
            next = 0;
        }

        private static Row GetRow(object owner) {
            if (indices.TryGetValue(Key.Of(owner), out var index)) return rows[index];

            var row = new Row(Key.Held(owner));
            indices.Add(row.Key, rows.Count);
            rows.Add(row);
            return row;
        }

        // Rows only come in between two steps, so the sweep never misses one, nor checks one twice in a step.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartSweep() {
            Clear();
            SweepAsync(Application.exitCancellationToken).Forget();

            static async UniTaskVoid SweepAsync(CancellationToken ct) {
                while (!await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) {
                    if (rows.Count is 0) continue;
                    if (next >= rows.Count) next = 0;

                    if (rows[next].Key.IsGone) RemoveAt(next);
                    else next++;
                }
            }
        }

        // The last row takes the place of the one removed, which the sweep checks next.
        private static void RemoveAt(int index) {
            indices.Remove(rows[index].Key);

            var last = rows.Count - 1;
            if (index != last) {
                rows[index] = rows[last];
                indices[rows[index].Key] = index;
            }

            rows.RemoveAt(last);
        }

        private sealed class Row {
            public readonly Key Key;
            public readonly List<Slot> Slots = new();

            public Row(Key key) {
                Key = key;
            }
        }

        // An object as the hub knows it, by reference, as a Unity object equals any other destroyed one, which would mix their rows up.
        // A row holds a Unity object as it is, and any other object weakly; a lookup holds the object it looks for.
        // Two keys are the same object when they hold it, or are the same weak hold of one collected since.
        private readonly struct Key : IEquatable<Key> {
            private readonly object held;
            private readonly WeakReference<object> weak;
            private readonly int hash;

            private Key(object held, WeakReference<object> weak, int hash) {
                this.held = held;
                this.weak = weak;
                this.hash = hash;
            }

            public static Key Of(object owner) {
                return new Key(owner, null, RuntimeHelpers.GetHashCode(owner));
            }

            public static Key Held(object owner) {
                return owner is Object ? Of(owner) : new Key(null, new WeakReference<object>(owner), RuntimeHelpers.GetHashCode(owner));
            }

            public bool IsGone => weak == null ? held is Object unity && unity == null : !weak.TryGetTarget(out _);

            public bool Equals(Key other) {
                if (weak != null && ReferenceEquals(weak, other.weak)) return true;

                var target = Target;
                return target != null && ReferenceEquals(target, other.Target);
            }

            public override bool Equals(object obj) {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode() {
                return hash;
            }

            private object Target => weak == null ? held : weak.TryGetTarget(out var target) ? target : null;
        }

        public abstract class Slot {
            public readonly string Member;

            protected Slot(string member) {
                Member = member;
            }
        }

        public sealed class PropertySlot<V> : Slot {
            public readonly IMemberAccess<V> Access;
            public readonly V Rest;

            public PropertySlot(object owner, PropertyInfo member) : base(member.Name) {
                Access = MemberAccess.Bind<V>(owner, member);
                Rest = Access.Get(owner);
            }
        }

        private sealed class HopSlot : Slot {
            private readonly Func<object, object> getter;

            public HopSlot(object owner, PropertyInfo member) : base(member.Name) {
                getter = MemberAccess.BindReadable(owner, member);
            }

            public object Get(object owner) {
                return getter(owner);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Transitions {
    /// <summary>
    /// The named float axes of struct types, such as <c>x</c> of a <see cref="Vector2"/>, used by <see cref="AxisModifier&lt;T, TVector&gt;"/> and <see cref="AxisSource&lt;T, TVector&gt;"/>.<br/>
    /// Vectors, colors and rects come registered; register other structs with <see cref="Register&lt;TStruct&gt;"/> before anything reads an axis of them.<br/>
    /// Reading and writing go through typed delegates, so they do not allocate.
    /// </summary>
    public static class ValueAxes {
        private static readonly Dictionary<Type, string[]> NAMES = new();

        static ValueAxes() {
            string[] vector = { nameof(Vector4.x), nameof(Vector4.y), nameof(Vector4.z), nameof(Vector4.w) };

            Register<Vector2>(vector[..2], static (v, i) => v[i], static (v, i, f) => { v[i] = f; return v; });
            Register<Vector3>(vector[..3], static (v, i) => v[i], static (v, i, f) => { v[i] = f; return v; });
            Register<Vector4>(vector, static (v, i) => v[i], static (v, i, f) => { v[i] = f; return v; });
            Register<Vector2Int>(vector[..2], static (v, i) => v[i], static (v, i, f) => { v[i] = Mathf.RoundToInt(f); return v; });
            Register<Vector3Int>(vector[..3], static (v, i) => v[i], static (v, i, f) => { v[i] = Mathf.RoundToInt(f); return v; });
            Register<Color>(new[] { nameof(Color.r), nameof(Color.g), nameof(Color.b), nameof(Color.a) }, static (v, i) => v[i], static (v, i, f) => { v[i] = f; return v; });
            Register<Rect>(new[] { nameof(Rect.x), nameof(Rect.y), nameof(Rect.width), nameof(Rect.height) }, ReadRect, WriteRect);

            static float ReadRect(Rect rect, int index) {
                return index switch {
                    0 => rect.x,
                    1 => rect.y,
                    2 => rect.width,
                    _ => rect.height
                };
            }

            static Rect WriteRect(Rect rect, int index, float value) {
                switch (index) {
                    case 0: rect.x = value; break;
                    case 1: rect.y = value; break;
                    case 2: rect.width = value; break;
                    default: rect.height = value; break;
                }

                return rect;
            }
        }

        /// <summary>
        /// Registers the float axes of a struct by name, with how to read an axis and how to return a copy with one axis changed.<br/>
        /// Call it from a <see cref="RuntimeInitializeOnLoadMethodAttribute"/> method, and from an <c>[InitializeOnLoadMethod]</c> one so the Inspector lists the axes.
        /// </summary>
        public static void Register<TStruct>(string[] names, Func<TStruct, int, float> read, Func<TStruct, int, float, TStruct> write) where TStruct : struct {
            NAMES[typeof(TStruct)] = names;
            ValueAxes<TStruct>.names = names;
            ValueAxes<TStruct>.read = read;
            ValueAxes<TStruct>.write = write;
        }

        /// <summary>
        /// The axis names registered for <paramref name="type"/>, or none.
        /// </summary>
        public static IReadOnlyList<string> GetNames(Type type) {
            EnsureRegistered();
            return NAMES.TryGetValue(type, out var names) ? names : Array.Empty<string>();
        }

        // Touching the class runs its static constructor, which registers the built-in types.
        internal static void EnsureRegistered() { }
    }

    /// <summary>
    /// The registered axes of one struct type; filled by <see cref="ValueAxes.Register&lt;TStruct&gt;"/>.
    /// </summary>
    internal static class ValueAxes<TStruct> where TStruct : struct {
        internal static string[] names;
        internal static Func<TStruct, int, float> read;
        internal static Func<TStruct, int, float, TStruct> write;

        public static int IndexOf(string axis) {
            ValueAxes.EnsureRegistered();
            return names == null ? -1 : Array.IndexOf(names, axis);
        }

        public static float Read(TStruct value, int index) {
            return read(value, index);
        }

        public static TStruct Write(TStruct value, int index, float axis) {
            return write(value, index, axis);
        }
    }
}
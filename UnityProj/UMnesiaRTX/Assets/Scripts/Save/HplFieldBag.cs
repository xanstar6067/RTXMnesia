using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace RTXMnesia.Save
{
    /// <summary>One plain field of a saved object: "DeclaringType.field" and its value as invariant text.</summary>
    [Serializable]
    public sealed class SavedField
    {
        public string name, value;
        public bool isNull;
    }

    /// <summary>
    /// The plain value fields of an object (primitives, enums, strings, Unity vectors / quaternion / color / matrix) as
    /// text, by declaring type and name: what HPL2's save data classes copy one by one (kCopyToVar / kCopyFromVar) for
    /// props, enemies, areas and effects. References, read-only and explicitly listed fields are not state.
    /// </summary>
    public static class HplFieldBag
    {
        static readonly Dictionary<(Type, Type), List<FieldInfo>> s_fields = new();

        /// <summary>The plain fields of <paramref name="type"/> and its base types up to <paramref name="stopAt"/> (included).</summary>
        public static List<FieldInfo> Fields(Type type, Type stopAt, ICollection<string> skip = null)
        {
            if (skip == null && s_fields.TryGetValue((type, stopAt), out var cached)) return cached;
            var list = new List<FieldInfo>();
            for (var t = type; t != null; t = t.BaseType)
            {
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsInitOnly && !field.IsLiteral && IsPlain(field.FieldType) && (skip == null || !skip.Contains(field.Name)))
                        list.Add(field);
                if (t == stopAt) break;
            }
            if (skip == null) s_fields[(type, stopAt)] = list;
            return list;
        }

        public static bool IsPlain(Type t) =>
            t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4) ||
            t == typeof(Quaternion) || t == typeof(Color) || t == typeof(Matrix4x4);

        static string Key(FieldInfo f) => f.DeclaringType.Name + "." + f.Name;

        public static List<SavedField> Capture(object obj, Type stopAt, ICollection<string> skip = null)
        {
            var list = new List<SavedField>();
            if (obj == null) return list;
            foreach (var field in Fields(obj.GetType(), stopAt, skip))
            {
                object v = field.GetValue(obj);
                list.Add(new SavedField { name = Key(field), value = v == null ? "" : ToText(v), isNull = v == null });
            }
            return list;
        }

        /// <summary>Sets the saved fields that the object's type still has; unknown names are skipped (older saves).</summary>
        public static void Restore(object obj, Type stopAt, List<SavedField> saved, ICollection<string> skip = null)
        {
            if (obj == null || saved == null) return;
            var byName = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            foreach (var field in Fields(obj.GetType(), stopAt, skip)) byName[Key(field)] = field;
            foreach (var s in saved)
            {
                if (!byName.TryGetValue(s.name, out var field)) continue;
                try { field.SetValue(obj, s.isNull ? null : FromText(s.value, field.FieldType)); }
                catch (Exception e) { Debug.LogWarning($"[RTXMnesia] Save: could not restore {s.name} = '{s.value}' ({e.Message})"); }
            }
        }

        /// <summary>Only the named fields of the object's own type (private ones included).</summary>
        public static List<SavedField> CaptureNamed(object obj, params string[] names)
        {
            var list = new List<SavedField>();
            foreach (string name in names)
            {
                var field = NamedField(obj.GetType(), name);
                if (field == null) continue;
                object v = field.GetValue(obj);
                list.Add(new SavedField { name = name, value = v == null ? "" : ToText(v), isNull = v == null });
            }
            return list;
        }

        public static void RestoreNamed(object obj, List<SavedField> saved)
        {
            if (obj == null || saved == null) return;
            foreach (var s in saved)
            {
                var field = NamedField(obj.GetType(), s.name);
                if (field == null) continue;
                try { field.SetValue(obj, s.isNull ? null : FromText(s.value, field.FieldType)); }
                catch (Exception e) { Debug.LogWarning($"[RTXMnesia] Save: could not restore {s.name} = '{s.value}' ({e.Message})"); }
            }
        }

        static FieldInfo NamedField(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && !field.IsInitOnly && IsPlain(field.FieldType)) return field;
            }
            return null;
        }

        // ------------------------------------------------------------------ text

        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        static string F(float f) => f.ToString("R", Inv);

        static string Join(params float[] values)
        {
            var parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++) parts[i] = F(values[i]);
            return string.Join(" ", parts);
        }

        static float[] Split(string s, int n)
        {
            var parts = s.Split(' ');
            var values = new float[n];
            for (int i = 0; i < n && i < parts.Length; i++) values[i] = float.Parse(parts[i], Inv);
            return values;
        }

        public static string ToText(object v)
        {
            switch (v)
            {
                case string s: return s;
                case float f: return F(f);
                case double d: return d.ToString("R", Inv);
                case bool b: return b ? "true" : "false";
                case Enum e: return Convert.ToInt64(e, Inv).ToString(Inv);
                case Vector2 a: return Join(a.x, a.y);
                case Vector3 a: return Join(a.x, a.y, a.z);
                case Vector4 a: return Join(a.x, a.y, a.z, a.w);
                case Quaternion q: return Join(q.x, q.y, q.z, q.w);
                case Color c: return Join(c.r, c.g, c.b, c.a);
                case Matrix4x4 m:
                    var values = new float[16];
                    for (int i = 0; i < 16; i++) values[i] = m[i];
                    return Join(values);
                case IFormattable formattable: return formattable.ToString(null, Inv);
                default: return v.ToString();
            }
        }

        public static object FromText(string s, Type t)
        {
            if (t == typeof(string)) return s;
            if (t == typeof(float)) return float.Parse(s, Inv);
            if (t == typeof(double)) return double.Parse(s, Inv);
            if (t == typeof(bool)) return s == "true";
            if (t.IsEnum) return Enum.ToObject(t, long.Parse(s, Inv));
            if (t == typeof(Vector2)) { var v = Split(s, 2); return new Vector2(v[0], v[1]); }
            if (t == typeof(Vector3)) { var v = Split(s, 3); return new Vector3(v[0], v[1], v[2]); }
            if (t == typeof(Vector4)) { var v = Split(s, 4); return new Vector4(v[0], v[1], v[2], v[3]); }
            if (t == typeof(Quaternion)) { var v = Split(s, 4); return new Quaternion(v[0], v[1], v[2], v[3]); }
            if (t == typeof(Color)) { var v = Split(s, 4); return new Color(v[0], v[1], v[2], v[3]); }
            if (t == typeof(Matrix4x4))
            {
                var v = Split(s, 16);
                var m = new Matrix4x4();
                for (int i = 0; i < 16; i++) m[i] = v[i];
                return m;
            }
            return Convert.ChangeType(s, t, Inv);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>Attribute readers with HPL2 semantics (space separated vectors, invariant culture, defaults).</summary>
    public static class HplXml
    {
        static readonly char[] Separators = { ' ', '\t', '\n', '\r' };

        public static string Str(this XmlElement e, string name, string fallback = "") =>
            e.HasAttribute(name) ? e.GetAttribute(name) : fallback;

        public static float Float(this XmlElement e, string name, float fallback = 0f) =>
            e.HasAttribute(name) && TryFloat(e.GetAttribute(name), out float v) ? v : fallback;

        public static int Int(this XmlElement e, string name, int fallback = 0) =>
            e.HasAttribute(name) && int.TryParse(e.GetAttribute(name).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

        public static bool Bool(this XmlElement e, string name, bool fallback = false)
        {
            if (!e.HasAttribute(name)) return fallback;
            string s = e.GetAttribute(name).Trim();
            if (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1") return true;
            if (s.Equals("false", StringComparison.OrdinalIgnoreCase) || s == "0") return false;
            return fallback;
        }

        public static Vector2 Vec2(this XmlElement e, string name, Vector2 fallback = default)
        {
            float[] v = Floats(e, name);
            return new Vector2(v.Length > 0 ? v[0] : fallback.x, v.Length > 1 ? v[1] : fallback.y);
        }

        public static Vector3 Vec3(this XmlElement e, string name, Vector3 fallback = default)
        {
            float[] v = Floats(e, name);
            return new Vector3(v.Length > 0 ? v[0] : fallback.x, v.Length > 1 ? v[1] : fallback.y, v.Length > 2 ? v[2] : fallback.z);
        }

        /// <summary>"r g b [a]" as written by HPL2 (not clamped, alpha defaults to 1).</summary>
        public static Color Color(this XmlElement e, string name, Color fallback)
        {
            float[] v = Floats(e, name);
            if (v.Length < 3) return fallback;
            return new Color(v[0], v[1], v[2], v.Length > 3 ? v[3] : 1f);
        }

        public static float[] Floats(this XmlElement e, string name)
        {
            if (!e.HasAttribute(name)) return Array.Empty<float>();
            return ParseFloats(e.GetAttribute(name));
        }

        public static float[] ParseFloats(string text)
        {
            string[] parts = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) TryFloat(parts[i], out result[i]);
            return result;
        }

        public static int[] ParseInts(string text)
        {
            string[] parts = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]);
            return result;
        }

        static bool TryFloat(string s, out float value) =>
            float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        /// <summary>&lt;Var Name="..." Value="..."/&gt; children of <paramref name="parent"/>.</summary>
        public static List<HplVariable> Variables(XmlElement parent)
        {
            var list = new List<HplVariable>();
            if (parent == null) return list;
            foreach (XmlNode node in parent.ChildNodes)
                if (node is XmlElement v && v.Name == "Var")
                    list.Add(new HplVariable(v.GetAttribute("Name"), v.GetAttribute("Value")));
            return list;
        }

        public static XmlElement Child(this XmlElement e, string name) => e?[name];

        public static IEnumerable<XmlElement> Children(this XmlElement e)
        {
            if (e == null) yield break;
            foreach (XmlNode node in e.ChildNodes)
                if (node is XmlElement child) yield return child;
        }
    }
}

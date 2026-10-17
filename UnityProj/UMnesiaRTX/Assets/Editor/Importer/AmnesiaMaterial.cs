using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace RTXMnesia.Importer
{
    /// <summary>Parsed HPL2 .mat file (format: HPL2 MaterialManager.cpp / MaterialType_*.cpp).</summary>
    public class AmnesiaMaterial
    {
        public enum Kind { Solid, Translucent, Decal, Water, Unknown }

        public string SourcePath;
        public Kind Type;
        public string TypeName;
        public string BlendMode;        // Translucent/Decal/Water: Add, Mul, MulX2, Alpha, PremulAlpha
        public string PhysicsMaterial;  // Footstep sounds / impact effects, used later by gameplay.
        public bool UseAlpha;

        /// <summary>Texture unit name (Diffuse, NMap, Specular, Height, Alpha, Illumination, CubeMap, ...) → file reference as written.</summary>
        public readonly Dictionary<string, string> Textures = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, string> Variables = new(StringComparer.OrdinalIgnoreCase);

        public string Texture(string unit) => Textures.TryGetValue(unit, out var f) && !string.IsNullOrWhiteSpace(f) ? f : null;

        public float Float(string name, float fallback) =>
            Variables.TryGetValue(name, out var v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)
                ? f : fallback;

        /// <summary>cResourceVarsObject::GetVarBool (cString::ToBool): missing → fallback, otherwise only "true" is true.</summary>
        public bool Bool(string name, bool fallback) =>
            Variables.TryGetValue(name, out var v) ? v.Equals("true", StringComparison.OrdinalIgnoreCase) : fallback;

        public static AmnesiaMaterial Load(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            var m = new AmnesiaMaterial { SourcePath = path };

            if (doc.GetElementsByTagName("Main")[0] is XmlElement main)
            {
                m.TypeName = main.GetAttribute("Type");
                m.Type = m.TypeName.ToLowerInvariant() switch
                {
                    "soliddiffuse" => Kind.Solid,
                    "translucent" => Kind.Translucent,
                    "decal" => Kind.Decal,
                    "water" => Kind.Water,
                    _ => Kind.Unknown,
                };
                m.BlendMode = main.GetAttribute("BlendMode");
                m.PhysicsMaterial = main.GetAttribute("PhysicsMaterial");
                m.UseAlpha = main.GetAttribute("UseAlpha").Equals("true", StringComparison.OrdinalIgnoreCase);
                // Older files keep material variables (HeightMapScale, ...) as attributes of <Main>.
                foreach (XmlAttribute a in main.Attributes) m.Variables[a.Name] = a.Value;
            }

            if (doc.GetElementsByTagName("TextureUnits")[0] is XmlElement units)
                foreach (XmlNode n in units.ChildNodes)
                    if (n is XmlElement unit)
                        m.Textures[unit.Name] = unit.GetAttribute("File");

            foreach (XmlElement v in doc.GetElementsByTagName("Var"))
                m.Variables[v.GetAttribute("Name")] = v.GetAttribute("Value");

            return m;
        }
    }
}

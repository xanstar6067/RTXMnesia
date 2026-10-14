using System;
using System.Collections.Generic;
using System.Xml;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// The parts of a .dae file the importers need besides what Unity's own Collada import provides:
    /// which texture each Collada material uses. HPL2 names the .mat after that texture
    /// (MeshLoaderCollada.cpp: GetMaterialTextureFile → SetFileExt(..., "mat")).
    /// </summary>
    public class ColladaInfo
    {
        /// <summary>Collada material id and name → texture file name ("ceiling02_arched.dds").</summary>
        public readonly Dictionary<string, string> MaterialTextures = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Collada materials (id, name, symbol) without a texture: HPL2 gives such sub meshes no material and never renders them.</summary>
        public readonly HashSet<string> UntexturedMaterials = new(StringComparer.OrdinalIgnoreCase);
        public bool HasSkin;
        public bool HasAnimation;
        public string UpAxis = "Y_UP";

        public static ColladaInfo Load(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("c", "http://www.collada.org/2005/11/COLLADASchema");
            var info = new ColladaInfo();

            info.UpAxis = doc.SelectSingleNode("//c:asset/c:up_axis", ns)?.InnerText.Trim() ?? "Y_UP";
            info.HasSkin = doc.SelectSingleNode("//c:library_controllers/c:controller/c:skin", ns) != null;
            info.HasAnimation = doc.SelectSingleNode("//c:library_animations/c:animation", ns) != null;

            // image id → file name
            var images = new Dictionary<string, string>();
            foreach (XmlElement image in doc.SelectNodes("//c:library_images/c:image", ns))
            {
                string from = image.SelectSingleNode("c:init_from", ns)?.InnerText.Trim();
                if (string.IsNullOrEmpty(from)) continue;
                // Paths like "file:///E:/lux/redist/x.dds" or "file://C|/penumbra/redist/x" (old Penumbra files):
                // only the file name matters, and it may lack an extension.
                from = Uri.UnescapeDataString(from);
                images[image.GetAttribute("id")] = from.Substring(from.LastIndexOfAny(new[] { '/', '\\', '|' }) + 1);
            }

            // effect id → image file: first <init_from> inside the effect (surface param or COLLADA 1.4.0 texture).
            var effects = new Dictionary<string, string>();
            foreach (XmlElement effect in doc.SelectNodes("//c:library_effects/c:effect", ns))
            {
                string file = null;
                foreach (XmlNode init in effect.SelectNodes(".//c:init_from", ns))
                    if (images.TryGetValue(init.InnerText.Trim(), out file)) break;
                if (file == null)
                    foreach (XmlElement tex in effect.SelectNodes(".//c:texture", ns))
                        if (images.TryGetValue(tex.GetAttribute("texture"), out file)) break;
                if (file != null) effects[effect.GetAttribute("id")] = file;
            }

            foreach (XmlElement material in doc.SelectNodes("//c:library_materials/c:material", ns))
            {
                string effectUrl = (material.SelectSingleNode("c:instance_effect", ns) as XmlElement)?.GetAttribute("url")?.TrimStart('#');
                if (effectUrl == null || !effects.TryGetValue(effectUrl, out string file))
                {
                    info.UntexturedMaterials.Add(material.GetAttribute("id"));
                    info.UntexturedMaterials.Add(material.GetAttribute("name"));
                    continue;
                }
                info.MaterialTextures[material.GetAttribute("id")] = file;
                string name = material.GetAttribute("name");
                if (name.Length > 0) info.MaterialTextures[name] = file;
            }

            // Geometry binds materials by symbol ("file5SG" → "#file5Material"); importers may use either name.
            foreach (XmlElement bind in doc.SelectNodes("//c:instance_material", ns))
            {
                string target = bind.GetAttribute("target").TrimStart('#');
                if (info.MaterialTextures.TryGetValue(target, out string file))
                    info.MaterialTextures[bind.GetAttribute("symbol")] = file;
                else if (info.UntexturedMaterials.Contains(target))
                    info.UntexturedMaterials.Add(bind.GetAttribute("symbol"));
            }
            return info;
        }
    }
}

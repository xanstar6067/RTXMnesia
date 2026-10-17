using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Writes a .dae_anim (a whole Collada file of a rig) as the .dae Unity imports, prepared so that Unity's
    /// resampling gives the poses HPL2 plays (MeshLoaderColladaHelpers.cpp):
    /// - HPL2 builds a rotation per key and slerps between keys (cAnimationTrack::GetInterpolatedKeyFrame), ignoring
    ///   the Bezier tangents. Unity evaluates the Euler curves between the keys while resampling (30 fps against the
    ///   24 fps keys), and Maya exports switch to the equivalent Euler triple (x + 180, 180 − y, z + 180) or jump by
    ///   360 between two keys: the sample between them spun the hand through a whole turn (the lantern draw / holster
    ///   pop). Each key's triple is replaced by the equivalent one nearest to the previous key and the
    ///   interpolation is linear, so the curve between keys follows the slerp.
    /// - HPL2 starts the animation at the scene's start_time (keys − start_time, length end − start).
    /// </summary>
    public static class ColladaAnimFix
    {
        /// <summary>Writes the prepared copy when it differs from the target; true if written.</summary>
        public static bool CopyFixed(string source, string target)
        {
            string temp = target + ".tmp";
            Write(source, temp);
            if (File.Exists(target) && File.ReadAllBytes(target).AsSpan().SequenceEqual(File.ReadAllBytes(temp)))
            {
                File.Delete(temp);
                return false;
            }
            File.Copy(temp, target, true);
            File.Delete(temp);
            return true;
        }

        public static void Write(string source, string target)
        {
            var doc = new XmlDocument();
            doc.Load(source);
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("c", doc.DocumentElement.NamespaceURI);
            XmlNodeList Select(string xpath) => doc.SelectNodes(string.IsNullOrEmpty(doc.DocumentElement.NamespaceURI) ? xpath.Replace("c:", "") : xpath, ns);

            static float[] Floats(XmlNode n) => n.InnerText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(t => float.Parse(t, CultureInfo.InvariantCulture)).ToArray();
            static void SetFloats(XmlNode n, IEnumerable<float> v) => n.InnerText = string.Join(" ", v.Select(x => x.ToString("R", CultureInfo.InvariantCulture)));

            XmlNode ArrayOf(XmlElement sampler, string semantic)
            {
                foreach (XmlElement input in sampler.ChildNodes.OfType<XmlElement>())
                {
                    if (input.LocalName != "input" || input.GetAttribute("semantic") != semantic) continue;
                    string id = input.GetAttribute("source").TrimStart('#');
                    foreach (XmlElement s in Select("//c:source").OfType<XmlElement>())
                        if (s.GetAttribute("id") == id)
                            return s.ChildNodes.OfType<XmlElement>().FirstOrDefault(e => e.LocalName is "float_array" or "Name_array");
                }
                return null;
            }

            // Start time (library_visual_scenes extra technique).
            float start = 0f;
            foreach (XmlElement e in Select("//c:start_time").OfType<XmlElement>())
                if (float.TryParse(e.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out float s)) start = s;
            foreach (XmlElement e in Select("//c:start_time").OfType<XmlElement>()) e.InnerText = "0";
            foreach (XmlElement e in Select("//c:end_time").OfType<XmlElement>())
                if (float.TryParse(e.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out float end))
                    e.InnerText = (end - start).ToString("R", CultureInfo.InvariantCulture);

            // The rotation order of each node (outermost first): the middle axis is the one that becomes 180 − angle.
            var middleAxis = new Dictionary<string, char>(StringComparer.Ordinal);
            foreach (XmlElement node in Select("//c:node").OfType<XmlElement>())
            {
                var axes = node.ChildNodes.OfType<XmlElement>().Where(e => e.LocalName == "rotate")
                               .Select(e => e.GetAttribute("sid")).Where(sid => sid.Length == 7 && sid.StartsWith("rotate")).Select(sid => char.ToUpperInvariant(sid[6])).ToList();
                if (axes.Count == 3 && axes.Distinct().Count() == 3)
                {
                    middleAxis[node.GetAttribute("id")] = axes[1];
                    if (node.HasAttribute("name")) middleAxis[node.GetAttribute("name")] = axes[1];
                }
            }

            var shifted = new HashSet<XmlNode>();
            var rotations = new Dictionary<string, Dictionary<char, XmlNode>>(StringComparer.Ordinal);
            foreach (XmlElement channel in Select("//c:channel").OfType<XmlElement>())
            {
                string samplerId = channel.GetAttribute("source").TrimStart('#');
                var sampler = Select("//c:sampler").OfType<XmlElement>().FirstOrDefault(x => x.GetAttribute("id") == samplerId);
                if (sampler == null) continue;
                var input = ArrayOf(sampler, "INPUT");
                if (input != null && start != 0f && shifted.Add(input)) SetFloats(input, Floats(input).Select(t => t - start));
                var interpolation = ArrayOf(sampler, "INTERPOLATION");
                if (interpolation != null)
                    interpolation.InnerText = string.Join(" ", interpolation.InnerText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Select(_ => "LINEAR"));

                // target="node/rotateX.ANGLE"
                string targetPath = channel.GetAttribute("target");
                int slash = targetPath.IndexOf('/');
                if (slash < 0) continue;
                string node = targetPath.Substring(0, slash), member = targetPath.Substring(slash + 1);
                if (!member.StartsWith("rotate", StringComparison.Ordinal) || !member.EndsWith(".ANGLE", StringComparison.Ordinal) || member.Length != 13) continue;
                var output = ArrayOf(sampler, "OUTPUT");
                if (output == null) continue;
                if (!rotations.TryGetValue(node, out var set)) rotations[node] = set = new Dictionary<char, XmlNode>();
                set[char.ToUpperInvariant(member[6])] = output;
            }

            foreach (var (node, set) in rotations.Select(p => (p.Key, p.Value)))
            {
                if (set.Count == 3 && set.TryGetValue('X', out var xs) && set.TryGetValue('Y', out var ys) && set.TryGetValue('Z', out var zs))
                {
                    float[] x = Floats(xs), y = Floats(ys), z = Floats(zs);
                    if (x.Length == y.Length && y.Length == z.Length)
                    {
                        char middle = middleAxis.TryGetValue(node, out var m) ? m : 'Y';
                        Unwrap(x, y, z, middle);
                        SetFloats(xs, x);
                        SetFloats(ys, y);
                        SetFloats(zs, z);
                        continue;
                    }
                }
                foreach (var output in set.Values)
                {
                    var v = Floats(output);
                    for (int i = 1; i < v.Length; i++) v[i] = Nearest(v[i], v[i - 1]);
                    SetFloats(output, v);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target));
            doc.Save(target);
        }

        /// <summary>Each key: of (x, y, z) and its equivalent (outer + 180, 180 − middle, inner + 180), the triple nearest the previous key.</summary>
        static void Unwrap(float[] x, float[] y, float[] z, char middle)
        {
            for (int i = 1; i < x.Length; i++)
            {
                float[] a = { x[i], y[i], z[i] };
                float[] b = { x[i] + 180f, y[i] + 180f, z[i] + 180f };
                int mi = middle - 'X';
                b[mi] = 180f - a[mi];
                float[] prev = { x[i - 1], y[i - 1], z[i - 1] };
                float Cost(float[] t)
                {
                    float c = 0;
                    for (int k = 0; k < 3; k++) { t[k] = Nearest(t[k], prev[k]); c += Math.Abs(t[k] - prev[k]); }
                    return c;
                }
                var best = Cost(a) <= Cost(b) ? a : b;
                x[i] = best[0]; y[i] = best[1]; z[i] = best[2];
            }
        }

        static float Nearest(float angle, float previous)
        {
            while (angle - previous > 180f) angle -= 360f;
            while (angle - previous < -180f) angle += 360f;
            return angle;
        }
    }
}

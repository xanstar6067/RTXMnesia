using System;
using System.IO;
using System.Linq;
using System.Text;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Translucent Alpha / PremulAlpha materials (HplTranslucent.shader, unlit as HPL2): reconverts all of them from
    /// source/ and the tinted copies in the levels, then checks shader, blend, cube map and light level flag.
    /// -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.TranslucentMaterialTests.ReimportBatch.
    /// Report: Logs/TranslucentMaterials.txt.
    /// </summary>
    public static class TranslucentMaterialTests
    {
        public static void ReimportBatch()
        {
            var report = new StringBuilder();
            int failures = 0;
            void Check(bool ok, string message)
            {
                report.AppendLine((ok ? "ok    " : "FAIL  ") + message);
                if (!ok) failures++;
            }
            try
            {
                var files = Directory.GetFiles(AmnesiaPaths.SourceRoot, "*.mat", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        var m = AmnesiaMaterial.Load(f);
                        return m.Type == AmnesiaMaterial.Kind.Translucent && (m.BlendMode ?? "").ToLowerInvariant() is "alpha" or "premulalpha";
                    }).OrderBy(f => f).ToArray();
                // Particle materials are converted by ParticleConverter (HplParticle) unless a mesh uses them too.
                var meshFiles = files.Where(f => AssetDatabase.LoadAssetAtPath<Material>(MaterialConverter.AssetPathFor(f)) != null ||
                                                 !AmnesiaPaths.ToSourceRelative(f).Replace('\\', '/').StartsWith("particles/", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                report.AppendLine($"Translucent Alpha/PremulAlpha source materials: {files.Length}, converted as mesh materials: {meshFiles.Length}");
                MaterialConverter.ConvertMaterials(meshFiles, false, "Translucent Alpha/PremulAlpha (HplTranslucent)");

                foreach (string f in meshFiles)
                {
                    var src = AmnesiaMaterial.Load(f);
                    var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialConverter.AssetPathFor(f));
                    string rel = AmnesiaPaths.ToSourceRelative(f);
                    if (m == null) { Check(false, rel + ": no material"); continue; }
                    bool premul = (src.BlendMode ?? "").ToLowerInvariant() == "premulalpha";
                    bool cube = src.Texture("CubeMap") != null;
                    Check(m.shader.name == MaterialConverter.TranslucentShader && m.GetFloat("_HplBlendMode") == (premul ? 4 : 3) &&
                          m.GetFloat("_HplEnvMap") == (cube ? 1 : 0) && (!cube || m.GetTexture("_CubeMap") != null) &&
                          (src.Texture("CubeMapAlpha") == null || m.GetTexture("_CubeMapAlpha") != null) &&
                          m.GetFloat("_HplAffectedByLightLevel") == (src.Bool("AffectedByLightLevel", false) ? 1 : 0) &&
                          m.GetTexture("_BaseColorMap") != null,
                          $"{rel}: {(premul ? "PremulAlpha" : "Alpha")}{(cube ? ", cube map" : "")}{(src.Bool("AffectedByLightLevel", false) ? ", light level" : "")}");
                }

                int copies = 0, litLeft = 0;
                string root = AmnesiaPaths.ImportedAssetsRoot + "/Levels";
                foreach (string file in Directory.GetFiles(AmnesiaPaths.ToAbsolute(root), "*_generated.asset", SearchOption.AllDirectories))
                {
                    string path = root + "/" + Path.GetRelativePath(AmnesiaPaths.ToAbsolute(root), file).Replace('\\', '/');
                    foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                    {
                        if (m.shader.name == MaterialConverter.TranslucentShader) copies++;
                        else if (m.shader.name == "HDRP/Lit" && m.GetFloat("_SurfaceType") == 1)
                        {
                            litLeft++;
                            report.AppendLine($"lit transparent copy left: {path} {m.name}");
                        }
                    }
                }
                Check(litLeft == 0, $"level copies: {copies} HplTranslucent, {litLeft} HDRP/Lit transparent left");
            }
            catch (Exception e)
            {
                Check(false, "exception: " + e);
            }
            report.AppendLine(failures == 0 ? "PASS" : $"FAIL: {failures}");
            File.WriteAllText(Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "TranslucentMaterials.txt"), report.ToString());
            Debug.Log("[TranslucentMaterials]\n" + report);
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }
}

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Import settings for .dae files copied to Assets/AmnesiaImported/Models by <see cref="ModelConverter"/>.
    /// Unity's own Collada import reads geometry, hierarchy, skinning and animation; this adds the HPL2 rules:
    /// - Collada materials are remapped to the converted HPL2 materials (.mat named after the texture);
    /// - nodes named "_collider_&lt;type&gt;_*" / "_charcollider_&lt;type&gt;_*" become invisible colliders
    ///   (MeshLoaderCollada.cpp: box/sphere/capsule/cylinder sized from the node's mesh bounds, "mesh" = mesh collider).
    ///   Char colliders block only characters; they keep their name prefix so gameplay can put them on a layer.
    /// </summary>
    public class ModelImportRules : AssetPostprocessor
    {
        // Bump to reimport every model after changing these rules.
        public override uint GetVersion() => 3;

        /// <summary>
        /// The player's hands (models/player/hands) and their animation files: legacy clips, played with
        /// cross fades like cMeshEntity::PlayFadeToName (see HandsConverter / PlayerHands).
        /// </summary>
        public static bool IsLegacyAnimated(string assetPath) =>
            assetPath.Replace('\\', '/').Contains("/models/player/hands/", StringComparison.OrdinalIgnoreCase);

        static ResourceIndex _index;
        static ResourceIndex Index => _index ??= new ResourceIndex(AmnesiaPaths.SourceRoot);

        /// <summary>Forget the cached source listing (call after source/ changed).</summary>
        public static void ResetIndex() => _index = null;

        bool IsAmnesiaModel => assetPath.StartsWith(AmnesiaPaths.ModelsRoot + "/", StringComparison.Ordinal);

        void OnPreprocessModel()
        {
            if (!IsAmnesiaModel) return;
            var importer = (ModelImporter)assetImporter;
            string source = AmnesiaPaths.SourceOf(AmnesiaPaths.ModelsRoot, assetPath);
            var info = ColladaInfo.Load(AmnesiaPaths.ToAbsolute(assetPath));

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.importBlendShapes = false;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            bool animated = info.HasSkin || info.HasAnimation;
            importer.animationType = !animated ? ModelImporterAnimationType.None
                : IsLegacyAnimated(assetPath) ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.Generic;
            importer.importAnimation = info.HasAnimation;

            foreach (var pair in info.MaterialTextures)
            {
                string matFile = Index.Find(Path.ChangeExtension(pair.Value, ".mat"), source);
                if (matFile == null) continue;
                var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialConverter.AssetPathFor(matFile));
                if (material != null)
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), material);
            }
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!IsAmnesiaModel) return;
            var info = ColladaInfo.Load(AmnesiaPaths.ToAbsolute(assetPath));
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                ConvertColliderNode(t.gameObject);
                RemoveUntexturedRenderer(t.gameObject, info);
            }
        }

        static void RemoveUntexturedRenderer(GameObject node, ColladaInfo info)
        {
            var renderer = node.GetComponent<Renderer>();
            if (renderer == null || renderer.sharedMaterials.Length == 0) return;
            foreach (var material in renderer.sharedMaterials)
                if (material != null && !info.UntexturedMaterials.Contains(material.name)) return;
            UnityEngine.Object.DestroyImmediate(renderer);
        }

        static void ConvertColliderNode(GameObject node)
        {
            string name = node.name.ToLowerInvariant();
            if (!name.StartsWith("_collider_") && !name.StartsWith("_charcollider_")) return;

            var filter = node.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null) return;

            string[] parts = name.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            string shape = parts.Length > 1 ? parts[1] : "mesh";
            Bounds b = mesh.bounds;
            // A char collider stops only the character (HplModelBuilder.IsCharColliderName).
            if (name.StartsWith("_charcollider_")) node.layer = RTXMnesia.Dynamics.HplPhysicsWorld.NoCollideLayer;

            switch (shape)
            {
                case "box":
                    var box = node.AddComponent<BoxCollider>();
                    box.center = b.center;
                    box.size = b.size;
                    break;
                case "sphere":
                    var sphere = node.AddComponent<SphereCollider>();
                    sphere.center = b.center;
                    sphere.radius = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
                    break;
                case "capsule":
                    var capsule = node.AddComponent<CapsuleCollider>();
                    capsule.center = b.center;
                    capsule.direction = LongestAxis(b.size);
                    capsule.height = b.size[capsule.direction];
                    capsule.radius = Mathf.Max(b.extents[(capsule.direction + 1) % 3], b.extents[(capsule.direction + 2) % 3]);
                    break;
                case "cylinder":
                    // Unity has no cylinder primitive; the node's own cylinder mesh as a convex hull is exact.
                    node.AddComponent<MeshCollider>().convex = true;
                    node.GetComponent<MeshCollider>().sharedMesh = mesh;
                    break;
                default: // "mesh"
                    node.AddComponent<MeshCollider>().sharedMesh = mesh;
                    break;
            }

            // Collider shapes are never rendered.
            UnityEngine.Object.DestroyImmediate(node.GetComponent<MeshRenderer>());
            if (shape is "box" or "sphere" or "capsule") UnityEngine.Object.DestroyImmediate(filter);
        }

        static int LongestAxis(Vector3 size) => size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
    }
}

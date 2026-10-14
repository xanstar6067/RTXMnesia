using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RTXMnesia.Dev;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Builds a Unity scene from an HPL2 .map (WorldLoaderHplMap.cpp, EngineFileLoading.cpp, EntityLoader_Object.cpp).
    /// Output: Assets/AmnesiaImported/Levels/&lt;map path&gt;.unity plus generated meshes/materials and a volume profile
    /// next to it. Models and materials the map needs are imported first if missing.
    ///
    /// - StaticObject → model prefab instance; mesh colliders from the visual mesh when Collides and the model has
    ///   no "_collider" nodes (HPL2 rule), CastShadows → shadow casting.
    /// - Plane primitive → generated quad (MeshCreator::CreatePlane), Decal → its stored world-space mesh.
    /// - Entity → .ent: model (unpacked) with the .ent sub mesh transforms, bodies with collider shapes (static for
    ///   now), and the lights / billboards / particle systems / sounds of the .ent.
    /// - Point/Spot lights → HDRP lights through <see cref="AmnesiaLight"/>; spot gobos → cookies (pyramid lights).
    /// - Area → <see cref="AmnesiaArea"/> (+ trigger box). Particle systems and sounds → placeholders.
    /// - Fog, fixed exposure and "no sky" (HPL2 has no ambient light) → level volume profile.
    /// </summary>
    public class LevelConverter
    {
        public const string LevelsRoot = AmnesiaPaths.ImportedAssetsRoot + "/Levels";

        // Spots without a gobo use the "short fade" falloff over the cone: full strength until ~80% of the angle.
        const float SpotInnerAngleFraction = 0.8f;
        const float CameraEyeHeight = 1.6f;
        const StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

        readonly string _mapAbsolute;
        readonly string _mapRelative;
        readonly string _mapName;
        readonly string _levelFolder;
        readonly ImportLog _log = new();
        readonly ResourceIndex _index = new(AmnesiaPaths.SourceRoot);

        string[] _staticFiles, _entityFiles, _decalFiles;
        readonly Dictionary<string, HplEntityFile> _entities = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, ColladaInfo> _collada = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, GameObject> _models = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Material> _materials = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Material> _variants = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, string> _cookies = new(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> _warnedOnce = new();
        readonly Dictionary<string, int> _counts = new();
        readonly List<Transform> _playerStarts = new();

        Mesh _container;
        Mesh _quad, _cylinder;

        public string ScenePath => _levelFolder + ".unity";
        string GeneratedPath => _levelFolder + "/" + _mapName + "_generated.asset";
        string VolumePath => _levelFolder + "/" + _mapName + "_volume.asset";

        LevelConverter(string mapAbsolute)
        {
            _mapAbsolute = Path.GetFullPath(mapAbsolute);
            _mapRelative = AmnesiaPaths.ToSourceRelative(_mapAbsolute);
            _mapName = Path.GetFileNameWithoutExtension(_mapAbsolute);
            _levelFolder = AmnesiaPaths.ToAssetPath(LevelsRoot, _mapRelative);
        }

        public static string ScenePathFor(string mapAbsolute) => new LevelConverter(mapAbsolute).ScenePath;

        /// <summary>Converts the map and saves the scene. Returns the scene asset path.</summary>
        public static string Convert(string mapAbsolute)
        {
            var converter = new LevelConverter(mapAbsolute);
            converter.Run();
            return converter.ScenePath;
        }

        void Run()
        {
            var doc = new XmlDocument();
            doc.Load(_mapAbsolute);
            var mapData = doc.DocumentElement.Child("MapData") ?? throw new InvalidDataException("No MapData element");
            var contents = mapData.Child("MapContents") ?? throw new InvalidDataException("No MapContents element");

            _staticFiles = FileIndex(contents, "FileIndex_StaticObjects");
            _entityFiles = FileIndex(contents, "FileIndex_Entities");
            _decalFiles = FileIndex(contents, "FileIndex_Decals");

            PrepareResources(contents);

            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(_levelFolder));
            AssetDatabase.Refresh();
            AssetDatabase.DeleteAsset(GeneratedPath);
            _container = new Mesh { name = _mapName + " generated" };
            AssetDatabase.CreateAsset(_container, GeneratedPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(_mapName);
            root.AddComponent<AmnesiaLevel>().mapFile = _mapRelative;
            Transform Group(string name)
            {
                var group = new GameObject(name).transform;
                group.SetParent(root.transform, false);
                return group;
            }

            Transform statics = Group("StaticObjects"), primitives = Group("Primitives"), decals = Group("Decals"),
                entities = Group("Entities"), lights = Group("Lights"), billboards = Group("Billboards"),
                particles = Group("ParticleSystems"), sounds = Group("Sounds"), areas = Group("Areas");

            ForEach(contents.Child("StaticObjects"), "StaticObject", e => PlaceStatic(e, statics));
            ForEach(contents.Child("Primitives"), "Plane", e => BuildPlane(e, primitives));
            ForEach(contents.Child("Decals"), "Decal", e => BuildDecal(e, decals));
            foreach (var e in contents.Child("Entities").Children())
            {
                try
                {
                    switch (e.Name)
                    {
                        case "Entity": PlaceEntity(e, entities); break;
                        case "Area": BuildArea(e, areas); break;
                        case "Billboard": BuildWorldEntity(e, billboards, "", Vector3.one); break;
                        case "ParticleSystem": BuildWorldEntity(e, particles, "", Vector3.one); break;
                        case "Sound": BuildWorldEntity(e, sounds, "", Vector3.one); break;
                        default: BuildWorldEntity(e, lights, "", Vector3.one); break;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"{e.Name} '{e.Str("Name")}': {ex.Message}");
                }
            }

            BuildEnvironment(mapData, root.transform);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            _log.Info("Built: " + string.Join(", ", _counts.OrderBy(p => p.Key).Select(p => $"{p.Key} {p.Value}")));
            _log.Info($"Scene: {ScenePath}");
            _log.Save($"Level: {_mapRelative}");
        }

        void ForEach(XmlElement parent, string name, Action<XmlElement> build)
        {
            foreach (var e in parent.Children())
            {
                if (e.Name != name)
                {
                    WarnOnce($"'{e.Name}' in {parent.Name} is not supported yet");
                    continue;
                }
                try { build(e); }
                catch (Exception ex) { _log.Error($"{e.Name} '{e.Str("Name")}': {ex.Message}"); }
            }
        }

        void Count(string what) => _counts[what] = _counts.TryGetValue(what, out int n) ? n + 1 : 1;

        void WarnOnce(string message)
        {
            if (_warnedOnce.Add(message)) _log.Warn(message);
        }

        // ---------- Resources ----------

        string[] FileIndex(XmlElement contents, string name)
        {
            var element = contents.Child(name);
            var files = new string[element?.Int("NumOfFiles") ?? 0];
            foreach (var file in element.Children())
            {
                int id = file.Int("Id", -1);
                if (id < 0) continue;
                if (id >= files.Length) Array.Resize(ref files, id + 1);
                string path = file.Str("Path");
                files[id] = _index.Find(path);
                if (files[id] == null) _log.Warn($"{name}: '{path}' not found in source/");
            }
            return files;
        }

        string Indexed(string[] files, XmlElement e, string attribute)
        {
            int i = e.Int(attribute, -1);
            return i >= 0 && i < files.Length ? files[i] : null;
        }

        HplEntityFile Entity(string entAbsolute)
        {
            if (!_entities.TryGetValue(entAbsolute, out var ent))
                _entities[entAbsolute] = ent = HplEntityFile.Load(entAbsolute);
            return ent;
        }

        ColladaInfo Collada(string daeAbsolute)
        {
            if (!_collada.TryGetValue(daeAbsolute, out var info))
                _collada[daeAbsolute] = info = ColladaInfo.Load(daeAbsolute);
            return info;
        }

        string EntityMesh(HplEntityFile ent) =>
            string.IsNullOrEmpty(ent.MeshFile) ? null : _index.Find(ent.MeshFile, ent.Path);

        /// <summary>Imports the models, materials and cookies the map needs that are not converted yet.</summary>
        void PrepareResources(XmlElement contents)
        {
            var daes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var mats = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var gobos = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddRef(SortedSet<string> set, string reference, string near = null)
            {
                if (string.IsNullOrWhiteSpace(reference)) return;
                string found = _index.Find(reference, near);
                if (found != null) set.Add(found);
                else WarnOnce($"'{reference}' not found in source/");
            }

            void AddWorldEntity(XmlElement e)
            {
                if (e.Name == "Billboard") AddRef(mats, e.Str("MaterialFile"));
                if (e.Name == "SpotLight") AddRef(gobos, e.Str("Gobo"));
            }

            foreach (string dae in _staticFiles) if (dae != null) daes.Add(dae);
            foreach (string ent in _entityFiles)
            {
                if (ent == null) continue;
                try
                {
                    var file = Entity(ent);
                    string mesh = EntityMesh(file);
                    if (mesh != null) daes.Add(mesh);
                    else if (!string.IsNullOrEmpty(file.MeshFile)) _log.Warn($"{AmnesiaPaths.ToSourceRelative(ent)}: mesh '{file.MeshFile}' not found");
                    foreach (var sub in file.SubMeshes) AddRef(mats, sub.Material, ent);
                    foreach (var e in file.WorldEntities) AddWorldEntity(e);
                }
                catch (Exception ex) { _log.Error($"{AmnesiaPaths.ToSourceRelative(ent)}: {ex.Message}"); }
            }
            foreach (var plane in contents.Child("Primitives").Children()) AddRef(mats, plane.Str("Material"));
            foreach (string decal in _decalFiles) if (decal != null) mats.Add(decal);
            foreach (var e in contents.Child("Entities").Children()) AddWorldEntity(e);

            var missingModels = daes.Where(d => !File.Exists(AmnesiaPaths.ToAbsolute(ModelConverter.AssetPathFor(d)))).ToList();
            if (missingModels.Count > 0) ModelConverter.ConvertModels(missingModels, $"Models for {_mapRelative}");

            var missingMats = mats.Where(m => !File.Exists(AmnesiaPaths.ToAbsolute(MaterialConverter.AssetPathFor(m)))).ToList();
            if (missingMats.Count > 0) MaterialConverter.ConvertMaterials(missingMats, false, $"Materials for {_mapRelative}");

            bool written = false;
            foreach (string gobo in gobos)
            {
                try
                {
                    _cookies[gobo] = TextureConverter.Convert(gobo, TextureRole.Cookie, null, false, out bool w);
                    written |= w;
                }
                catch (Exception ex) { _log.Warn($"Gobo {AmnesiaPaths.ToSourceRelative(gobo)}: {ex.Message}"); }
            }
            if (written) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            _log.Info($"Resources: {daes.Count} models ({missingModels.Count} imported now), {mats.Count} extra materials " +
                      $"({missingMats.Count} converted now), {_cookies.Count} cookies");
        }

        GameObject Model(string daeAbsolute)
        {
            if (daeAbsolute == null) return null;
            if (!_models.TryGetValue(daeAbsolute, out var model))
            {
                model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelConverter.AssetPathFor(daeAbsolute));
                if (model == null) _log.Error($"Model not imported: {AmnesiaPaths.ToSourceRelative(daeAbsolute)}");
                _models[daeAbsolute] = model;
            }
            return model;
        }

        Material LevelMaterial(string reference, string near = null)
        {
            if (string.IsNullOrWhiteSpace(reference)) return null;
            string abs = _index.Find(reference, near);
            if (abs == null) return null;
            if (!_materials.TryGetValue(abs, out var material))
            {
                material = AssetDatabase.LoadAssetAtPath<Material>(MaterialConverter.AssetPathFor(abs));
                if (material == null) _log.Warn($"Material not converted: {AmnesiaPaths.ToSourceRelative(abs)}");
                _materials[abs] = material;
            }
            return material;
        }

        T AddGenerated<T>(T asset) where T : Object
        {
            AssetDatabase.AddObjectToAsset(asset, _container);
            return asset;
        }

        // ---------- Coordinate helpers ----------

        // Z_UP Collada files (verified on castle.dae, chandelier_simple_short.dae, plane_black.dae):
        // - Unity keeps node-local vertices in the file's axes (X mirrored) and adds Rx(-90°) at the top
        //   (on the root, or on each top-level node when the root is empty);
        // - HPL2 rewrites vertices (x, y, z) → (-x, z, y) and node translations/scales (x, z, y), keeping rotate
        //   elements as written (MeshLoaderColladaHelpers.cpp GetVectorPos / GetVectorPosFromPtr).
        // In Unity space the HPL2 vertex of a Unity node-local vertex (-x, y, z) is (x, z, y):
        static readonly Matrix4x4 ZUpNodeCorrection = new Matrix4x4(
            new Vector4(-1f, 0f, 0f, 0f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(0f, 0f, 0f, 1f));
        // Unity's whole converted model vs HPL2: turned 180° around Y (used for skinned models placed whole).
        static readonly Matrix4x4 ZUpModelCorrection = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));
        static readonly Matrix4x4 UndoUnityZUp = Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f));

        bool IsZUp(string daeAbsolute) => Collada(daeAbsolute).UpAxis == "Z_UP";

        Matrix4x4 NodeCorrection(string daeAbsolute) => IsZUp(daeAbsolute) ? ZUpNodeCorrection : Matrix4x4.identity;

        Matrix4x4 ModelCorrection(string daeAbsolute) => IsZUp(daeAbsolute) ? ZUpModelCorrection : Matrix4x4.identity;

        static Matrix4x4 LocalMatrix(Transform t) => Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);

        static void SetLocalMatrix(Transform t, Matrix4x4 m)
        {
            t.localPosition = m.GetPosition();
            t.localRotation = m.rotation;
            t.localScale = m.lossyScale;
        }

        /// <summary>Places a static model instance with HPL2 transform values.</summary>
        void PlaceModel(Transform t, string daeAbsolute, Vector3 hplPosition, Vector3 hplRotation, Vector3 scale)
        {
            if (!IsZUp(daeAbsolute) && LocalMatrix(t).isIdentity)
            {
                // Exact path (also keeps negative scales intact).
                t.SetLocalPositionAndRotation(HplSpace.Position(hplPosition), HplSpace.Rotation(hplRotation));
                t.localScale = scale;
                return;
            }
            var placement = Matrix4x4.TRS(HplSpace.Position(hplPosition), HplSpace.Rotation(hplRotation), scale);
            if (!IsZUp(daeAbsolute))
            {
                SetLocalMatrix(t, placement * LocalMatrix(t));
                return;
            }

            // Z_UP: rebuild every mesh/collider node the way HPL2 places it: translation (x, z, y) of the node
            // in the file's axes, node-local vertices as (-x, z, y), node scale. Node rotations are not used
            // (none in the Z_UP static objects of the game except outdoor foliage).
            Transform group = t.parent;
            var targets = new List<(Transform node, Matrix4x4 matrix)>();
            foreach (var node in t.GetComponentsInChildren<Transform>(true))
            {
                if (node.GetComponent<MeshFilter>() == null && node.GetComponent<Collider>() == null) continue;
                Matrix4x4 unity = group.worldToLocalMatrix * node.localToWorldMatrix;
                Vector3 raw = (UndoUnityZUp * unity).GetPosition(); // file axes, X mirrored
                Matrix4x4 hpl = Matrix4x4.Translate(new Vector3(raw.x, raw.z, raw.y)) * ZUpNodeCorrection * Matrix4x4.Scale(unity.lossyScale);
                targets.Add((node, placement * hpl));
            }
            SetLocalMatrix(t, placement);
            foreach (var (node, matrix) in targets.OrderBy(x => Depth(x.node)))
            {
                Matrix4x4 parentToGroup = group.worldToLocalMatrix * node.parent.localToWorldMatrix;
                SetLocalMatrix(node, parentToGroup.inverse * matrix);
            }
        }

        static void SetStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticFlags);
        }

        static void SetCastShadows(GameObject go, bool castShadows)
        {
            if (castShadows) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------- Static objects ----------

        void PlaceStatic(XmlElement e, Transform parent)
        {
            string dae = Indexed(_staticFiles, e, "FileIndex");
            var prefab = Model(dae);
            if (prefab == null) return;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = e.Str("Name");
            PlaceModel(go.transform, dae, e.Vec3("WorldPos"), e.Vec3("Rotation"), e.Vec3("Scale", Vector3.one));
            SetCastShadows(go, e.Bool("CastShadows", true));

            // WorldLoaderHplMap.cpp CreateStaticObjectEntity: collider shape nodes replace the visual mesh as collision.
            var colliders = go.GetComponentsInChildren<Collider>(true);
            if (!e.Bool("Collides", true))
            {
                foreach (var c in colliders) c.enabled = false;
            }
            else if (!colliders.Any(c => c.name.StartsWith("_collider", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null && filter.GetComponent<Renderer>() != null && filter.GetComponent<Collider>() == null)
                        filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
            SetStatic(go);
            Count("static objects");
        }

        // ---------- Primitives ----------

        void BuildPlane(XmlElement e, Transform parent)
        {
            var mesh = PlaneMesh(e);
            if (mesh == null) return;
            var go = new GameObject(e.Str("Name"));
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(HplSpace.Position(e.Vec3("WorldPos")), HplSpace.Rotation(e.Vec3("Rotation")));
            go.transform.localScale = e.Vec3("Scale", Vector3.one);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LevelMaterial(e.Str("Material"));
            SetCastShadows(go, e.Bool("CastShadows", true));
            if (e.Bool("Collides", true)) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            if (!e.Bool("Active", true)) go.SetActive(false);
            SetStatic(go);
            Count("planes");
        }

        /// <summary>Port of MeshCreator::CreatePlane: an axis-aligned quad between two corners, UVs per corner.</summary>
        Mesh PlaneMesh(XmlElement e)
        {
            Vector3 c1 = e.Vec3("StartCorner"), c2 = e.Vec3("EndCorner");
            Vector3 diff = c2 - c1;
            int normalAxis = -1, sameCount = 0;
            var axes = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                if (diff[i] == 0f) { normalAxis = i; sameCount++; }
                else axes.Add(i);
            }
            if (normalAxis < 0 || sameCount > 1)
            {
                _log.Warn($"Plane '{e.Str("Name")}': corners are not an axis-aligned rectangle, skipped");
                return null;
            }

            Vector3 center = (c1 + c2) * 0.5f;
            var temp = new Vector3[4];
            temp[0] = c1;
            temp[1] = c2;
            for (int i = 2; i < 4; i++)
            {
                temp[i][normalAxis] = temp[0][normalAxis];
                temp[i][axes[0]] = temp[i - 2][axes[0]];
                temp[i][axes[1]] = temp[3 - i][axes[1]];
            }
            var coords = new Vector3[4];
            foreach (var p in temp)
            {
                Vector3 corner = p - center;
                int index = corner[axes[0]] <= 0f ? (corner[axes[1]] > 0f ? 0 : 1) : (corner[axes[1]] > 0f ? 3 : 2);
                coords[index] = p;
            }
            Vector3 first = coords[1];

            var vertices = new Vector3[4];
            var uvs = new Vector2[4];
            var normals = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = HplSpace.Position(coords[i] - first);
                uvs[i] = HplSpace.Uv(e.Vec2($"Corner{i + 1}UV"));
                normals[i] = Vector3.up; // HPL2 always uses (0, 1, 0); planes in maps lie in XZ and are rotated.
            }
            var mesh = new Mesh { name = e.Str("Name"), vertices = vertices, uv = uvs, normals = normals };
            mesh.triangles = new[] { 0, 1, 2, 2, 3, 0 };
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return AddGenerated(mesh);
        }

        // ---------- Decals ----------

        void BuildDecal(XmlElement e, Transform parent)
        {
            string matFile = Indexed(_decalFiles, e, "MaterialIndex") ?? _index.Find(e.Str("Material"));
            var baseMaterial = matFile != null ? LevelMaterial(matFile) : null;
            var mesh = DecalMesh(e);
            if (mesh == null) return;

            var go = new GameObject(e.Str("Name"));
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.sharedMaterial = DecalVariant(baseMaterial, e.Color("Color", Color.white));
            if (!e.Bool("Active", true)) go.SetActive(false);
            SetStatic(go);
            Count("decals");
        }

        /// <summary>Decal geometry is stored projected, in world space (EngineFileLoading.cpp LoadDecalMeshHelper).</summary>
        Mesh DecalMesh(XmlElement e)
        {
            var data = e.Child("DecalMesh");
            if (data == null) return null;
            float[] positions = HplXml.ParseFloats(data.Child("Positions")?.Str("Array") ?? "");
            float[] normals = HplXml.ParseFloats(data.Child("Normals")?.Str("Array") ?? "");
            float[] texCoords = HplXml.ParseFloats(data.Child("TexCoords")?.Str("Array") ?? "");
            int[] indices = HplXml.ParseInts(data.Child("Indices")?.Str("Array") ?? "");
            int count = data.Int("NumVerts");
            if (count <= 0 || indices.Length == 0 || positions.Length < count * 4 || texCoords.Length < count * 3)
            {
                _log.Warn($"Decal '{e.Str("Name")}' has no or broken geometry, skipped");
                return null;
            }

            var vertices = new Vector3[count];
            var vertexNormals = new Vector3[count];
            var uvs = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = HplSpace.Position(new Vector3(positions[i * 4], positions[i * 4 + 1], positions[i * 4 + 2]));
                if (normals.Length >= count * 3)
                    vertexNormals[i] = HplSpace.Direction(new Vector3(normals[i * 3], normals[i * 3 + 1], normals[i * 3 + 2]));
                uvs[i] = HplSpace.Uv(new Vector2(texCoords[i * 3], texCoords[i * 3 + 1]));
            }
            var mesh = new Mesh { name = e.Str("Name") };
            if (count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            if (normals.Length >= count * 3) mesh.normals = vertexNormals;
            mesh.triangles = indices;
            if (normals.Length < count * 3) mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return AddGenerated(mesh);
        }

        Material DecalVariant(Material baseMaterial, Color color)
        {
            if (baseMaterial == null) return null;
            var c = new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), Mathf.Clamp01(color.a));
            if (c == Color.white || !baseMaterial.HasProperty("_BaseColor")) return baseMaterial;
            return Variant(baseMaterial, c, m => m.SetColor("_BaseColor", c));
        }

        Material Variant(Material baseMaterial, Color color, Action<Material> setup)
        {
            string key = AssetDatabase.GetAssetPath(baseMaterial) + "|" + ColorKey(color);
            if (_variants.TryGetValue(key, out var variant)) return variant;
            variant = new Material(baseMaterial) { name = baseMaterial.name + " " + ColorKey(color) };
            setup(variant);
            HDMaterial.ValidateMaterial(variant);
            _variants[key] = AddGenerated(variant);
            return variant;
        }

        static string ColorKey(Color c) => $"({c.r:0.###} {c.g:0.###} {c.b:0.###} {c.a:0.###})";

        // ---------- Entities ----------

        void PlaceEntity(XmlElement e, Transform parent)
        {
            string entFile = Indexed(_entityFiles, e, "FileIndex");
            if (entFile == null)
            {
                _log.Warn($"Entity '{e.Str("Name")}': file index {e.Str("FileIndex")} unresolved");
                return;
            }
            var ent = Entity(entFile);
            string name = e.Str("Name");
            Vector3 scale = e.Vec3("Scale", Vector3.one);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(HplSpace.Position(e.Vec3("WorldPos")), HplSpace.Rotation(e.Vec3("Rotation")));

            var entity = go.AddComponent<AmnesiaEntity>();
            entity.file = AmnesiaPaths.ToSourceRelative(entFile);
            entity.hplId = e.Int("ID", -1);
            entity.entityType = ent.EntityType;
            entity.entitySubType = ent.EntitySubType;
            entity.scale = scale;
            entity.variables = new List<HplVariable>(ent.Variables);
            entity.instanceVariables = HplXml.Variables(e.Child("UserVariables"));

            BuildEntityModel(ent, go.transform, scale);
            BuildEntityBodies(ent, go.transform, scale);
            foreach (var child in ent.WorldEntities)
            {
                try { BuildWorldEntity(child, go.transform, name + "_", scale); }
                catch (Exception ex) { _log.Error($"{entity.file} {child.Name} '{child.Str("Name")}': {ex.Message}"); }
            }

            // Map instance CastShadows variable (Lux entities read it in their loaders).
            if (HplVariable.Find(entity.instanceVariables, "CastShadows") == "false") SetCastShadows(go, false);
            // Developer commentary icons only exist in commentary mode.
            bool commentary = ent.EntityType == "CommentaryIcon";
            if (!e.Bool("Active", true) || commentary) go.SetActive(false);
            Count("entities");
        }

        void BuildEntityModel(HplEntityFile ent, Transform parent, Vector3 scale)
        {
            string dae = EntityMesh(ent);
            var prefab = Model(dae);
            if (prefab == null) return;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Entities collide through the .ent shapes; collider nodes of the model are only used by static objects.
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            if (ent.HasBones || Collada(dae).HasSkin)
            {
                // Skinned meshes are placed whole with the entity scale; .ent sub mesh transforms are not used.
                SetLocalMatrix(model.transform, Matrix4x4.Scale(scale) * ModelCorrection(dae) * LocalMatrix(model.transform));
                return;
            }
            Matrix4x4 correction = NodeCorrection(dae);

            // EntityLoader_Object.cpp: each sub mesh gets the .ent transform relative to the entity, replacing
            // the Collada node transform (e.g. the castle door mesh is turned 180° there). Node scale stays.
            var nodes = model.GetComponentsInChildren<Transform>(true);
            var renderNodes = nodes.Where(t => t.GetComponent<Renderer>() != null).ToList();
            var targets = new List<(HplEntityFile.SubMesh sub, Transform node, Matrix4x4 matrix)>();
            var unmatched = new List<HplEntityFile.SubMesh>();
            foreach (var sub in ent.SubMeshes)
            {
                var node = renderNodes.FirstOrDefault(t => t.name == sub.Name) ?? nodes.FirstOrDefault(t => t.name == sub.Name);
                if (node == null) { unmatched.Add(sub); continue; }
                targets.Add((sub, node, SubMeshMatrix(sub, scale, correction, node, parent)));
                renderNodes.Remove(node);
            }
            // Unity merges a lone Collada node into the model root and renames it after the file.
            if (unmatched.Count == 1 && renderNodes.Count == 1)
            {
                targets.Add((unmatched[0], renderNodes[0], SubMeshMatrix(unmatched[0], scale, correction, renderNodes[0], parent)));
                unmatched.Clear();
            }
            foreach (var sub in unmatched)
                _log.Warn($"{AmnesiaPaths.ToSourceRelative(ent.Path)}: sub mesh '{sub.Name}' not found in the model");

            // Parents first, so children are computed against their final parent transform.
            foreach (var (_, node, matrix) in targets.OrderBy(t => Depth(t.node)))
            {
                Matrix4x4 parentToEntity = parent.worldToLocalMatrix * node.parent.localToWorldMatrix;
                SetLocalMatrix(node, parentToEntity.inverse * matrix);
            }

            foreach (var (sub, node, _) in targets)
            {
                if (string.IsNullOrEmpty(sub.Material)) continue;
                var material = LevelMaterial(sub.Material, ent.Path);
                var renderer = node.GetComponent<Renderer>();
                if (material != null && renderer != null)
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            }
        }

        /// <summary>Sub mesh transform relative to the entity: .ent values (scaled by the instance) × imported node scale.</summary>
        static Matrix4x4 SubMeshMatrix(HplEntityFile.SubMesh sub, Vector3 entityScale, Matrix4x4 correction, Transform node, Transform entity)
        {
            Vector3 nodeScale = (entity.worldToLocalMatrix * node.localToWorldMatrix).lossyScale;
            return Matrix4x4.TRS(HplSpace.Position(Vector3.Scale(sub.Position, entityScale)),
                       HplSpace.Rotation(sub.Rotation), Vector3.Scale(sub.Scale, entityScale))
                   * correction * Matrix4x4.Scale(nodeScale);
        }

        static int Depth(Transform t)
        {
            int depth = 0;
            for (; t.parent != null; t = t.parent) depth++;
            return depth;
        }

        void BuildEntityBodies(HplEntityFile ent, Transform parent, Vector3 scale)
        {
            foreach (var body in ent.Bodies)
            {
                var go = new GameObject(body.Name);
                go.transform.SetParent(parent, false);
                go.transform.SetLocalPositionAndRotation(HplSpace.Position(Vector3.Scale(body.Position, scale)), HplSpace.Rotation(body.Rotation));
                var data = go.AddComponent<AmnesiaBody>();
                data.mass = body.Mass;
                data.material = body.Material;
                data.collideCharacter = body.CollideCharacter;
                data.collideNonCharacter = body.CollideNonCharacter;
                data.hasGravity = body.HasGravity;
                data.linearDamping = body.LinearDamping;
                data.angularDamping = body.AngularDamping;
                data.blocksSound = body.BlocksSound;

                foreach (int id in body.ShapeIds)
                {
                    var shape = ent.Shapes.FirstOrDefault(s => s.Id == id);
                    if (shape != null) BuildShape(shape, go.transform, scale);
                }
            }
        }

        /// <summary>EntityLoader_Object.cpp CreateCollideShape: size = Scale × instance scale, offset relative to the body.</summary>
        void BuildShape(HplEntityFile.Shape shape, Transform body, Vector3 scale)
        {
            var go = new GameObject($"Shape_{shape.Id}_{shape.Type}");
            go.transform.SetParent(body, false);
            go.transform.SetLocalPositionAndRotation(HplSpace.Position(Vector3.Scale(shape.RelativePosition, scale)), HplSpace.Rotation(shape.RelativeRotation));
            Vector3 size = Abs(Vector3.Scale(shape.Size, scale));
            switch (shape.Type)
            {
                case "box":
                    go.AddComponent<BoxCollider>().size = size;
                    break;
                case "sphere":
                    go.AddComponent<SphereCollider>().radius = Mathf.Max(size.x, size.y, size.z);
                    break;
                case "cylinder":
                    // (radius, height) along Y. Unity has no cylinder collider: a convex unit cylinder, scaled.
                    go.transform.localScale = new Vector3(size.x, size.y, size.x);
                    var collider = go.AddComponent<MeshCollider>();
                    collider.sharedMesh = Cylinder();
                    collider.convex = true;
                    break;
                case "capsule":
                    var capsule = go.AddComponent<CapsuleCollider>();
                    capsule.direction = 1;
                    capsule.radius = size.x;
                    capsule.height = Mathf.Max(size.y, size.x * 2f);
                    break;
                default:
                    _log.Warn($"Unknown shape type '{shape.Type}'");
                    break;
            }
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        // ---------- Lights, billboards, particle systems, sounds ----------

        /// <summary>Map-level or .ent world entity. Inside an entity positions scale with it (EntityLoader_Object.cpp).</summary>
        void BuildWorldEntity(XmlElement e, Transform parent, string namePrefix, Vector3 positionScale)
        {
            switch (e.Name)
            {
                case "PointLight":
                case "SpotLight":
                    BuildLight(e, parent, namePrefix, positionScale);
                    break;
                case "Billboard":
                    BuildBillboard(e, parent, namePrefix, positionScale);
                    break;
                case "ParticleSystem":
                {
                    var go = Child(e, parent, namePrefix, positionScale, HplSpace.Rotation(e.Vec3("Rotation")));
                    var ps = go.AddComponent<AmnesiaParticleSystem>();
                    ps.file = e.Str("File");
                    ps.color = e.Color("Color", Color.white);
                    ps.fadeAtDistance = e.Bool("FadeAtDistance");
                    ps.minFadeDistance = new Vector2(e.Float("MinFadeDistanceStart"), e.Float("MinFadeDistanceEnd"));
                    ps.maxFadeDistance = new Vector2(e.Float("MaxFadeDistanceStart"), e.Float("MaxFadeDistanceEnd"));
                    FinishChild(e, go, "particle systems (placeholders)");
                    break;
                }
                case "Sound":
                {
                    var go = Child(e, parent, namePrefix, positionScale, HplSpace.Rotation(e.Vec3("Rotation")));
                    var sound = go.AddComponent<AmnesiaSound>();
                    sound.soundEntityFile = e.Str("SoundEntityFile");
                    sound.useDefault = e.Bool("UseDefault");
                    sound.minDistance = e.Float("MinDistance");
                    sound.maxDistance = e.Float("MaxDistance");
                    sound.volume = e.Float("Volume", 1f);
                    FinishChild(e, go, "sounds (placeholders)");
                    break;
                }
                default:
                    WarnOnce($"'{e.Name}' is not supported yet (skipped)");
                    break;
            }
        }

        GameObject Child(XmlElement e, Transform parent, string namePrefix, Vector3 positionScale, Quaternion rotation)
        {
            var go = new GameObject(namePrefix + e.Str("Name"));
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(HplSpace.Position(Vector3.Scale(e.Vec3("WorldPos"), positionScale)), rotation);
            return go;
        }

        void FinishChild(XmlElement e, GameObject go, string counter)
        {
            if (!e.Bool("Active", true)) go.SetActive(false);
            Count(counter);
        }

        void BuildLight(XmlElement e, Transform parent, string namePrefix, Vector3 positionScale)
        {
            bool spot = e.Name == "SpotLight";
            string gobo = e.Str("Gobo");
            string goboFile = gobo.Length > 0 ? _index.Find(gobo) : null;
            Texture2D cookie = null;
            if (spot && goboFile != null && _cookies.TryGetValue(goboFile, out string cookiePath))
                cookie = AssetDatabase.LoadAssetAtPath<Texture2D>(cookiePath);
            if (!spot && gobo.Length > 0) WarnOnce("Point light gobos (cube maps) are not converted yet");

            Vector3 rotation = e.Vec3("Rotation");
            var go = Child(e, parent, namePrefix, positionScale, spot ? HplSpace.LookRotation(rotation) : HplSpace.Rotation(rotation));
            var type = !spot ? LightType.Point : cookie != null ? LightType.Pyramid : LightType.Spot;
            var hd = go.AddHDLight(type);
            var light = go.GetComponent<Light>();

            if (spot)
            {
                // FOV is the full (vertical) angle; HPL2 spots are frustums, with a gobo exactly like HDRP pyramids.
                float fov = e.Float("FOV", 1f);
                float aspect = e.Float("Aspect", 1f);
                light.spotAngle = Mathf.Clamp(fov * Mathf.Rad2Deg, 1f, 179f);
                light.innerSpotAngle = type == LightType.Pyramid
                    ? Mathf.Clamp(2f * Mathf.Atan(aspect * Mathf.Tan(fov * 0.5f)) * Mathf.Rad2Deg, 1f, 179f)
                    : light.spotAngle * SpotInnerAngleFraction;
                if (cookie != null) hd.SetCookie(cookie);
                hd.shadowNearPlane = Mathf.Max(0.05f, e.Float("NearClipPlane", 0.1f));
            }

            bool shadows = e.Bool("CastShadows");
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            if (shadows)
            {
                hd.SetShadowResolutionOverride(false);
                hd.SetShadowResolutionLevel(e.Str("ShadowResolution", "High").ToLowerInvariant() switch
                {
                    "low" => 0,
                    "medium" => 1,
                    _ => 2,
                });
            }

            var data = go.AddComponent<AmnesiaLight>();
            data.diffuseColor = e.Color("DiffuseColor", Color.white);
            data.radius = e.Float("Radius", 1f);
            data.falloffMap = e.Str("FalloffMap");
            data.gobo = gobo;
            data.flickerActive = e.Bool("FlickerActive");
            data.flickerOffColor = e.Color("FlickerOffColor", Color.black);
            data.flickerOffRadius = e.Float("FlickerOffRadius");
            data.flickerOnLength = new Vector2(e.Float("FlickerOnMinLength"), e.Float("FlickerOnMaxLength"));
            data.flickerOffLength = new Vector2(e.Float("FlickerOffMinLength"), e.Float("FlickerOffMaxLength"));
            data.flickerFade = e.Bool("FlickerFade");
            data.flickerOnFadeLength = new Vector2(e.Float("FlickerOnFadeMinLength"), e.Float("FlickerOnFadeMaxLength"));
            data.flickerOffFadeLength = new Vector2(e.Float("FlickerOffFadeMinLength"), e.Float("FlickerOffFadeMaxLength"));
            data.flickerOnSound = e.Str("FlickerOnSound");
            data.flickerOffSound = e.Str("FlickerOffSound");
            data.flickerOnPS = e.Str("FlickerOnPS");
            data.flickerOffPS = e.Str("FlickerOffPS");
            data.Apply();
            FinishChild(e, go, spot ? "spot lights" : "point lights");
        }

        void BuildBillboard(XmlElement e, Transform parent, string namePrefix, Vector3 positionScale)
        {
            var go = Child(e, parent, namePrefix, positionScale, HplSpace.Rotation(e.Vec3("Rotation")));
            Vector2 size = e.Vec2("BillboardSize", Vector2.one);
            Color color = e.Color("BillboardColor", Color.white);
            var material = BillboardVariant(LevelMaterial(e.Str("MaterialFile")), color);
            // Billboard.cpp IsVisible: black billboards are hidden.
            bool visible = color.r > 0f || color.g > 0f || color.b > 0f;
            var kind = e.Str("BillboardType") switch
            {
                "Axis" => AmnesiaBillboard.Kind.Axis,
                "FixedAxis" => AmnesiaBillboard.Kind.FixedAxis,
                _ => AmnesiaBillboard.Kind.Point,
            };

            if (kind == AmnesiaBillboard.Kind.FixedAxis)
            {
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
                go.AddComponent<MeshFilter>().sharedMesh = Quad();
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.sharedMaterial = material;
                renderer.enabled = visible;
            }

            var billboard = go.AddComponent<AmnesiaBillboard>();
            billboard.kind = kind;
            billboard.size = size;
            billboard.mesh = Quad();
            billboard.material = material;
            billboard.visible = visible;
            billboard.axis = HplSpace.Direction(e.Vec3("Axis", Vector3.up));
            billboard.forwardOffset = e.Float("BillboardOffset");
            billboard.color = color;
            billboard.isHalo = e.Bool("IsHalo");
            billboard.haloSourceSize = e.Vec3("HaloSourceSize", Vector3.one);
            string connect = e.Str("ConnectLight");
            billboard.connectLight = connect.Length > 0 ? namePrefix + connect : "";
            FinishChild(e, go, "billboards");
        }

        /// <summary>The billboard color multiplies the material (vertex color in HPL2); quads are two-sided.</summary>
        Material BillboardVariant(Material baseMaterial, Color color)
        {
            if (baseMaterial == null) return null;
            return Variant(baseMaterial, color, m =>
            {
                m.SetFloat("_DoubleSidedEnable", 1f);
                if (m.shader.name == "HDRP/Unlit")
                {
                    // Additive HPL2 blending ignores alpha; HDRP multiplies additive color by it.
                    bool additive = Mathf.Approximately(m.GetFloat("_BlendMode"), 1f);
                    MaterialConverter.SetUnlitColor(m, new Color(color.r, color.g, color.b, additive ? 1f : color.a));
                }
                else if (m.HasProperty("_BaseColor"))
                {
                    m.SetColor("_BaseColor", color);
                }
            });
        }

        /// <summary>Unit billboard quad (Billboard.cpp), mirrored to Unity space; UVs as in HPL2 (v flipped).</summary>
        Mesh Quad()
        {
            if (_quad != null) return _quad;
            var hplCorners = new[] { new Vector2(0.5f, -0.5f), new Vector2(-0.5f, -0.5f), new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f) };
            var vertices = new Vector3[4];
            var uvs = new Vector2[4];
            var normals = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = HplSpace.Position(hplCorners[i]);
                uvs[i] = HplSpace.Uv(new Vector2(hplCorners[i].x + 0.5f, hplCorners[i].y + 0.5f));
                normals[i] = Vector3.forward;
            }
            _quad = new Mesh { name = "Billboard quad", vertices = vertices, uv = uvs, normals = normals };
            _quad.triangles = new[] { 0, 1, 2, 2, 3, 0 };
            _quad.RecalculateTangents();
            _quad.RecalculateBounds();
            return AddGenerated(_quad);
        }

        /// <summary>Convex collision cylinder: radius 1, height 1, along Y, centered.</summary>
        Mesh Cylinder()
        {
            if (_cylinder != null) return _cylinder;
            const int segments = 16;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(a), -0.5f, Mathf.Sin(a)));
                vertices.Add(new Vector3(Mathf.Cos(a), 0.5f, Mathf.Sin(a)));
            }
            for (int i = 0; i < segments; i++)
            {
                int a = i * 2, b = (i + 1) % segments * 2;
                triangles.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 });
                if (i > 0 && i < segments - 1)
                {
                    triangles.AddRange(new[] { 0, b, a });
                    triangles.AddRange(new[] { 1, a + 1, b + 1 });
                }
            }
            _cylinder = new Mesh { name = "Collision cylinder", vertices = vertices.ToArray(), triangles = triangles.ToArray() };
            _cylinder.RecalculateNormals();
            _cylinder.RecalculateBounds();
            return AddGenerated(_cylinder);
        }

        // ---------- Areas ----------

        void BuildArea(XmlElement e, Transform parent)
        {
            var go = new GameObject(e.Str("Name"));
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(HplSpace.Position(e.Vec3("WorldPos")), HplSpace.Rotation(e.Vec3("Rotation")));
            var area = go.AddComponent<AmnesiaArea>();
            area.areaType = e.Str("AreaType");
            area.hplId = e.Int("ID", -1);
            area.size = Abs(e.Vec3("Scale", Vector3.one));
            area.variables = HplXml.Variables(e.Child("UserVariables"));
            if (area.areaType == "PlayerStart")
            {
                _playerStarts.Add(go.transform);
            }
            else
            {
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = area.size;
            }
            if (!e.Bool("Active", true)) go.SetActive(false);
            Count($"areas ({area.areaType})");
        }

        // ---------- Environment ----------

        void BuildEnvironment(XmlElement mapData, Transform root)
        {
            AssetDatabase.DeleteAsset(VolumePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumePath);

            // HPL2 has no ambient light or sky lighting unless the map has a sky box.
            var environment = profile.Add<VisualEnvironment>(true);
            environment.skyType.Override(0);
            if (mapData.Bool("SkyBoxActive")) _log.Warn("Map has a sky box: not converted yet (black background)");

            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(HplLighting.ExposureEV100);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.None);

            // deferred_fog_frag.glsl: alpha = ((d - start) / (end - start))^falloff × color.a, constant color.
            // HDRP fog: 1 - exp(-d / meanFreePath), capped at maxFogDistance. Matched at the fog end.
            var fog = profile.Add<Fog>(true);
            bool fogActive = mapData.Bool("FogActive");
            fog.enabled.Override(fogActive);
            if (fogActive)
            {
                Color color = mapData.Color("FogColor", Color.white);
                float end = Mathf.Max(1f, mapData.Float("FogEnd", 40f));
                float alpha = Mathf.Clamp(color.a, 0.01f, 0.99f);
                fog.colorMode.Override(FogColorMode.ConstantColor);
                fog.color.Override(HplLighting.Radiance(new Color(color.r, color.g, color.b, 1f)));
                fog.meanFreePath.Override(-end / Mathf.Log(1f - alpha));
                fog.maxFogDistance.Override(end);
                fog.baseHeight.Override(10000f);
                fog.maximumHeight.Override(10001f);
                fog.enableVolumetricFog.Override(false);
            }
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);

            var volume = new GameObject("Level Volume").AddComponent<Volume>();
            volume.transform.SetParent(root, false);
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            // Free camera at the first player start, eye height, looking where the player would (-Z in HPL2).
            var cameraObject = new GameObject("Camera (fly: RMB + WASD)") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            var cameraData = cameraObject.AddComponent<HDAdditionalCameraData>();
            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            // Without a sky HPL2 clears to black (HDRP's default background is dark blue).
            cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            cameraData.backgroundColorHDR = Color.black;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<FlyCamera>();
            var start = _playerStarts.FirstOrDefault(t => t.name == "PlayerStartArea_1") ?? _playerStarts.FirstOrDefault();
            if (start != null)
                cameraObject.transform.SetPositionAndRotation(start.position + Vector3.up * CameraEyeHeight, start.rotation * Quaternion.Euler(0f, 180f, 0f));
        }
    }
}

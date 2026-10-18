using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RTXMnesia.Dev;
using RTXMnesia.Effects;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
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
    /// - Area → <see cref="AmnesiaArea"/> (+ trigger box). Particle systems → <see cref="AmnesiaParticleSystem"/>,
    ///   sounds → <see cref="AmnesiaSound"/> (created at runtime by HplMap from the particle / sound banks).
    /// - Fog, fixed exposure and "no sky" (HPL2 has no ambient light) → level volume profile.
    /// - <see cref="AmnesiaPlayer"/> with its camera at the first player start, settings from config/game.cfg.
    /// </summary>
    public class LevelConverter
    {
        public const string LevelsRoot = AmnesiaPaths.ImportedAssetsRoot + "/Levels";

        // Spots without a gobo use the "short fade" falloff over the cone: full strength until ~80% of the angle.
        const float SpotInnerAngleFraction = 0.8f;
        const StaticEditorFlags StaticFlags =
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

        readonly string _mapAbsolute;
        readonly string _mapRelative;
        readonly string _mapName;
        readonly bool _menuBackground;
        readonly HplCampaign _campaign;
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
        HplConfig _gameConfig;
        HplSurfaces _surfaces;
        readonly Dictionary<Material, string> _physicsMaterialNames = new();
        Mesh _quad, _cylinder;

        public string ScenePath => _levelFolder + ".unity";
        string GeneratedPath => _levelFolder + "/" + _mapName + "_generated.asset";
        string VolumePath => _levelFolder + "/" + _mapName + "_volume.asset";

        LevelConverter(string mapAbsolute, bool menuBackground = false)
        {
            _menuBackground = menuBackground;
            _mapAbsolute = Path.GetFullPath(mapAbsolute);
            _mapRelative = AmnesiaPaths.ToSourceRelative(_mapAbsolute);
            _campaign = HplCampaign.ForMap(_mapRelative);
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

        public static string ConvertMenuBackground(string mapAbsolute)
        {
            var converter = new LevelConverter(mapAbsolute, true);
            converter.Run();
            return converter.ScenePath;
        }

        /// <summary>Our generated test maps use the same resource and entity pipeline as game maps.</summary>
        internal static string ConvertGenerated(XmlDocument document, string mapRelative, Action<GameObject> finish)
        {
            var converter = new LevelConverter(Path.Combine(AmnesiaPaths.SourceRoot, mapRelative));
            converter.Run(document, finish);
            return converter.ScenePath;
        }

        /// <summary>
        /// .ent files outside any map (the player's hands and hand objects): built like map entities at the origin
        /// (model, lights, billboards, particles, sounds), saved as one prefab with its generated meshes next to it.
        /// <paramref name="finish"/> adds what the game needs on top before the prefab is saved.
        /// </summary>
        public static GameObject BuildEntitiesPrefab(string prefabPath, IReadOnlyList<(string entAbsolute, string name)> entities,
                                                     Action<GameObject, ImportLog> finish)
        {
            var converter = new LevelConverter(entities[0].entAbsolute);
            return converter.RunStandalone(prefabPath, entities, finish);
        }

        GameObject RunStandalone(string prefabPath, IReadOnlyList<(string entAbsolute, string name)> entities, Action<GameObject, ImportLog> finish)
        {
            _staticFiles = Array.Empty<string>();
            _decalFiles = Array.Empty<string>();
            _entityFiles = entities.Select(e => e.entAbsolute).ToArray();
            PrepareResources(null);
            _gameConfig = ConfigConverter.Convert(_campaign.GameConfig, _log, _campaign);
            _surfaces = SurfaceConverter.Convert(_log, _campaign);

            string generated = Path.ChangeExtension(prefabPath, null) + "_generated.asset";
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(prefabPath)));
            AssetDatabase.Refresh();
            AssetDatabase.DeleteAsset(generated);
            _container = new Mesh { name = Path.GetFileNameWithoutExtension(prefabPath) + " generated" };
            AssetDatabase.CreateAsset(_container, generated);

            var root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            foreach (var (ent, name) in entities)
                BuildEntity(ent, name, Vector3.zero, Vector3.zero, Vector3.one, -1, new List<HplVariable>(), root.transform);
            finish?.Invoke(root, _log);
            AssetDatabase.SaveAssets();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            _log.Info($"Prefab: {prefabPath}");
            _log.Save($"Entities: {prefabPath}");
            return prefab;
        }

        void Run(XmlDocument document = null, Action<GameObject> finish = null)
        {
            var doc = document ?? new XmlDocument();
            if (document == null) doc.Load(_mapAbsolute);
            var mapData = doc.DocumentElement.Child("MapData") ?? throw new InvalidDataException("No MapData element");
            var contents = mapData.Child("MapContents") ?? throw new InvalidDataException("No MapContents element");

            _staticFiles = FileIndex(contents, "FileIndex_StaticObjects");
            _entityFiles = FileIndex(contents, "FileIndex_Entities");
            _decalFiles = FileIndex(contents, "FileIndex_Decals");

            PrepareResources(contents);
            _gameConfig = ConfigConverter.Convert(_campaign.GameConfig, _log, _campaign);
            _surfaces = SurfaceConverter.Convert(_log, _campaign);

            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(_levelFolder));
            AssetDatabase.Refresh();
            AssetDatabase.DeleteAsset(GeneratedPath);
            _container = new Mesh { name = _mapName + " generated" };
            AssetDatabase.CreateAsset(_container, GeneratedPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(_mapName);
            var level = root.AddComponent<AmnesiaLevel>();
            level.mapFile = _mapRelative;
            level.fogActive = mapData.Bool("FogActive");
            level.fogColor = mapData.Color("FogColor", Color.white);
            level.fogStart = mapData.Float("FogStart");
            level.fogEnd = mapData.Float("FogEnd", 40f);
            level.fogFalloffExp = mapData.Float("FogFalloffExp", 1f);
            level.fogCulling = mapData.Bool("FogCulling", true);
            level.skyBoxActive = mapData.Bool("SkyBoxActive");
            level.skyBoxColor = mapData.Color("SkyBoxColor", Color.white);
            level.skyBoxTexture = mapData.Str("SkyBoxTexture");
            Transform Group(string name)
            {
                var group = new GameObject(name).transform;
                group.SetParent(root.transform, false);
                return group;
            }

            Transform statics = Group("StaticObjects"), primitives = Group("Primitives"), decals = Group("Decals"),
                entities = Group("Entities"), lights = Group("Lights"), billboards = Group("Billboards"),
                particles = Group("ParticleSystems"), sounds = Group("Sounds"), areas = Group("Areas"), fogAreas = Group("FogAreas");

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
                        case "FogArea": BuildWorldEntity(e, fogAreas, "", Vector3.one); break;
                        default: BuildWorldEntity(e, lights, "", Vector3.one); break;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"{e.Name} '{e.Str("Name")}': {ex.Message}");
                }
            }

            BuildEntityTemplates(root.transform, entities);
            BuildEnvironment(mapData, root.transform);
            BuildScriptRuntime(root);
            finish?.Invoke(root);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            // Map changes load the scenes by name from the build list.
            if (!_menuBackground) MainMenuConverter.ConfigureBuildScenes();

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
            if (contents != null)
            {
                foreach (var plane in contents.Child("Primitives").Children()) AddRef(mats, plane.Str("Material"));
                foreach (var e in contents.Child("Entities").Children()) AddWorldEntity(e);
            }
            foreach (string decal in _decalFiles) if (decal != null) mats.Add(decal);

            var missingModels = daes.Where(d => !ModelConverter.IsConverted(d)).ToList();
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

        // Static meshes come from HplModelBuilder with the engine's node transforms: an instance only needs the
        // HPL2 placement. Skinned meshes are Unity imports: Unity's whole converted Z_UP model vs HPL2 is turned
        // 180° around Y.
        static readonly Matrix4x4 ZUpModelCorrection = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));

        bool IsZUp(string daeAbsolute) => Collada(daeAbsolute).UpAxis == "Z_UP";

        Matrix4x4 ModelCorrection(string daeAbsolute) => IsZUp(daeAbsolute) ? ZUpModelCorrection : Matrix4x4.identity;

        static bool IsBuiltModel(GameObject prefab) => prefab.GetComponent<AmnesiaModel>() != null;

        static Matrix4x4 LocalMatrix(Transform t) => Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);

        static void SetLocalMatrix(Transform t, Matrix4x4 m)
        {
            t.localPosition = m.GetPosition();
            t.localRotation = m.rotation;
            t.localScale = m.lossyScale;
        }

        /// <summary>
        /// Places a model instance with HPL2 transform values (static object: T·R·S, CreateStaticObjectEntity).
        /// Built models carry the engine's node transforms, so this is exact; Unity-imported (skinned) ones get
        /// their root transform kept and the Z_UP correction.
        /// </summary>
        void PlaceModel(Transform t, string daeAbsolute, Vector3 hplPosition, Vector3 hplRotation, Vector3 scale, bool builtModel)
        {
            if (builtModel)
            {
                t.SetLocalPositionAndRotation(HplSpace.Position(hplPosition), HplSpace.Rotation(hplRotation));
                t.localScale = scale;
                return;
            }
            var placement = Matrix4x4.TRS(HplSpace.Position(hplPosition), HplSpace.Rotation(hplRotation), scale);
            SetLocalMatrix(t, placement * ModelCorrection(daeAbsolute) * LocalMatrix(t));
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
            bool built = IsBuiltModel(prefab);
            PlaceModel(go.transform, dae, e.Vec3("WorldPos"), e.Vec3("Rotation"), e.Vec3("Scale", Vector3.one), built);
            SetCastShadows(go, e.Bool("CastShadows", true));

            // WorldLoaderHplMap.cpp CreateStaticObjectEntity: built models already have the static collision
            // (collider nodes, else mesh colliders). Collides = false: no collision at all.
            var colliders = go.GetComponentsInChildren<Collider>(true);
            if (!e.Bool("Collides", true))
            {
                foreach (var c in colliders) c.enabled = false;
            }
            else if (!built && !colliders.Any(c => c.name.StartsWith("_collider", StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh != null && filter.GetComponent<Renderer>() != null && filter.GetComponent<Collider>() == null)
                        filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            }
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                if (c.enabled) c.sharedMaterial = StaticPhysicsMaterial(c);
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
            // The multiply shader takes the HPL2 vertex color as is: values above 1 brighten the texture before the
            // clamp to the 8-bit target (00_rainy_hall: dirt_floor01 with color 1.5).
            var c = baseMaterial.shader.name == MaterialConverter.MultiplyShader ? color
                : new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), Mathf.Clamp01(color.a));
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
            var go = BuildEntity(entFile, e.Str("Name"), e.Vec3("WorldPos"), e.Vec3("Rotation"), e.Vec3("Scale", Vector3.one),
                                 e.Int("ID", -1), HplXml.Variables(e.Child("UserVariables")), parent);
            var entity = go.GetComponent<AmnesiaEntity>();

            // Map instance CastShadows variable (Lux entities read it in their loaders).
            if (HplVariable.Find(entity.instanceVariables, "CastShadows") == "false") SetCastShadows(go, false);
            // Developer commentary icons only exist in commentary mode.
            bool commentary = entity.entityType == "CommentaryIcon";
            if (!e.Bool("Active", true) || commentary) go.SetActive(false);
            Count("entities");
        }

        /// <summary>An .ent instance (EntityLoader_Object.cpp Load): model, world entities, bodies with their children, joints.</summary>
        GameObject BuildEntity(string entFile, string name, Vector3 hplPosition, Vector3 hplRotation, Vector3 scale, int id,
                               List<HplVariable> instanceVariables, Transform parent)
        {
            var ent = Entity(entFile);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(HplSpace.Position(hplPosition), HplSpace.Rotation(hplRotation));

            var entity = go.AddComponent<AmnesiaEntity>();
            entity.file = AmnesiaPaths.ToSourceRelative(entFile);
            entity.hplId = id;
            entity.entityType = ent.EntityType;
            entity.entitySubType = ent.EntitySubType;
            entity.scale = scale;
            entity.variables = new List<HplVariable>(ent.Variables);
            entity.instanceVariables = instanceVariables;

            // Sub meshes, then world entities, form the list of children the bodies pick from by unique ID; what no
            // body takes goes to the first body.
            var attachables = new List<(int id, Transform transform)>();
            BuildEntityModel(ent, go.transform, scale, attachables);
            foreach (var child in ent.WorldEntities)
            {
                try
                {
                    var built = BuildWorldEntity(child, go.transform, name + "_", scale);
                    if (built != null) attachables.Add((child.Int("ID", -1), built.transform));
                }
                catch (Exception ex) { _log.Error($"{entity.file} {child.Name} '{child.Str("Name")}': {ex.Message}"); }
            }
            var bodies = BuildEntityBodies(ent, go.transform, scale);
            AttachEntityChildren(ent, bodies, attachables);
            BuildEntityJoints(ent, go.transform, scale, name, bodies);
            return go;
        }

        /// <summary>
        /// Entities the game creates while playing (cLuxMap::CreateEntity): broken versions (BreakEntity of objects,
        /// BrokenEntity of doors), contained items and .ent files the level script names. Built once, inactive, at
        /// scale 1 under "EntityTemplates", named by file; the map clones them.
        /// </summary>
        void BuildEntityTemplates(Transform root, Transform entities)
        {
            var queue = new Queue<(string reference, string near)>();
            foreach (var e in entities.GetComponentsInChildren<AmnesiaEntity>(true))
            {
                string near = Path.Combine(AmnesiaPaths.SourceRoot, e.file);
                foreach (string v in new[] { "BreakEntity", "BrokenEntity" })
                {
                    string s = HplVariable.Find(e.variables, v);
                    if (!string.IsNullOrEmpty(s)) queue.Enqueue((s, near));
                }
                string item = HplVariable.Find(e.instanceVariables, "ContainedItem");
                if (!string.IsNullOrEmpty(item) && item != "None") queue.Enqueue((item, near));
            }
            string hps = Path.ChangeExtension(_mapAbsolute, ".hps");
            if (File.Exists(hps))
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(hps), "\"([^\"]+\\.ent)\""))
                    queue.Enqueue((m.Groups[1].Value, null));

            Transform templates = null;
            var built = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (queue.Count > 0)
            {
                var (reference, near) = queue.Dequeue();
                // cLuxMap::CreateEntity takes the file with or without its extension.
                if (Path.GetExtension(reference) == "") reference += ".ent";
                string file = _index.Find(reference, near);
                if (file == null)
                {
                    WarnOnce($"Entity template '{reference}' not found in source/");
                    continue;
                }
                if (!built.Add(file)) continue;
                if (templates == null)
                {
                    templates = new GameObject("EntityTemplates").transform;
                    templates.SetParent(root, false);
                }
                try
                {
                    string models = EntityMesh(Entity(file));
                    var entFile = Entity(file);
                    if (models != null && !ModelConverter.IsConverted(models)) ModelConverter.ConvertModels(new List<string> { models }, $"Model for {reference}");
                    var mats = entFile.SubMeshes.Select(s => string.IsNullOrEmpty(s.Material) ? null : _index.Find(s.Material, file))
                        .Where(m => m != null && !File.Exists(AmnesiaPaths.ToAbsolute(MaterialConverter.AssetPathFor(m)))).Distinct().ToList();
                    if (mats.Count > 0) MaterialConverter.ConvertMaterials(mats, false, $"Materials for {reference}");
                    var go = BuildEntity(file, Path.GetFileNameWithoutExtension(file), Vector3.zero, Vector3.zero, Vector3.one, -1,
                                         new List<HplVariable>(), templates);
                    go.SetActive(false);
                    var ent = go.GetComponent<AmnesiaEntity>();
                    foreach (string v in new[] { "BreakEntity", "BrokenEntity" })
                    {
                        string s = HplVariable.Find(ent.variables, v);
                        if (!string.IsNullOrEmpty(s)) queue.Enqueue((s, file));
                    }
                    Count("entity templates");
                }
                catch (Exception ex) { _log.Error($"Entity template {AmnesiaPaths.ToSourceRelative(file)}: {ex.Message}"); }
            }
        }

        void BuildEntityModel(HplEntityFile ent, Transform parent, Vector3 scale, List<(int id, Transform transform)> attachables)
        {
            string dae = EntityMesh(ent);
            var prefab = Model(dae);
            if (prefab == null) return;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            // Entities collide through the .ent shapes; collider nodes of the model are only used by static objects.
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);

            if (!IsBuiltModel(prefab))
            {
                // Skinned meshes are placed whole with the entity scale; .ent sub mesh transforms are not used.
                // The whole mesh goes to the first body (ragdoll bones come with enemies).
                SetLocalMatrix(model.transform, Matrix4x4.Scale(scale) * ModelCorrection(dae) * LocalMatrix(model.transform));
                attachables.Add((int.MinValue, model.transform));
                return;
            }

            // EntityLoader_Object.cpp: each sub mesh listed in the .ent gets the matrix T(pos·s)·R(rot)·S(scale·s)
            // relative to the entity, replacing its node transform (its vertices already hold the node's own
            // scale). Sub meshes not listed keep their node transform. Sub mesh entities are not nested in HPL2,
            // so they are moved directly under the model root first.
            var subMeshNodes = model.GetComponentsInChildren<MeshFilter>(true).Select(f => f.transform).ToList();
            foreach (var node in subMeshNodes) node.SetParent(model.transform, true);
            var targets = new List<(HplEntityFile.SubMesh sub, Transform node, Matrix4x4 matrix)>();
            foreach (var sub in ent.SubMeshes)
            {
                var node = subMeshNodes.FirstOrDefault(t => t.name == sub.Name);
                if (node == null)
                {
                    _log.Warn($"{AmnesiaPaths.ToSourceRelative(ent.Path)}: sub mesh '{sub.Name}' not found in the model");
                    continue;
                }
                targets.Add((sub, node, Matrix4x4.TRS(HplSpace.Position(Vector3.Scale(sub.Position, scale)),
                                                      HplSpace.Rotation(sub.Rotation), Vector3.Scale(sub.Scale, scale))));
                subMeshNodes.Remove(node);
            }

            Matrix4x4 modelToEntity = parent.worldToLocalMatrix * model.transform.localToWorldMatrix;
            foreach (var (_, node, matrix) in targets)
                HplModelBuilder.SetLocalMatrix(node, modelToEntity.inverse * matrix, _log, $"{AmnesiaPaths.ToSourceRelative(ent.Path)} '{node.name}'");

            foreach (var (sub, node, _) in targets) attachables.Add((sub.Id, node));
            // Sub meshes the .ent does not list are not attached in HPL2; here they follow the first body too.
            foreach (var node in subMeshNodes) attachables.Add((int.MinValue, node));

            foreach (var (sub, node, _) in targets)
            {
                if (string.IsNullOrEmpty(sub.Material)) continue;
                var material = LevelMaterial(sub.Material, ent.Path);
                var renderer = node.GetComponent<Renderer>();
                if (material != null && renderer != null)
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            }
        }

        /// <summary>
        /// EntityLoader_Object.cpp: a body per &lt;Body&gt; with its shapes (a body without shapes is skipped with an
        /// warning, a shape belongs to the first body that lists it). Colliders get the body's physics material.
        /// </summary>
        List<(HplEntityFile.Body data, AmnesiaBody body)> BuildEntityBodies(HplEntityFile ent, Transform parent, Vector3 scale)
        {
            var built = new List<(HplEntityFile.Body, AmnesiaBody)>();
            var freeShapes = new List<HplEntityFile.Shape>(ent.Shapes);
            foreach (var body in ent.Bodies)
            {
                var shapes = new List<HplEntityFile.Shape>();
                foreach (int id in body.ShapeIds)
                {
                    var shape = freeShapes.FirstOrDefault(s => s.Id == id);
                    if (shape == null) continue;
                    freeShapes.Remove(shape);
                    shapes.Add(shape);
                }
                if (shapes.Count == 0)
                {
                    _log.Warn($"{AmnesiaPaths.ToSourceRelative(ent.Path)}: no shapes found for body '{body.Name}' (skipped)");
                    continue;
                }

                var go = new GameObject(body.Name);
                go.transform.SetParent(parent, false);
                go.transform.SetLocalPositionAndRotation(HplSpace.Position(Vector3.Scale(body.Position, scale)), HplSpace.Rotation(body.Rotation));
                var data = go.AddComponent<AmnesiaBody>();
                data.hplId = body.Id;
                data.mass = body.Mass;
                data.material = body.Material;
                data.collideCharacter = body.CollideCharacter;
                data.collideNonCharacter = body.CollideNonCharacter;
                data.hasGravity = body.HasGravity;
                data.linearDamping = body.LinearDamping;
                data.angularDamping = body.AngularDamping;
                data.maxLinearSpeed = body.MaxLinearSpeed;
                data.maxAngularSpeed = body.MaxAngularSpeed;
                data.continuousCollision = body.ContinuousCollision;
                data.buoyancyDensityMul = body.BuoyancyDensityMul;
                data.isVolatile = body.Volatile;
                data.useSurfaceEffects = body.UseSurfaceEffects;
                data.blocksSound = body.BlocksSound;
                data.pushedByCharacterGravity = body.PushedByCharacterGravity;
                data.canAttachCharacter = body.CanAttachCharacter;

                var physicsMaterial = _surfaces != null ? _surfaces.GetOrDefault(body.Material)?.physicsMaterial : null;
                foreach (var shape in shapes) BuildShape(shape, go.transform, scale, physicsMaterial);
                built.Add((body, data));
            }
            return built;
        }

        /// <summary>
        /// EntityLoader_Object.cpp LoadAndAttachChildren: each body takes the children its &lt;Children&gt; lists (first
        /// match by unique ID); everything left is attached to the first body. World poses stay.
        /// </summary>
        void AttachEntityChildren(HplEntityFile ent, List<(HplEntityFile.Body data, AmnesiaBody body)> bodies,
                                  List<(int id, Transform transform)> attachables)
        {
            if (bodies.Count == 0) return;
            var left = new List<(int id, Transform transform)>(attachables);
            foreach (var (data, body) in bodies)
            {
                foreach (int id in data.ChildIds)
                {
                    int index = left.FindIndex(a => a.id == id);
                    if (index < 0)
                    {
                        _log.Warn($"{AmnesiaPaths.ToSourceRelative(ent.Path)}: could not find child with unique ID {id} to attach to body '{data.Name}'");
                        continue;
                    }
                    left[index].transform.SetParent(body.transform, true);
                    left.RemoveAt(index);
                }
            }
            foreach (var (_, t) in left) t.SetParent(bodies[0].body.transform, true);
        }

        /// <summary>EntityLoader_Object.cpp CreateJoint + SetJointProperties, as <see cref="AmnesiaJoint"/> data.</summary>
        void BuildEntityJoints(HplEntityFile ent, Transform parent, Vector3 scale, string entityName,
                               List<(HplEntityFile.Body data, AmnesiaBody body)> bodies)
        {
            AmnesiaBody Find(int id) => bodies.FirstOrDefault(b => b.data.Id == id).body;

            foreach (var joint in ent.Joints)
            {
                var e = joint.Element;
                string jointName = entityName + "_" + e.Str("Name");
                // ToJointType: the screw name is misspelled in the original, so screws load as ball joints.
                HplJointType type;
                switch (joint.Type.ToLowerInvariant())
                {
                    case "jointhinge": type = HplJointType.Hinge; break;
                    case "jointball": type = HplJointType.Ball; break;
                    case "jointslider": type = HplJointType.Slider; break;
                    case "joinscrew": type = HplJointType.Screw; break;
                    default:
                        _log.Warn($"{AmnesiaPaths.ToSourceRelative(ent.Path)}: joint type '{joint.Type}' does not exist (ball joint)");
                        type = HplJointType.Ball;
                        break;
                }

                int parentId = e.Int("ConnectedParentBodyID");
                int childId = e.Int("ConnectedChildBodyID");
                var parentBody = parentId > 0 ? Find(parentId) : null;
                var childBody = Find(childId);
                if (childBody == null)
                {
                    _log.Warn($"{AmnesiaPaths.ToSourceRelative(ent.Path)}: could not find child body with ID {childId} for joint '{jointName}'");
                    continue;
                }

                var go = new GameObject("Joint_" + e.Str("Name"));
                go.transform.SetParent(parent, false);
                var j = go.AddComponent<AmnesiaJoint>();
                j.type = type;
                j.jointName = jointName;
                j.hplId = e.Int("ID", -1);
                j.parentBody = parentBody;
                j.childBody = childBody;
                j.pivot = HplSpace.Position(Vector3.Scale(e.Vec3("WorldPos"), scale));
                j.pinDir = HplSpace.Direction(e.Vec3("PinDir"));
                go.transform.localPosition = j.pivot;

                j.minAngle = e.Float("MinAngle") * Mathf.Deg2Rad;
                j.maxAngle = e.Float("MaxAngle") * Mathf.Deg2Rad;
                j.maxConeAngle = e.Float("MaxConeAngle") * Mathf.Deg2Rad;
                j.maxTwistAngle = e.Float("MaxTwistAngle") * Mathf.Deg2Rad;
                j.minDistance = e.Float("MinDistance");
                j.maxDistance = e.Float("MaxDistance");

                j.moveSound = e.Str("MoveSound");
                j.minMoveSpeed = e.Float("MinMoveSpeed", 0.5f);
                j.minMoveFreq = e.Float("MinMoveFreq", 0.9f);
                j.minMoveVolume = e.Float("MinMoveVolume", 0.3f);
                j.minMoveFreqSpeed = e.Float("MinMoveFreqSpeed", 0.9f);
                j.maxMoveFreq = e.Float("MaxMoveFreq", 1.1f);
                j.maxMoveVolume = e.Float("MaxMoveVolume", 1f);
                j.maxMoveFreqSpeed = e.Float("MaxMoveFreqSpeed", 1.1f);
                j.middleMoveSpeed = e.Float("MiddleMoveSpeed", 1f);
                j.middleMoveVolume = e.Float("MiddleMoveVolume", 1f);
                j.moveSpeedAngular = e.Str("MoveType", "Linear").ToLowerInvariant() == "angular";
                j.stickyMinLimit = e.Bool("StickyMinLimit");
                j.stickyMaxLimit = e.Bool("StickyMaxLimit");
                j.breakable = e.Bool("Breakable");
                j.breakForce = e.Float("BreakForce", 1000f);
                j.breakSound = e.Str("BreakSound");
                j.limitAutoSleep = e.Bool("LimitAutoSleep");
                j.limitAutoSleepDist = e.Float("LimitAutoSleepDist", 0.02f);
                j.limitAutoSleepNumSteps = e.Int("LimitAutoSleepNumSteps", 10);
                j.collideBodies = e.Bool("CollideBodies", true);
                j.maxLimitSound = e.Str("MaxLimitSound");
                j.maxLimitMaxSpeed = e.Float("MaxLimitMaxSpeed", 10f);
                j.maxLimitMinSpeed = e.Float("MaxLimit_MinSpeed", 20f);
                if (j.maxLimitMaxSpeed <= 0f) j.maxLimitMaxSpeed = 0.01f;
                j.minLimitSound = e.Str("MinLimitSound");
                j.minLimitMaxSpeed = e.Float("MinLimitMaxSpeed", 10f);
                j.minLimitMinSpeed = e.Float("MinLimitMinSpeed", 20f);
                // Kept: the original checks the min limit but resets the max one.
                if (j.minLimitMaxSpeed <= 0f) j.maxLimitMaxSpeed = 0.01f;
                Count("joints");
            }
        }

        /// <summary>The physics material of a static collider: its sub mesh material's PhysicsMaterial (WorldLoaderHplMap.cpp).</summary>
        PhysicsMaterial StaticPhysicsMaterial(Collider collider)
        {
            if (_surfaces == null) return null;
            var renderer = collider.GetComponent<Renderer>();
            var material = renderer != null ? renderer.sharedMaterial : null;
            string name = null;
            if (material != null && !_physicsMaterialNames.TryGetValue(material, out name))
            {
                string assetPath = AssetDatabase.GetAssetPath(material);
                if (assetPath.StartsWith(AmnesiaPaths.MaterialsRoot + "/", StringComparison.Ordinal))
                {
                    string source = AmnesiaPaths.SourceOf(AmnesiaPaths.MaterialsRoot, assetPath);
                    try { if (File.Exists(source)) name = AmnesiaMaterial.Load(source).PhysicsMaterial; }
                    catch (Exception ex) { _log.Warn($"{source}: {ex.Message}"); }
                }
                _physicsMaterialNames[material] = name;
            }
            return _surfaces.GetOrDefault(name)?.physicsMaterial;
        }

        /// <summary>EntityLoader_Object.cpp CreateCollideShape: size = Scale × instance scale, offset relative to the body.</summary>
        void BuildShape(HplEntityFile.Shape shape, Transform body, Vector3 scale, PhysicsMaterial physicsMaterial)
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
            var shapeCollider = go.GetComponent<Collider>();
            if (shapeCollider != null) shapeCollider.sharedMaterial = physicsMaterial;
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        // ---------- Lights, billboards, particle systems, sounds ----------

        /// <summary>Map-level or .ent world entity. Inside an entity positions scale with it (EntityLoader_Object.cpp).</summary>
        GameObject BuildWorldEntity(XmlElement e, Transform parent, string namePrefix, Vector3 positionScale)
        {
            switch (e.Name)
            {
                case "PointLight":
                case "SpotLight":
                    return BuildLight(e, parent, namePrefix, positionScale);
                case "BoxLight":
                    return BuildBoxLight(e, parent, namePrefix, positionScale);
                case "Billboard":
                    return BuildBillboard(e, parent, namePrefix, positionScale);
                case "ParticleSystem":
                {
                    var go = Child(e, parent, namePrefix, positionScale, HplSpace.Rotation(e.Vec3("Rotation")));
                    var ps = go.AddComponent<AmnesiaParticleSystem>();
                    ps.file = e.Str("File");
                    ps.color = e.Color("Color", Color.white);
                    ps.fadeAtDistance = e.Bool("FadeAtDistance");
                    ps.minFadeDistance = new Vector2(e.Float("MinFadeDistanceStart"), e.Float("MinFadeDistanceEnd"));
                    ps.maxFadeDistance = new Vector2(e.Float("MaxFadeDistanceStart"), e.Float("MaxFadeDistanceEnd"));
                    FinishChild(e, go, "particle systems");
                    return go;
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
                    FinishChild(e, go, "sounds");
                    return go;
                }
                case "FogArea":
                {
                    // cEngineFileLoading::LoadFogArea + SetupWorldEntity: the matrix R·S, the unit box; Active is not read.
                    var go = Child(e, parent, namePrefix, positionScale, HplSpace.Rotation(e.Vec3("Rotation")));
                    go.transform.localScale = Vector3.Scale(e.Vec3("Scale", Vector3.one), positionScale);
                    var fog = go.AddComponent<AmnesiaFogArea>();
                    fog.color = e.Color("Color", Color.white);
                    fog.start = e.Float("Start");
                    fog.end = e.Float("End");
                    fog.falloffExp = e.Float("FalloffExp");
                    fog.showBacksideWhenInside = e.Bool("ShownBacksideWhenInside", true);
                    fog.showBacksideWhenOutside = e.Bool("ShownBacksideWhenOutside", true);
                    fog.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/HplFogArea.shader");
                    Count("fog areas");
                    return go;
                }
                default:
                    WarnOnce($"'{e.Name}' is not supported yet (skipped)");
                    return null;
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

        GameObject BuildLight(XmlElement e, Transform parent, string namePrefix, Vector3 positionScale)
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
            data.castShadows = shadows;
            if (spot)
            {
                data.spotFov = e.Float("FOV", 1f);
                data.spotAspect = e.Float("Aspect", 1f);
                data.spotNearClip = e.Float("NearClipPlane", 0.1f);
            }
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
            return go;
        }

        /// <summary>
        /// cLightBox (cEntityLoader / MapLoader BoxLight): ambient light in a box drawn by HplBoxLightPass. HPL2 renders
        /// it with scale(Size) + position only, so the rotation is dropped. Inactive boxes stay enabled and hidden,
        /// so scripts can show them.
        /// </summary>
        GameObject BuildBoxLight(XmlElement e, Transform parent, string namePrefix, Vector3 positionScale)
        {
            var go = Child(e, parent, namePrefix, positionScale, Quaternion.identity);
            var data = go.AddComponent<AmnesiaLight>();
            data.box = true;
            data.boxSize = Vector3.Scale(e.Vec3("Size", Vector3.one), positionScale);
            data.boxBlendAdd = e.Int("BlendFunc") == 1;
            data.boxVisible = e.Bool("Active", true);
            data.diffuseColor = e.Color("DiffuseColor", Color.white);
            data.radius = e.Float("Radius", 1f);
            data.falloffMap = e.Str("FalloffMap");
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
            Count("box lights");
            return go;
        }

        GameObject BuildBillboard(XmlElement e, Transform parent, string namePrefix, Vector3 positionScale)
        {
            var go = Child(e, parent, namePrefix, positionScale, HplSpace.Rotation(e.Vec3("Rotation")));
            Vector2 size = e.Vec2("BillboardSize", Vector2.one);
            Color color = e.Color("BillboardColor", Color.white);
            var material = e.Bool("IsHalo")
                ? HaloMaterial(e.Str("MaterialFile"), LevelMaterial(e.Str("MaterialFile")), color)
                : BillboardVariant(LevelMaterial(e.Str("MaterialFile")), color);
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
            return go;
        }

        /// <summary>
        /// Halo billboards (IsHalo): RTXMnesia/HplHalo with the texture of the converted material, the blend mode and
        /// depth test of the .mat (halo_torch.mat has DepthTest="False") and the billboard color as is.
        /// </summary>
        Material HaloMaterial(string reference, Material baseMaterial, Color color)
        {
            if (baseMaterial == null) return null;
            string key = "halo|" + AssetDatabase.GetAssetPath(baseMaterial) + "|" + ColorKey(color);
            if (_variants.TryGetValue(key, out var variant)) return variant;
            string abs = _index.Find(reference);
            var src = abs != null ? AmnesiaMaterial.Load(abs) : null;
            var m = new Material(Shader.Find(AmnesiaBillboard.HaloShader)) { name = baseMaterial.name + " halo " + ColorKey(color) };
            Texture texture = baseMaterial.HasProperty("_UnlitColorMap") ? baseMaterial.GetTexture("_UnlitColorMap")
                : baseMaterial.HasProperty("_BaseColorMap") ? baseMaterial.GetTexture("_BaseColorMap") : null;
            m.SetTexture("_BaseColorMap", texture);
            m.SetVector("_HplColor", color);
            ParticleConverter.SetBlend(m, src?.BlendMode);
            bool depthTest = src == null || !src.Variables.TryGetValue("DepthTest", out string dt) || dt.ToLowerInvariant() == "true";
            m.SetFloat("_ZTest", (float)(depthTest ? CompareFunction.LessEqual : CompareFunction.Always));
            m.renderQueue = (int)RenderQueue.Transparent;
            _variants[key] = AddGenerated(m);
            return m;
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

            // HPL2 has no ambient light or sky lighting; its sky box only draws the background (AmnesiaLevel.skyBox*,
            // the camera background color at runtime).
            var environment = profile.Add<VisualEnvironment>(true);
            environment.skyType.Override(0);
            if (mapData.Bool("SkyBoxActive") && mapData.Str("SkyBoxTexture") != "")
                _log.Warn("Map sky box texture is not converted yet (only its color)");

            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(HplLighting.ExposureEV100);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.None);

            var fog = profile.Add<Fog>(true);
            HplLighting.ApplyFog(fog, mapData.Bool("FogActive"), mapData.Color("FogColor", Color.white), mapData.Float("FogEnd", 40f));
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);

            var volume = new GameObject("Level Volume").AddComponent<Volume>();
            volume.transform.SetParent(root, false);
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            if (_menuBackground) BuildMenuCamera(root); else BuildPlayer();
        }

        void BuildMenuCamera(Transform root)
        {
            var start = _playerStarts.FirstOrDefault(t => t.name == "CameraPos");
            if (start == null) throw new InvalidDataException("Main menu background has no CameraPos");
            var go = new GameObject("MenuCamera") { tag = "MainCamera" };
            go.transform.SetParent(root, false);
            // HPL cameras look along local -Z; PlayerStart's +Z is only for gameplay spawning.
            go.transform.SetPositionAndRotation(start.position, start.rotation * Quaternion.Euler(0, 180, 0));
            var config = ConfigConverter.Convert(_campaign.MenuConfig, _log, _campaign);
            var camera = go.AddComponent<Camera>(); camera.fieldOfView = config.GetFloat("Main", "BGCamera_FOV", 70);
            camera.nearClipPlane = 0.05f; camera.farClipPlane = 500;
            var data = go.AddComponent<HDAdditionalCameraData>();
            data.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color; data.backgroundColorHDR = Color.black;
            go.AddComponent<AudioListener>();
        }

        // The player at the first player start; in edit mode its camera shows the start view at eye height.
        void BuildPlayer()
        {
            var playerObject = new GameObject("Player");
            var player = playerObject.AddComponent<AmnesiaPlayer>();
            player.gameConfig = _gameConfig;

            var cameraObject = new GameObject("PlayerCamera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(playerObject.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = _gameConfig != null ? _gameConfig.GetFloat("Player_General", "FOV", 70f) : 70f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            var cameraData = cameraObject.AddComponent<HDAdditionalCameraData>();
            cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            // Without a sky HPL2 clears to black (HDRP's default background is dark blue).
            cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            cameraData.backgroundColorHDR = Color.black;
            cameraObject.AddComponent<AudioListener>();
            // Free camera for looking around (F2 in play mode), off by default.
            cameraObject.AddComponent<FlyCamera>().enabled = false;
            player.playerCamera = camera;

            var start = _playerStarts.FirstOrDefault(t => t.name == "PlayerStartArea_1") ?? _playerStarts.FirstOrDefault();
            if (start == null)
            {
                _log.Warn("Map has no PlayerStart area: player at the origin");
                return;
            }
            player.startArea = start.name;
            Vector3 forward = Vector3.ProjectOnPlane(start.forward, Vector3.up);
            playerObject.transform.SetPositionAndRotation(start.position + new Vector3(0f, 0.05f, 0f),
                forward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(forward) : Quaternion.identity);
            cameraObject.transform.localPosition = new Vector3(0f, EyeHeight(), 0f);
        }

        /// <summary>
        /// The level's runtime for scripts: <see cref="HplMap"/> (finds the C# port of the .hps by its map path),
        /// screen effects with their shader and sepia table, the HUD with the quest icon, game / menu configs,
        /// the texts of the language, the sound handler with the sound bank (all game sounds, see SoundConverter) and
        /// the particle bank (all .ps, see ParticleConverter).
        /// </summary>
        void BuildScriptRuntime(GameObject root)
        {
            var map = root.AddComponent<HplMap>();
            map.mapFile = _mapRelative;
            map.menuBackground = _menuBackground;
            if (_menuBackground) map.backgroundCamera = root.GetComponentInChildren<Camera>();
            map.gameConfig = _gameConfig;
            map.initConfig = ConfigConverter.Convert(_campaign.InitFile, _log, _campaign);
            map.preMenuConfig = ConfigConverter.Convert(_campaign.PreMenuConfig, _log, _campaign);
            map.menuConfig = ConfigConverter.Convert(_campaign.MenuConfig, _log, _campaign);
            map.keysConfig = ConfigConverter.Convert(ConfigConverter.KeysConfig, _log, _campaign);
            map.language = LanguageConverter.Convert(LanguageConverter.DefaultLanguage, _log, _campaign);
            map.player = Object.FindAnyObjectByType<AmnesiaPlayer>();
            map.surfaces = _surfaces;

            var effects = root.AddComponent<HplEffects>();
            effects.screenEffectsShader = Shader.Find(HplScreenEffectsPass.ShaderName);
            if (effects.screenEffectsShader == null) _log.Error($"Shader {HplScreenEffectsPass.ShaderName} not found");
            effects.sepiaLookup = RuntimeTexture("colorconv_sepia.tga", TextureRole.Lookup);
            map.effects = effects;

            var hud = root.AddComponent<HplHud>();
            string questIcon = map.menuConfig != null ? map.menuConfig.GetString("Messages", "QuestAddedIcon") : "";
            hud.questAddedIcon = RuntimeTexture(questIcon, TextureRole.Gui);
            // iLuxPlayerState_DefaultBase: crosshair images by eLuxFocusCrosshair.
            string[] crosshairs = { "default", "over_grab", "over_push", "over_ignite", "over_pick", "over_leveldoor", "over_ladder" };
            hud.crosshairs = crosshairs.Select(c => RuntimeTexture($"hud_crosshair_{c}.tga", TextureRole.Gui)).ToArray();
            // The HUD fonts of the language (LuxMessageHandler, LuxHintHandler, cLuxPlayer::LoadFonts).
            hud.defaultFont = FontConverter.Convert("game_default.fnt", map.language, _index, _log);
            string hintsFont = map.menuConfig != null ? map.menuConfig.GetString("Hints", "DefaultFont") : "";
            hud.hintsFont = FontConverter.Convert(string.IsNullOrEmpty(hintsFont) ? "game_hints.fnt" : hintsFont, map.language, _index, _log);
            string focusFont = _gameConfig != null ? _gameConfig.GetString("Player_General", "FocusTextFont") : "";
            hud.focusFont = string.IsNullOrEmpty(focusFont) || focusFont == "game_default.fnt" ? hud.defaultFont
                : FontConverter.Convert(focusFont, map.language, _index, _log);
            map.hud = hud;
            MenuConverter.Convert(_index, map.language, map.menuConfig, _log);
            InventoryConverter.Convert(_index, map.language, _log);
            JournalConverter.Convert(_index, map.language, _log);
            LoadScreenConverter.Convert(_index, map.language, map.menuConfig, _log);
            PostEffectsConverter.Convert(_log);
            FlashbackConverter.Convert(_log);
            HudEffectsConverter.Convert(_log);
            if (_menuBackground) PreMenuConverter.Convert(_index, _log);
            if (!_menuBackground) CommentaryConverter.Convert(_log);
            if (!_menuBackground) EnemyConverter.Convert(_index, _log);
            if (!_menuBackground) InsanityConverter.Convert(_index, _log);
            if (!_menuBackground && File.Exists(_mapAbsolute)) RopeConverter.Convert(_index, _log, new[] { _mapAbsolute });

            var sound = root.AddComponent<RTXMnesia.Sound.HplSoundHandler>();
            sound.bank = SoundConverter.Convert(_log, map.language);
            map.sound = sound;
            map.particles = ParticleConverter.Convert(_log);

            if (_menuBackground)
            {
                root.AddComponent<HplMainMenu>();
                _log.Info("Main menu background: camera, world, particles and audio; no gameplay player / script");
                return;
            }
            var script = HplScript.FindScriptType(_mapRelative);
            if (script != null) _log.Info($"Level script: {script.FullName}");
            else _log.Warn($"No level script port for {_mapRelative} yet (the level runs without its events)");
        }

        /// <summary>A game texture the runtime needs (lookup tables, HUD images), converted if missing.</summary>
        Texture2D RuntimeTexture(string reference, TextureRole role)
        {
            if (string.IsNullOrEmpty(reference)) return null;
            string file = _index.Find(reference);
            if (file == null)
            {
                _log.Warn($"Texture '{reference}' not found in source/");
                return null;
            }
            try
            {
                string assetPath = TextureConverter.Convert(file, role, null, false, out bool written);
                if (written) AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            catch (Exception ex)
            {
                _log.Warn($"Texture {AmnesiaPaths.ToSourceRelative(file)}: {ex.Message}");
                return null;
            }
        }

        // Camera above the feet when standing: body height + CameraPosAdd (CharacterBody::UpdateCamera).
        float EyeHeight() => _gameConfig == null ? 1.75f
            : _gameConfig.GetVector3("Player_Body", "Size").y + _gameConfig.GetVector3("Player_Body", "CameraPosAdd").y;
    }
}

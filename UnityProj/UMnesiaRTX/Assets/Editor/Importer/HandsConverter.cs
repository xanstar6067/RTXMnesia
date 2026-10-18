using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RTXMnesia.Player;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// The player's hands and hand objects (cLuxPlayerHands, iLuxHandObject): models/player/hands/hands.ent with its
    /// animations (.dae_anim Collada files, imported as legacy clips and renamed as the .ent's Animation names) and
    /// every models/hand_objects/&lt;name&gt;/&lt;name&gt;.ho with its model entity, into the ignored
    /// Resources/HplHands.prefab (<see cref="HplHandsData"/>). Maps need no re-import.
    /// </summary>
    public static class HandsConverter
    {
        public const string PrefabPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + HplHandsData.ResourceName + ".prefab";
        const string HandsEnt = "models/player/hands/hands.ent";

        [MenuItem("Tools/RTXMnesia/Import/Player hands and hand objects")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(new ResourceIndex(AmnesiaPaths.SourceRoot), log);
            log.Save("Player hands");
            if (log.Errors > 0) throw new InvalidOperationException("Hands import failed; see AmnesiaImport.log");
        }

        public static GameObject Convert(ResourceIndex index, ImportLog log)
        {
            string handsEnt = Path.Combine(AmnesiaPaths.SourceRoot, HandsEnt);
            var handsDoc = new XmlDocument();
            handsDoc.Load(handsEnt);
            var clips = ImportAnimations(handsEnt, handsDoc, log);

            var objects = new List<(string ho, XmlElement main, XmlElement settings, string ent)>();
            string objectsRoot = Path.Combine(AmnesiaPaths.SourceRoot, "models/hand_objects");
            foreach (string ho in Directory.EnumerateFiles(objectsRoot, "*.ho", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var doc = new XmlDocument();
                doc.Load(ho);
                var root = doc.DocumentElement;
                var main = root.Child("Main");
                var settings = root.Child("Settings");
                if (main == null || settings == null)
                {
                    log.Error($"{AmnesiaPaths.ToSourceRelative(ho)}: no Main / Settings element");
                    continue;
                }
                // iLuxHandObject::CreateEntity: "models/player/<name>/<Model>", found by name.
                string ent = index.Find(main.Str("Model"), ho);
                if (ent == null)
                {
                    log.Error($"{AmnesiaPaths.ToSourceRelative(ho)}: model '{main.Str("Model")}' not found");
                    continue;
                }
                objects.Add((ho, main, settings, ent));
            }

            var entities = new List<(string, string)> { (handsEnt, "PlayerHands") };
            foreach (var o in objects) entities.Add((o.ent, Path.GetFileNameWithoutExtension(o.ho)));

            return LevelConverter.BuildEntitiesPrefab(PrefabPath, entities, (root, buildLog) =>
            {
                var data = root.AddComponent<HplHandsData>();
                data.hands = root.transform.Find("PlayerHands");
                var animation = data.hands.GetComponentInChildren<Animation>(true);
                if (animation == null)
                {
                    var model = data.hands.childCount > 0 ? data.hands.GetChild(0).gameObject : data.hands.gameObject;
                    animation = model.AddComponent<Animation>();
                }
                foreach (var clip in animation.Cast<AnimationState>().Select(s => s.clip).ToList()) animation.RemoveClip(clip);
                animation.playAutomatically = false;
                animation.clip = null;
                animation.cullingType = AnimationCullingType.AlwaysAnimate;
                foreach (var clip in clips) animation.AddClip(clip, clip.name);
                data.handsAnimation = animation;
                // cLuxPlayerHands: hands and hand objects cast no shadows.
                foreach (var r in root.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                foreach (var (ho, main, settings, _) in objects)
                {
                    string name = Path.GetFileNameWithoutExtension(ho);
                    var o = new HplHandsData.HandObject
                    {
                        name = name,
                        type = main.Str("Type"),
                        entity = root.transform.Find(name),
                        offsetScale = settings.Vec3("OffsetScale", Vector3.one),
                        offsetRotation = settings.Vec3("OffsetRotation"),
                        offsetPosition = settings.Vec3("OffsetPosition"),
                        animIdle = settings.Str("HandsAnim_Idle"),
                        animDraw = settings.Str("HandsAnim_Draw"),
                        animHolster = settings.Str("HandsAnim_Holster"),
                        attachBone = settings.Str("AttachBoneName", "attachpoint"),
                        fadeInTime = settings.Float("FadeInTime"),
                        fadeOutTime = settings.Float("FadeOutTime"),
                        hasSwayPhysics = settings.Bool("HasSwayPhysics"),
                        maxSwayVel = settings.Float("MaxSwayVel"),
                        swayAngleLimits = settings.Vec2("SwayAngleLimits"),
                        swayDownAngleLimits = settings.Vec2("SwayDownAngleLimits"),
                        swayPinDir = settings.Vec3("SwayPinDir", Vector3.one),
                        swayGravity = settings.Float("SwayGravity"),
                        swayFriction = settings.Float("SwayFriction"),
                        swayPlayerSpeedMul = settings.Float("SwayPlayerSpeedMul"),
                        swayCameraRollMul = settings.Float("SwayCameraRollMul"),
                        skipSwaySubMesh = settings.Str("SkipSwaySubMesh"),
                    };
                    if (o.entity == null) buildLog.Error($"Hand object '{name}': entity not built");
                    else o.entity.gameObject.SetActive(false);
                    data.handObjects.Add(o);
                }
                data.hands.gameObject.SetActive(false);
                buildLog.Info($"Hands: {clips.Count} animations ({string.Join(", ", clips.Select(c => c.name))}), hand objects {string.Join(", ", data.handObjects.Select(h => h.name + "/" + h.type))}");
            });
        }

        /// <summary>
        /// Unity's Collada import renames a node whose id repeats another's (the hands model's "hand" bone became
        /// "elbow_hand"), so the clips' paths "shoulder/elbow/hand/…" matched nothing: the wrist and every finger stayed
        /// in the bind pose (flat open palm instead of the grip on the lantern). Each unresolved path segment is
        /// replaced with the model's child whose name ends with "_" + segment.
        /// </summary>
        static void RetargetCurvePaths(AnimationClip clip, string modelAsset, ImportLog log)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelAsset);
            if (model == null) return;
            var skeleton = model.transform;
            var cache = new Dictionary<string, string>();
            string Resolve(string path)
            {
                if (cache.TryGetValue(path, out var done)) return done;
                var current = skeleton;
                var parts = new List<string>();
                foreach (string segment in path.Split('/'))
                {
                    Transform next = current != null ? current.Find(segment) : null;
                    if (next == null && current != null)
                        foreach (Transform child in current)
                            if (child.name.EndsWith("_" + segment, StringComparison.Ordinal)) { next = child; break; }
                    parts.Add(next != null ? next.name : segment);
                    current = next;
                }
                return cache[path] = string.Join("/", parts);
            }

            int moved = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (string.IsNullOrEmpty(binding.path)) continue;
                string path = Resolve(binding.path);
                if (path == binding.path) continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                AnimationUtility.SetEditorCurve(clip, binding, null);
                var changed = binding;
                changed.path = path;
                AnimationUtility.SetEditorCurve(clip, changed, curve);
                moved++;
            }
            if (moved > 0) log.Info($"Hands animation {clip.name}: {moved} curve paths retargeted to the model's bone names");
        }

        /// <summary>
        /// The hands' animations: each .dae_anim is a whole Collada file of the rig; copied as .dae next to the hands
        /// model and imported with legacy clips; the clip is saved under the .ent's animation name.
        /// </summary>
        static List<AnimationClip> ImportAnimations(string handsEnt, XmlDocument handsDoc, ImportLog log)
        {
            string handsDae = Path.Combine(Path.GetDirectoryName(handsEnt), "hands.dae");
            if (!ModelConverter.IsConverted(handsDae)) ModelConverter.ConvertModels(new[] { handsDae }, "Player hands model");
            string handsAsset = ModelConverter.AssetPathFor(handsDae);
            string animFolder = Path.GetDirectoryName(handsAsset).Replace('\\', '/') + "/animations";
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(animFolder));

            var list = new List<(string asset, string name, float speed)>();
            var animations = handsDoc.DocumentElement.Child("ModelData").Child("Animations");
            foreach (XmlElement a in animations.ChildNodes.OfType<XmlElement>())
            {
                string source = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(handsEnt), a.Str("File")));
                if (!File.Exists(source))
                {
                    log.Error($"Hands animation file missing: {a.Str("File")}");
                    continue;
                }
                string asset = animFolder + "/" + Path.GetFileNameWithoutExtension(source) + ".dae";
                string target = AmnesiaPaths.ToAbsolute(asset);
                ColladaAnimFix.CopyFixed(source, target);
                list.Add((asset, a.Str("Name"), a.Float("Speed", 1f)));
            }
            AssetDatabase.Refresh();
            foreach (var (asset, _, _) in list) AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);

            var clips = new List<AnimationClip>();
            foreach (var (asset, name, speed) in list)
            {
                var source = AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>()
                                          .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
                if (source == null)
                {
                    log.Error($"{asset}: Unity imported no animation clip");
                    continue;
                }
                string clipPath = animFolder + "/" + name + ".anim";
                var clip = Object.Instantiate(source);
                clip.name = name;
                clip.legacy = true;
                if (speed != 1f) log.Warn($"Hands animation {name}: speed {speed} is not applied");
                RetargetCurvePaths(clip, handsAsset, log);
                AssetDatabase.DeleteAsset(clipPath);
                AssetDatabase.CreateAsset(clip, clipPath);
                clips.Add(clip);
                log.Info($"Hands animation {name}: {clip.length:0.000} s, {AnimationUtility.GetCurveBindings(clip).Length} curves");
            }
            AssetDatabase.SaveAssets();
            return clips;
        }
    }
}

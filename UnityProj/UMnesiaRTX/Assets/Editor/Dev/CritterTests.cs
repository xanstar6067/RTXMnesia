using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Critters (iLuxProp_CritterBase, cLuxCritter_Bug, cLuxCritter_Spider) in edit mode with the map stepping like
    /// PlayerSimulation, on 15_prison_north: rats (skinned, Idle / Walk) and cockroaches (sub meshes) — not updated
    /// beyond 15 m, wandering around the start point on the ground, Walk at the speed, random pauses, fleeing the
    /// player, sounds, saved fields, death. The spider is in no map of the game: a cockroach of the scene gets the
    /// spider.ent variables and a sphere shape before the map is built (idle → move → jump attack, not lethal, hit,
    /// death effects, sanity near it). Batch: -batchmode -nographics -quit -executeMethod
    /// RTXMnesia.EditorDev.CritterTests.RunBatch. Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class CritterTests
    {
        const string MapFile = "maps/main/ch02/15_prison_north.map";

        [MenuItem("Tools/RTXMnesia/Tests/Critters")]
        public static void RunBatch()
        {
            var log = new ImportLog();
            int checks = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++;
                log.Info("ok: " + message);
            }
            var simulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            bool failed = false;
            try
            {
                Bugs(Check, log);
                Spider(Check, log);
                log.Info($"PASS: {checks} critter checks (types, distance, wander, ground, Walk, pauses, flee, sound, save, death, spider states, attack, hit, sanity)");
            }
            catch (Exception exception)
            {
                failed = true;
                log.Error($"FAIL after {checks} checks: {exception.Message}\n{exception.StackTrace}");
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                log.Save("Critter tests");
                if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
            }
        }

        static void Bugs(Action<bool, string> check, ImportLog log)
        {
            var (map, player) = Open(null);
            var bugs = map.Entities.OfType<PropCritterBug>().ToList();
            var rats = bugs.Where(b => b.Animation != null).ToList();
            var roaches = bugs.Where(b => b.Animation == null).ToList();
            check(rats.Count == 12 && roaches.Count == 22, $"15_prison_north: 12 rats with animations and 22 cockroaches are CritterBug props ({rats.Count} / {roaches.Count})");
            check(bugs.All(b => !b.CanInteract(b.CritterBody)), "critters can not be interacted with");
            check(bugs.All(b => Quaternion.Angle(b.CritterBody.Rotation, Quaternion.identity) < 0.01f), "the rotation is removed from their bodies");

            var rat = rats.First(r => r.Active);
            var body = rat.CritterBody;
            Vector3 start = body.Position;
            // Far: the player more than 15 m away (on the same height) — no update.
            player.PlaceAt(FarFrom(map, start, 20f), 0f);
            Step(map, 1f);
            check((body.Position - start).magnitude < 0.0001f && rat.HplVelocity == Vector3.zero, $"{rat.Name}: not updated with the player 20 m away");

            // Near (6 m): wanders.
            player.PlaceAt(FarFrom(map, start, 6f), 0f);
            bool walked = false, walkSpeedOk = false, paused = false;
            int sounds = 0;
            var seen = new HashSet<Sound.HplSoundEntity>();
            float maxFromStart = 0f, maxHeight = 0f;
            for (int i = 0; i < 60 * 20; i++)
            {
                map.Step(Time.fixedDeltaTime);
                maxFromStart = Mathf.Max(maxFromStart, Vector2.Distance(new Vector2(body.Position.x, body.Position.z), new Vector2(start.x, start.z)));
                maxHeight = Mathf.Max(maxHeight, Mathf.Abs(body.Position.y - start.y));
                var walk = rat.Animation.Get("Walk");
                if (walk.IsActive && walk.Weight > 0.99f)
                {
                    walked = true;
                    if (Mathf.Abs(walk.Speed - rat.HplVelocity.magnitude) < 0.0001f) walkSpeedOk = true;
                }
                if (rat.Paused) paused = true;
                if (i % 120 == 0) log.Info($"  t{i / 60} pos {body.Position} vel {rat.HplVelocity} max {rat.MaxSpeed} paused {rat.Paused} grav {rat.HplGravityVelocity}");
                sounds += map.sound.SoundEntities.Count(s => s != null && s.Name.EndsWith("_Critter_BugSound", StringComparison.Ordinal) && !seen.Contains(s) && seen.Add(s));
            }
            check(maxFromStart > 0.3f, $"{rat.Name}: wanders with the player 6 m away (up to {maxFromStart:0.00} m from the start in 20 s)");
            check(maxFromStart < 6f, $"it swarms around its start point (never farther than {maxFromStart:0.00} m)");
            check(maxHeight < 0.05f, $"the gravity ray keeps it on the floor (height change ≤ {maxHeight:0.000} m)");
            check(walked && walkSpeedOk, "Walk plays while it moves, at the speed of the critter");
            check(paused, "HasRandomPauses: it pauses");
            // The skinned model is turned with the body: not the import pose any more.
            check(rat.AnimatedModel.transform.parent == body.Data.transform, "the rat model is posed under its body");

            // Flee: the player 1.5 m from a cockroach.
            var roach = roaches.First(r => r.Active);
            var rbody = roach.CritterBody;
            Vector3 roachStart = rbody.Position;
            player.PlaceAt(FarFrom(map, roachStart, 1.5f), 0f);
            float startDist = Flat(rbody.Position - player.Body.FeetPosition);
            Step(map, 0.5f);
            float fleeDist = Flat(rbody.Position - player.Body.FeetPosition);
            check(Mathf.Abs(roach.MaxSpeed - 1.3f) < 0.001f && fleeDist > startDist, $"{roach.Name}: flees the player (MaxSpeedAfraid 1.3, {startDist:0.00} → {fleeDist:0.00} m)");
            var meshes = rbody.Data.transform.GetComponentsInChildren<MeshFilter>().Where(f => f.transform.parent == rbody.Data.transform).ToList();
            check(meshes.Count == 2, $"the cockroach sub meshes ({meshes.Count}) are turned under the body");
            Vector3 v = HplSpace.Direction(roach.HplVelocity).normalized;
            Vector3 meshFwd = meshes[0].transform.rotation * Vector3.forward;
            check(Vector3.Dot(new Vector3(v.x, 0f, v.z).normalized, new Vector3(meshFwd.x, 0f, meshFwd.z).normalized) > 0.5f,
                  "the cockroach faces where it runs (mesh +Z ≈ the velocity)");

            // Sounds: rats and cockroaches play their sounds (Critter_BugSound).
            check(sounds > 0, $"idle / scared sounds play ({sounds} sound entities)");

            // Saved like cLuxCritter_Bug_SaveData; the pause state is not.
            var fields = Save.HplFieldBag.Capture(roach, typeof(HplProp)).Select(f => f.name).ToList();
            check(fields.Contains("PropCritterBase.Vel") && fields.Contains("PropCritterBase.GroundNormal") && fields.Contains("PropCritterBug._playSoundCount")
                  && !fields.Any(f => f.Contains("_bug")), "saved: velocity, gravity, swarm point, ground normal, sound count (not the pause)");

            // Death: health 100 → destroyed.
            roach.GiveDamage(150f, 10);
            Step(map, 2f / 60f);
            check(map.GetEntityByName(roach.Name) == null, "health ≤ 0 destroys the critter");
        }

        static void Spider(Action<bool, string> check, ImportLog log)
        {
            var (map, player) = Open(MakeSpider);
            var spider = map.GetEntityByName("spider_test") as PropCritterSpider;
            check(spider != null && spider.CausesSanityDecrease, "a CritterSpider prop (spider.ent variables on a cockroach model) causes sanity decrease");
            var body = spider.CritterBody;
            Vector3 start = body.Position;
            player.PlaceAt(FarFrom(map, start, 2.5f), 0f);
            player.health = 100f;
            Step(map, 0.1f);
            check(spider.StateName == "Move", $"Idle with no count → Move ({spider.StateName})");
            bool attacked = false, attack2 = false;
            float minHealth = player.health;
            for (int i = 0; i < 60 * 12 && !attack2; i++)
            {
                map.Step(Time.fixedDeltaTime);
                if (!attacked) player.PlaceAt(FarFrom(map, body.Position, 2.5f), 0f);   // the player stands still once it jumps
                if (spider.StateName == "Attack1") attacked = true;
                if (spider.StateName == "Attack2") attack2 = true;
                minHealth = Mathf.Min(minHealth, player.health);
            }
            check(attacked && attack2, "after its move time it jumps at the player 2.5 m away (Attack1 → Attack2)");
            Step(map, 0.6f);
            // Attack2 → Idle with the count already ≤ 0, so straight on to Move (as in HPL2).
            check(spider.StateName == "Move", $"after the attack Idle, then Move at once ({spider.StateName})");
            check(minHealth < 100f && minHealth >= 10f, $"the bite hits the player and is not lethal (health {minHealth:0})");

            // Sanity: the spider within 3 m of the head lowers it (compare with the spider inactive).
            player.PlaceAt(FarFrom(map, body.Position, 1.2f), 0f);
            player.SetSanity(80f);
            Step(map, 3f);
            float withSpider = 80f - player.sanity;
            spider.SetActive(false);
            player.SetSanity(80f);
            Step(map, 3f);
            float without = 80f - player.sanity;
            spider.SetActive(true);
            check(withSpider > without + 0.05f, $"NearCritterDecrease near the spider (sanity −{withSpider:0.00} vs −{without:0.00} without it)");

            spider.GiveDamage(5f, 10);
            check(spider.StateName == "Hit", "damage > 2 → Hit");
            spider.GiveDamage(10f, 10);
            Step(map, 2f / 60f);
            check(map.GetEntityByName("spider_test") == null && map.sound.SoundEntities.Any(s => s != null && s.Name == "spider_test_CritterDeath"),
                  "death: spider_die and the spider is destroyed");
        }

        /// <summary>
        /// GPU snapshots (no -nographics / -quit not needed): a rat and a cockroach after 4 s of wandering, seen from
        /// above-side with a temporary light → Logs/CritterCheck/*.png. -executeMethod RTXMnesia.EditorDev.CritterTests.RunSnapshotBatch
        /// </summary>
        public static void RunSnapshotBatch()
        {
            var log = new ImportLog();
            var simulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                var (map, player) = Open(null);
                var bugs = map.Entities.OfType<PropCritterBug>().Where(b => b.Active).ToList();
                foreach (var critter in new[] { bugs.First(b => b.Animation != null), bugs.First(b => b.Animation == null) })
                {
                    var body = critter.CritterBody;
                    player.PlaceAt(FarFrom(map, body.Position, 5f), 0f);
                    Step(map, 2f);
                    for (int i = 0; i < 600 && critter.HplVelocity.magnitude < 0.3f; i++) map.Step(Time.fixedDeltaTime);   // moving
                    Vector3 p = body.Position;
                    Vector3 vel = HplSpace.Direction(critter.HplVelocity);
                    Vector3 side = vel.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, vel.normalized) : Vector3.right;
                    var camGo = new GameObject("CritterCam");
                    var cam = camGo.AddComponent<Camera>();
                    cam.nearClipPlane = 0.02f;
                    cam.fieldOfView = 40f;
                    camGo.transform.position = p + side * 0.45f + Vector3.up * 0.35f;
                    camGo.transform.LookAt(p);
                    var light = new GameObject("CritterLight").AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 4f;
                    light.intensity = 40f;
                    light.transform.position = camGo.transform.position + Vector3.up * 0.3f;
                    light.gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
                    string file = Path.Combine("Logs", "CritterCheck", critter.Name + ".png");
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    ImportMenu.RenderToFile(cam, file);
                    log.Info($"{critter.Name}: velocity (Unity) {vel} → {file}");
                    UnityEngine.Object.DestroyImmediate(camGo);
                    UnityEngine.Object.DestroyImmediate(light.gameObject);
                }
            }
            catch (Exception e) { log.Error(e.ToString()); }
            finally
            {
                Physics.simulationMode = simulationMode;
                log.Save("Critter snapshots");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        /// <summary>Before the map is built: a cockroach becomes a CritterSpider with the variables of spider.ent and its sphere.</summary>
        static void MakeSpider()
        {
            var roach = UnityEngine.Object.FindObjectsByType<AmnesiaEntity>(FindObjectsInactive.Include)
                .First(e => e.entityType == "CritterBug" && e.file.EndsWith("cockroach.ent", StringComparison.OrdinalIgnoreCase) && e.gameObject.activeInHierarchy);
            var go = UnityEngine.Object.Instantiate(roach.gameObject, roach.transform.parent);
            go.name = "spider_test";
            var entity = go.GetComponent<AmnesiaEntity>();
            entity.entityType = "CritterSpider";
            entity.file = "entities/critter/spider/spider.ent";
            entity.hplId = 999999;
            entity.instanceVariables = new List<HplVariable>();
            entity.variables = new List<HplVariable>();
            var xml = new XmlDocument();
            xml.Load(Path.Combine(AmnesiaPaths.SourceRoot, "entities/critter/spider/spider.ent"));
            foreach (XmlElement v in xml.SelectNodes("//UserDefinedVariables/Var"))
                entity.variables.Add(new HplVariable(v.GetAttribute("Name"), v.GetAttribute("Value")));
            var bodyGo = go.GetComponentInChildren<AmnesiaBody>().gameObject;
            foreach (var c in bodyGo.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c.gameObject == bodyGo ? (UnityEngine.Object)c : c.gameObject);
            var shape = new GameObject("Shape_1");
            shape.transform.SetParent(bodyGo.transform, false);
            shape.AddComponent<SphereCollider>().radius = 0.19917f;
            go.transform.position += new Vector3(0f, 0.2f, 0f);
        }

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        /// <summary>A free spot for the player's feet at about this distance from the point on the same floor.</summary>
        static Vector3 FarFrom(HplMap map, Vector3 point, float distance)
        {
            for (int k = 0; k < 32; k++)
            {
                float a = k * Mathf.PI * 2f / 32f;
                Vector3 p = point + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * distance;
                if (Physics.Linecast(point + Vector3.up * 0.3f, p + Vector3.up * 0.3f)) continue;
                if (!Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var hit, 2f)) continue;
                return hit.point;
            }
            return point + new Vector3(distance, 0f, 0f);
        }

        static (HplMap map, AmnesiaPlayer player) Open(Action beforeBuild)
        {
            string scenePath = LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, MapFile));
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HplMath.Seed(1);
            beforeBuild?.Invoke();
            var player = UnityEngine.Object.FindAnyObjectByType<AmnesiaPlayer>();
            Physics.SyncTransforms();
            player.readDevices = false;
            Invoke(player, "Awake");
            Invoke(player, "Start");
            var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            map.runScript = false;
            map.Build();
            map.Begin();
            return (map, player);
        }

        static void Step(HplMap map, float seconds)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt(seconds / Time.fixedDeltaTime));
            for (int i = 0; i < steps; i++) map.Step(Time.fixedDeltaTime);
        }

        static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic)?.Invoke(target, null);
    }
}

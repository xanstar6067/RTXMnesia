using System;
using System.IO;
using System.Linq;
using RTXMnesia.Effects;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Ropes, interact connections, sticky areas and slime damage on imported maps, edit mode with the map stepping
    /// like PlayerSimulation and no level script (its functions are called directly):
    /// 06_distillery — four ropes with the hatch and pulleys, the crank turning them (InteractConnectPropWithRope),
    /// the drawn rope, a full save and load; 07_archives_cellar — the valve moving the portcullis
    /// (InteractConnectPropWithMoveObject); 13_machine_room — a cog wheel attached by its sticky area, detached by
    /// interaction, the map cache; 02_entrance_hall — slime damage areas.
    /// Batch: -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.RopeStickySlimeTests.RunBatch.
    /// Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class RopeStickySlimeTests
    {
        [MenuItem("Tools/RTXMnesia/Tests/Ropes, sticky areas, slime")]
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
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "RopeCheck"));
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            HplSaveHandler.FolderOverride = folder;
            bool failed = false;
            try
            {
                HplGame.Reset();
                HplInsanity.ResetSession();
                Ropes(Check);
                MoveObjectConnection(Check);
                Sticky(Check);
                Slime(Check);
                log.Info($"PASS: {checks} rope / connection / sticky / slime checks");
                log.Save("Rope, sticky and slime tests");
                Debug.Log($"PASS: {checks} rope / connection / sticky / slime checks");
            }
            catch (Exception ex)
            {
                log.Error($"FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
                log.Save("Rope, sticky and slime tests");
                Debug.LogError($"FAIL: {ex.Message}");
                failed = true;
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                HplSaveHandler.FolderOverride = null;
                HplSaveHandler.ClearPending();
                HplSavedMaps.Reset();
                HplInsanity.ResetSession();
                HplGame.Reset();
            }
            if (failed && Application.isBatchMode) EditorApplication.Exit(1);
        }

        // ================================================================== 06: ropes and the crank

        static void Ropes(Action<bool, string> check)
        {
            var (map, player) = Open("maps/main/ch01/06_distillery.map");
            check(HplRopeAssets.Instance != null && HplRopeAssets.Instance.materials.Count >= 2, "rope resources imported (Resources/HplRopes.asset)");
            string[] names = { "Rope01", "RopeArea_1", "RopeArea_2", "RopeArea_3" };
            check(map.Ropes.Count == 4 && names.All(n => map.GetEntityByName(n) is HplRope { Physics: not null }) &&
                  map.PhysicsWorld.Ropes.Count == 4, "06: four rope entities with physics ropes (Rope areas, AfterWorldLoad)");

            var rope01 = (HplRope)map.GetEntityByName("Rope01");
            var rope1 = (HplRope)map.GetEntityByName("RopeArea_1");
            var pulley1 = (HplProp)map.GetEntityByName("pulley_1");
            check(rope01.Physics.AttachedEndBody?.Name == "hatch_large_1_Body_door" && rope01.Physics.AttachedStartBody == null,
                  "Rope01 ends at the hatch's door body (EndBody by body name)");
            check(rope1.Physics.AttachedEndBody == pulley1.MainBody, "RopeArea_1 ends at pulley_1's main body (EndBody by prop name)");
            check(rope1.BodyCount == 0 && rope1.Type == HplEntityType.Rope && !rope1.Component.GetComponents<Collider>().Any(c => c.enabled),
                  "the rope entity has no bodies, its area box is off");

            float length = rope1.Physics.TotalLength;
            int expected = 2 + (int)(length / 0.2f + 0.999999f) - 1;
            check(rope1.Physics.Particles.Count == expected && rope1.Physics.StartParticle.InvMass == 0f && rope1.Physics.EndParticle.InvMass == 1f,
                  $"particles for the length ({rope1.Physics.Particles.Count} for {length:0.00} m, segments 0.2; start fixed)");

            check(rope1.Graphics != null && rope1.Graphics.GameObject.GetComponent<MeshRenderer>().sharedMaterial != null,
                  $"drawn rope with its material ({rope1.Graphics?.GameObject.GetComponent<MeshRenderer>().sharedMaterial?.name})");

            // The crank: OnStart's connections. Not held, interaction only = false: every step drives the motor.
            var crank = (PropWheel)map.GetEntityByName("crank_iron_1");
            CallScript(map, "InteractConnectPropWithRope", "CrankRope", "crank_iron_1", "Rope01", false, 1f, 1f, 1f, false, 0);
            for (int i = 1; i < 4; i++) CallScript(map, "InteractConnectPropWithRope", "CrankRope" + i, "crank_iron_1", "RopeArea_" + i, false, 1f, 1f, 1f, false, 0);
            check(crank.InteractConnections.Count == 4 && crank.InteractConnections.All(c => c is HplInteractConnectionRope && !c.InteractionOnly),
                  "four rope connections of the crank");

            Step(map, 4f);
            // The map's crank: limits −600°..0°, stuck at the max (angle = max − half the limit range).
            check(crank.StuckState == 1 && Mathf.Abs(crank.Angle - (crank.MaxLimit - 2.5f * Mathf.Deg2Rad)) < 1e-4f,
                  $"the crank is stuck at its max ({crank.Angle * Mathf.Rad2Deg:0.0}°)");
            float t0 = (crank.Angle - crank.MinLimit) / (crank.MaxLimit - crank.MinLimit);
            float wanted1 = rope1.Physics.MinTotalLength + (rope1.Physics.MaxTotalLength - rope1.Physics.MinTotalLength) * t0;
            check(Mathf.Abs(rope1.Physics.TotalLength - wanted1) < 0.03f, $"the crank holds the rope at its share of the range ({rope1.Physics.TotalLength:0.00} m, wanted {wanted1:0.00})");
            Vector3 end = rope1.Physics.EndParticle.Position, attach = rope1.Physics.AttachmentHplPosition(1);
            check(Vector3.Distance(end, attach) < 0.2f, $"the pulley hangs at the rope's end ({Vector3.Distance(end, attach):0.000} m)");
            int expected2 = 2 + (int)(rope1.Physics.TotalLength / 0.2f + 0.999999f) - 1;
            check(rope1.Physics.Particles.Count == expected2, $"particles follow the length ({rope1.Physics.Particles.Count})");
            rope1.Graphics.Build(Vector3.forward);
            var mesh = rope1.Graphics.GameObject.GetComponent<MeshFilter>().sharedMesh;
            check(rope1.Graphics.SegmentCount == rope1.Physics.Particles.Count - 1 && mesh.vertexCount == 4 * rope1.Graphics.SegmentCount,
                  $"one quad per segment ({rope1.Graphics.SegmentCount} segments, {mesh.vertexCount} vertices)");
            Vector3 n = mesh.normals[0];
            check(Vector3.Dot(n, Vector3.forward) < -0.5f, "the quads face the camera");

            // Turning the crank to the min shortens the ropes: the hanging rope between the (static) pulleys sags
            // less, the hatch rope lifts the hatch (a hinged Object, 0..160°).
            check(pulley1.MainBody.IsStatic, "the pulleys are static (pulley.ent mass 0)");
            float sag = rope1.Physics.Particles.Min(p => p.Position.y);
            float hatchLength = rope01.Physics.TotalLength;
            var hatch = (HplProp)map.GetEntityByName("hatch_large_1");
            float hatchAngle = Mathf.Abs(hatch.Joints[0].GetAngle());
            crank.SetStuckState(0, false);
            float angle = crank.MinLimit + (crank.MaxLimit - crank.MinLimit) * 0.4f;
            crank.SetAngle(angle, false);
            // The motor speed is the length difference × SpeedMul (1), at most 1 m/s: the last metre goes as e^-t.
            Step(map, 7f);
            float wanted2 = rope1.Physics.MinTotalLength + (rope1.Physics.MaxTotalLength - rope1.Physics.MinTotalLength) * 0.4f;
            check(Mathf.Abs(rope1.Physics.TotalLength - wanted2) < 0.03f, $"turned to 40 % of the range: the rope runs to {rope1.Physics.TotalLength:0.00} m (wanted {wanted2:0.00})");
            float sag2 = rope1.Physics.Particles.Min(p => p.Position.y);
            check(sag2 > sag + 0.2f, $"the rope sags less (lowest point {sag:0.00} → {sag2:0.00})");
            float hatchAngle2 = Mathf.Abs(hatch.Joints[0].GetAngle());
            check(rope01.Physics.TotalLength < hatchLength - 0.3f && hatchAngle2 > hatchAngle + 10f * Mathf.Deg2Rad,
                  $"the hatch rope shortens ({hatchLength:0.00} → {rope01.Physics.TotalLength:0.00} m) and lifts the hatch ({hatchAngle * Mathf.Rad2Deg:0}° → {hatchAngle2 * Mathf.Rad2Deg:0}°)");

            // A full save and load keeps the ropes and the connections.
            float savedLength = rope1.Physics.TotalLength;
            int savedCount = rope1.Physics.Particles.Count;
            Vector3 savedEnd = rope1.Physics.EndParticle.Position;
            var savedPulley = pulley1.MainBody.Position;
            string path = HplSaveHandler.AutoSave(map);
            var save = HplSaveHandler.ReadFile(path);
            var savedRope = save.map.fullEntities.First(e => e.name == "RopeArea_1").rope;
            check(savedRope.created && savedRope.particles.Count == savedCount && savedRope.endBody == pulley1.MainBody.Name,
                  "the save holds the rope's particles and end body");
            var savedCrank = save.map.fullEntities.First(e => e.name == "crank_iron_1");
            check(savedCrank.links.interactConnections.Count == 4 && savedCrank.links.interactConnections[1].target == "RopeArea_1",
                  "and the crank's interact connections");

            HplGame.Reset();
            HplSaveHandler.BeginLoad(save);
            (map, player) = Open("maps/main/ch01/06_distillery.map");
            var rope1b = (HplRope)map.GetEntityByName("RopeArea_1");
            var pulley1b = (HplProp)map.GetEntityByName("pulley_1");
            var crankb = (PropWheel)map.GetEntityByName("crank_iron_1");
            check(Mathf.Abs(rope1b.Physics.TotalLength - savedLength) < 1e-4f && rope1b.Physics.Particles.Count == savedCount &&
                  Vector3.Distance(rope1b.Physics.EndParticle.Position, savedEnd) < 1e-4f, $"loaded: rope length and particles ({rope1b.Physics.TotalLength:0.00} m)");
            check(rope1b.Physics.AttachedEndBody == pulley1b.MainBody && Vector3.Distance(pulley1b.MainBody.Position, savedPulley) < 0.01f,
                  "loaded: the pulley at its place, attached");
            check(crankb.InteractConnections.Count == 4, "loaded: the crank's connections");
            Step(map, 1f);
            check(Mathf.Abs(rope1b.Physics.TotalLength - savedLength) < 0.05f, "the loaded rope stays (the crank is where it was)");
        }

        // ================================================================== 07: valve and portcullis

        static void MoveObjectConnection(Action<bool, string> check)
        {
            var (map, _) = Open("maps/main/ch01/07_archives_cellar.map");
            var valve = (PropWheel)map.GetEntityByName("valve_iron_1");
            var gate = (PropMoveObject)map.GetEntityByName("castle_portcullis_2");
            CallScript(map, "InteractConnectPropWithMoveObject", "SecondGate", "valve_iron_1", "castle_portcullis_2", true, false, 0);
            check(valve.InteractConnections.Count == 1 && valve.InteractConnections[0].InteractionOnly, "07: valve connected to the portcullis (interaction only)");
            Step(map, 0.5f);
            check(Mathf.Abs(gate.GetMoveState()) < 0.01f, "not held: the gate stays closed");

            valve.IsInteractedWith = true;
            valve.SetAngle(valve.MinLimit + (valve.MaxLimit - valve.MinLimit) * 0.6f, false);
            Step(map, 3f);
            float state = gate.GetMoveState();
            check(state > 0.4f, $"held and turned: the gate opens with the valve (state {state:0.00})");
            valve.IsInteractedWith = false;
            Step(map, 1f);
            check(Mathf.Abs(gate.GetMoveState() - state) < 0.2f, $"let go: the gate stays ({gate.GetMoveState():0.00})");
        }

        // ================================================================== 13: sticky cog wheel

        static void Sticky(Action<bool, string> check)
        {
            HplSavedMaps.Reset();
            var (map, player) = Open("maps/main/ch01/13_machine_room.map");
            var area = (HplArea)map.GetEntityByName("cogwheel01");
            var wheel = (PropObject)map.GetEntityByName("cogwheel01_dyn");
            var body = wheel.MainBody;
            check(area.Sticky != null && area.FullGameSave, "13: sticky area cogwheel01 (saved whole)");
            float mass = body.Mass;
            var other = (PropObject)map.GetEntityByName("cogwheel02_dyn");

            // A wrong wheel in the area is not taken (its name does not contain cogwheel01_dyn).
            var areaT = area.GameObject.transform;
            other.MainBody.SetPose(areaT.position, other.MainBody.Rotation);
            Step(map, 0.1f);
            check(area.Sticky.AttachedBody == null && !other.MainBody.IsStatic, "another body in the area is not attached");
            other.MainBody.SetPose(areaT.position + Vector3.up * 3f, other.MainBody.Rotation);

            body.SetPose(areaT.position + new Vector3(0.05f, 0.02f, 0f), Quaternion.Euler(30f, 10f, 0f) * body.Rotation);
            Step(map, 1f / 60f);
            check(area.Sticky.AttachedBody == body && body.IsStatic && !body.Gravity, "the right wheel touching the area is attached (static, no gravity)");
            check(!area.Sticky.CanDetach && !map.BodyIsInDetachableStickyArea(body), "while it is posed it cannot be detached");
            check(wheel.FullGameSave, "the attached prop is saved whole");
            Step(map, 0.6f);
            Vector3 com = body.WorldCenterOfMass;
            check(Quaternion.Angle(body.Rotation, areaT.rotation) < 0.5f && Vector3.Distance(body.Position, areaT.position) < 0.2f,
                  $"in place after PoseTime 0.5 s: the area's rotation ({Quaternion.Angle(body.Rotation, areaT.rotation):0.0}°), at its position ({Vector3.Distance(body.Position, areaT.position):0.000} m)");
            check(area.Sticky.CanDetach && map.BodyIsInDetachableStickyArea(body) && wheel.CanInteract(body),
                  "in place: detachable, so the static body can be interacted with");
            Step(map, 1f);
            check(Vector3.Distance(body.WorldCenterOfMass, com) < 0.001f, "it is held there");

            // The map cache keeps it attached.
            Vector3 heldPos = body.Position;
            HplSavedMaps.SaveMap(map);
            (map, player) = Open("maps/main/ch01/13_machine_room.map");
            HplSavedMaps.LoadMap(map);
            area = (HplArea)map.GetEntityByName("cogwheel01");
            wheel = (PropObject)map.GetEntityByName("cogwheel01_dyn");
            body = wheel.MainBody;
            areaT = area.GameObject.transform;
            check(area.Sticky.AttachedBody == body && body.IsStatic && Vector3.Distance(body.Position, heldPos) < 0.01f,
                  "map cache: back attached at its place");
            Step(map, 0.5f);
            check(Vector3.Distance(body.Position, heldPos) < 0.01f, "and stays");

            // Interaction detaches it (grab).
            player.PlaceAt(areaT.position - areaT.forward * 1f + Vector3.down * 0.9f, 0f);
            check(wheel.OnInteract(body, HplSpace.Position(body.Position)), "interacting with it");
            check(area.Sticky.AttachedBody == null && !body.IsStatic, "interaction detaches it (dynamic again)");
            check(player.States.CurrentType == PlayerStateType.InteractGrab, "and the player grabs it");
            player.States.ChangeState(PlayerStateType.Normal);
            check(body.Gravity && Mathf.Approximately(body.Mass, mass), $"let go: its mass ({body.Mass}) and gravity are back");
            Step(map, 0.2f);
            check(area.Sticky.AttachedBody == null, "the last attached body is not taken again while still inside");
            // Taken away step by step (a teleport out would skip the broad phase band and keep it the last one).
            for (int i = 1; i <= 40; i++)
            {
                body.SetPose(areaT.position + Vector3.up * (0.05f * i), body.Rotation);
                body.LinearVelocity = Vector3.zero;
                Step(map, 1f / 60f);
            }
            body.SetPose(areaT.position, body.Rotation);
            Step(map, 1f / 60f);
            check(area.Sticky.AttachedBody == body, "after leaving the area it is attached again");
            CallScript(map, "DetachFromStickyArea", "cogwheel01");
            check(area.Sticky.AttachedBody == null && !body.IsStatic, "DetachFromStickyArea lets it go");
            CallScript(map, "AttachPropToStickyArea", "cogwheel01", "cogwheel01_dyn");
            check(area.Sticky.AttachedBody == body, "AttachPropToStickyArea attaches the main body");
        }

        // ================================================================== 02: slime

        static void Slime(Action<bool, string> check)
        {
            var (map, player) = Open("maps/main/ch01/02_entrance_hall.map");
            var area7 = (HplArea)map.GetEntityByName("SlimeDamageArea_7");
            check(area7.Active && area7.AreaType == "SlimeDamage", "02: active slime area SlimeDamageArea_7");
            player.health = 100f;
            var t = area7.GameObject.transform;
            player.PlaceAt(t.position - new Vector3(0f, area7.Component.size.y / 2f - 0.05f, 0f), 0f);
            // Step until the first hit, then look at its effects in that step.
            float firstHit = -1f;
            for (int i = 0; i < 6 * 60 && firstHit < 0f; i++)
            {
                map.Step(Time.fixedDeltaTime);
                if (player.health < 100f) firstHit = (i + 1) / 60f;
            }
            float hit = 100f - player.health;
            check(firstHit > 0f && firstHit <= 5.05f && hit >= 4.99f && hit <= 8.01f, $"standing in it: hit after {firstHit:0.00} s (2–5 s) by {hit:0.0} (5–8)");
            check(map.sound.SoundEntities.Any(s => s.Data != null && s.Data.name.StartsWith("slime_attack_normal_hit")), "the attack sound (game.cfg Slime) plays");
            check(map.ParticleWorld.Systems.Any(ps => ps.DataName != null && ps.DataName.Contains("ps_slime_attack_normal")), "the attack particle system");
            check(map.effects.ScreenShake.Active, "the screen shakes");
            check(player.Hurt != null && area7.Active, "without DisableAfterAttack the area stays on");
            Step(map, 5.5f);
            check(100f - player.health > hit + 4.9f, $"and hurts again ({player.health:0.0})");

            var area1 = (HplArea)map.GetEntityByName("SlimeDamageArea_1");
            area1.SetActive(true);
            area7.SetActive(false);
            player.health = 100f;
            var t1 = area1.GameObject.transform;
            player.PlaceAt(t1.position - new Vector3(0f, area1.Component.size.y / 2f - 0.05f, 0f), 0f);
            Step(map, 0.6f);
            check(player.health < 100f && !area1.Active, $"SlimeDamageArea_1 (0.1–0.4 s, DisableAfterAttack): one hit, then off (health {player.health:0.0})");
        }

        // ================================================================== helpers

        static (HplMap map, AmnesiaPlayer player) Open(string mapFile)
        {
            string scenePath = LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, mapFile));
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HplMath.Seed(1);
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

        static object CallScript(HplMap map, string function, params object[] args)
        {
            var method = map.Script.GetType().GetMethod(function, System.Reflection.BindingFlags.Instance |
                                                                   System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                                                        null, args.Select(a => a.GetType()).ToArray(), null);
            if (method == null) throw new InvalidOperationException($"no script function {function}");
            return method.Invoke(map.Script, args);
        }

        static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic)?.Invoke(target, null);
    }
}

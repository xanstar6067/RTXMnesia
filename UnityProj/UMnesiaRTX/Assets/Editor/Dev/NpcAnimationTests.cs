using System;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// NPCs and prop animations (cLuxProp_NPC, iLuxProp::PlayAnimation, cMeshEntity animations) in edit mode with the
    /// map stepping like PlayerSimulation: Agrippa on 21_torture_nave (asleep, waking with effects, the idle after it,
    /// the head following the player in the follow area and not after SetNPCFollowPlayer false, saved states), the ritual
    /// prisoner on 24_torture_choir_west (the first animation looped from a random time, a non-looping one with its
    /// script callback) and Alexander on 29_orb_chamber. Batch: -batchmode -nographics -quit -executeMethod
    /// RTXMnesia.EditorDev.NpcAnimationTests.RunBatch. Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class NpcAnimationTests
    {
        [MenuItem("Tools/RTXMnesia/Tests/NPCs and prop animations")]
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
                Agrippa(Check);
                Prisoner(Check);
                Alexander(Check);
                Attached(Check);
                log.Info($"PASS: {checks} NPC / prop animation / attached prop checks (data, first animation, wake, idle, head, follow, save, callbacks, attach, follow parent, remove, destroy)");
            }
            catch (Exception exception)
            {
                failed = true;
                log.Error($"FAIL after {checks} checks: {exception.Message}\n{exception.StackTrace}");
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                log.Save("NPC and prop animation tests");
                if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
            }
        }

        static void Agrippa(Action<bool, string> check)
        {
            var (map, player) = Open("maps/main/ch02/21_torture_nave.map");
            var npc = map.GetEntityByName("agrippa_2") as PropNPC;
            check(npc != null && npc.Animation != null && npc.Animation.States.Count == 4, "21: agrippa_2 is an NPC with its 4 animations");
            Step(map, 0.3f);
            var sleepIdle = npc.Animation.Get("SleepIdle");
            check(!npc.Awake && sleepIdle.IsActive && sleepIdle.Loop && sleepIdle.Weight >= 1f && !npc.Animation.Get("Idle").IsActive,
                  "Awake=false: SleepIdle looped (no effects), Idle faded out");
            var neck = FindBone(npc.AnimatedModel.transform, "CenterNeck");
            check(neck != null, "head bone CenterNeck found");
            // SleepIdle holds its opening pose for over 3 s; sample its moving middle, not that hold.
            sleepIdle.SetTimePosition(sleepIdle.Length * 0.4f);
            npc.Animation.Update(0f, null);
            var bones = npc.AnimatedModel.GetComponentInChildren<SkinnedMeshRenderer>().bones.Where(b => b != null).ToArray();
            var before = bones.Select(b => b.localRotation).ToArray();
            float poseTime = sleepIdle.TimePosition;
            Step(map, 0.7f);
            float moved = bones.Select((b, k) => Quaternion.Angle(before[k], b.localRotation)).Max();
            check(moved > 0.01f, $"the skeleton is posed by SleepIdle (largest bone turn {moved:0.00}° in 0.7 s; active {npc.Active}, time {poseTime:0.00} → {sleepIdle.TimePosition:0.00}, model {npc.AnimatedModel.name})");

            CallScript(map, "SetNPCAwake", "agrippa_2", true, true);
            var awake = npc.Animation.Get("Awake");
            check(npc.Awake && awake.IsActive && !awake.Loop, "SetNPCAwake(true, true): Awake plays once");
            Step(map, awake.Length + 0.5f);
            var idle = npc.Animation.Get("Idle");
            check(idle.IsActive && idle.Loop && idle.Weight >= 1f && !awake.IsActive, $"after Awake ({awake.Length:0.0} s) the Idle loop");

            // The player in AreaAgrippaStartTalk, to the side of Agrippa: the head turns (twice per second check).
            var area = map.GetEntityByName("AreaAgrippaStartTalk");
            check(area != null, "follow area AreaAgrippaStartTalk");
            var mesh = npc.AnimatedModel.transform;
            var box = area.GameObject.GetComponent<Collider>().bounds;
            player.PlaceAt(new Vector3(box.center.x, box.min.y + 0.05f, box.center.z), 0f);
            Step(map, 2f);
            check(npc.PlayerIsInArea && Mathf.Abs(npc.HeadAngle) > 0.05f,
                  $"the player in the area (box {box.center} {box.size}, feet {player.Body.FeetPosition}, in {npc.PlayerIsInArea}): head angle {npc.HeadAngle * Mathf.Rad2Deg:0.0}°");
            Quaternion withHead = neck.localRotation;
            npc.Animation.Update(0f, null);
            float rotated = Quaternion.Angle(withHead, neck.localRotation);
            check(Mathf.Abs(rotated - Mathf.Abs(npc.HeadAngle * 0.5f) * Mathf.Rad2Deg) < 1f,
                  $"CenterNeck turned by HeadAngle × MoveHeadBoneMuls (-0.5): {rotated:0.0}°");

            CallScript(map, "SetNPCFollowPlayer", "agrippa_2", false);
            Step(map, 6f);
            check(!npc.FollowPlayer && Mathf.Abs(npc.HeadAngle) < 0.02f, $"SetNPCFollowPlayer false: the head turns back ({npc.HeadAngle * Mathf.Rad2Deg:0.0}°)");

            // cEngineMeshEntity_SaveData: the states come back.
            var saved = new System.Collections.Generic.List<Save.SavedAnimationState>();
            npc.CaptureAnimations(saved);
            float idleTime = idle.TimePosition;
            npc.PlayAnimation("Sleep", 0f, false, "");
            Step(map, 0.2f);
            npc.RestoreAnimations(saved);
            check(idle.IsActive && Mathf.Abs(idle.TimePosition - idleTime) < 0.001f && !npc.Animation.Get("Sleep").IsActive,
                  "the saved animation states are restored");
        }

        static void Prisoner(Action<bool, string> check)
        {
            var (map, _) = Open("maps/main/ch02/24_torture_choir_west.map");
            map.runScript = true;
            var prisoner = map.GetEntityByName("ritual_prisoner") as HplProp;
            check(prisoner != null && prisoner.Animation != null, "24: ritual_prisoner has mesh animations");
            var first = prisoner.Animation.States[0];
            check(first.IsActive && first.Loop && first.TimePosition > 0f, $"its first animation ({first.Name}) loops from a random time ({first.TimePosition:0.00})");
            // The map keeps it inactive until its event (an inactive entity is not updated, as in HPL2).
            check(!prisoner.Active, "the prisoner starts inactive");
            CallScript(map, "SetEntityActive", "ritual_prisoner", true);
            CallScript(map, "PlayPropAnimation", "ritual_prisoner", "squirm", 0.2f, false, "PrisonerSquirmOver");
            var squirm = prisoner.Animation.Get("squirm");
            check(squirm.IsActive && !squirm.Loop, "PlayPropAnimation squirm (not looped)");
            Step(map, squirm.Length + 0.3f);
            check(prisoner.Animation.Get("Idle").IsActive && prisoner.Animation.Get("Idle").Loop && map.GetTimerTimeLeft("TimerPrisonerSquirm") > 0f,
                  $"when it is over PrisonerSquirmOver runs (Idle again, TimerPrisonerSquirm) — active {prisoner.Active}, squirm {squirm.TimePosition:0.00}/{squirm.Length:0.00} over {squirm.IsOver}, timer {map.GetTimerTimeLeft("TimerPrisonerSquirm"):0.00}");
            CallScript(map, "PlayPropAnimation", "ritual_prisoner", "missing_anim", 0.2f, false, "");
            check(prisoner.Animation.Get("Idle").IsActive, "an unknown animation is an engine error and changes nothing");
        }

        static void Alexander(Action<bool, string> check)
        {
            var (map, _) = Open("maps/main/ch03/29_orb_chamber.map");
            var alexander = map.GetEntityByName("alexander_1") as PropNPC;
            check(alexander != null && alexander.Animation != null && alexander.Awake, "29: alexander_1 is an awake NPC");
            CallScript(map, "PlayPropAnimation", "alexander_1", "Release", 0.3f, false, "");
            var release = alexander.Animation.Get("Release");
            Step(map, 0.4f);
            check(release.IsActive && release.Weight >= 1f && !alexander.Animation.Get("Idle").IsActive, "Release fades in, Idle out");
            Step(map, release.Length);
            check(release.IsOver && release.IsActive, "a finished non-looping animation holds its last frame");
            CallScript(map, "SetNPCFollowPlayer", "alexander_1", false);
            check(!alexander.FollowPlayer, "SetNPCFollowPlayer(alexander_1, false)");
        }

        /// <summary>iLuxProp::AddAndAttachProp on 21_torture_nave: the meat on the rope's weight (AddAttachedPropToProp).</summary>
        static void Attached(Action<bool, string> check)
        {
            var (map, _) = Open("maps/main/ch02/21_torture_nave.map");
            var parent = map.GetEntityByName("invisible_box_mass_2_1") as HplProp;
            check(parent != null && parent.MainBody != null, "21: invisible_box_mass_2_1 (the rope's weight)");
            CallScript(map, "AddAttachedPropToProp", "invisible_box_mass_2_1", "fresh_meat_remains_onrope_1", "fresh_meat_remains_onrope.ent", 0f, 0f, 0f, 0f, 0f, 0f);
            var meat = map.GetEntityByName("fresh_meat_remains_onrope_1") as HplProp;
            check(meat != null && meat.AttachmentParent == parent && parent.HasAttachedProp && !meat.IsSaved,
                  "the meat is created, attached and not saved by itself");
            var body = meat.PhysicsBodies[0];
            check(meat.PhysicsBodies.All(b => b.IsStatic && !b.Collide && !b.CollideCharacter && !b.Active), "its bodies: static, no collision, inactive");
            Vector3 offset = body.Position - parent.MainBody.Position;
            check(offset.magnitude < 3f, $"it starts at the weight ({offset.magnitude:0.00} m from its main body)");

            parent.MainBody.SetPose(parent.MainBody.Position + new Vector3(0f, 0f, 2f), parent.MainBody.Rotation);
            parent.MainBody.Rigidbody.WakeUp();
            Step(map, 1f / 60f);
            Vector3 moved = body.Position - parent.MainBody.Position;
            check((moved - offset).magnitude < 0.2f, $"it follows the weight moved 2 m (offset kept within {(moved - offset).magnitude:0.000} m)");

            var saved = new System.Collections.Generic.List<Save.SavedAttachedProp>();
            parent.CaptureAttachedProps(saved);
            check(saved.Count == 1 && saved[0].file == "fresh_meat_remains_onrope.ent", "the weight saves its attached prop");

            CallScript(map, "RemoveAttachedPropFromProp", "invisible_box_mass_2_1", "fresh_meat_remains_onrope_1");
            Step(map, 1f / 60f);
            check(map.GetEntityByName("fresh_meat_remains_onrope_1") == null && !parent.HasAttachedProp, "RemoveAttachedPropFromProp destroys it");

            // The deprecated function puts fRotZ in the z position.
            CallScript(map, "AddAttachedPropToProp", "invisible_box_mass_2_1", "fresh_meat_onrope_1", "fresh_meat_onrope.ent", 0f, 0f, 0f, 0f, 0f, 90f);
            var meat2 = map.GetEntityByName("fresh_meat_onrope_1") as HplProp;
            Vector3 local = HplSpace.Position(Quaternion.Inverse(parent.MainBody.Rotation) * (meat2.PhysicsBodies[0].Position - parent.MainBody.Position));
            check(meat2 != null && local.z > 80f, $"AddAttachedPropToProp: fRotZ as the z position (local z {local.z:0.0})");
            map.DestroyEntity(parent);
            Step(map, 2f / 60f);
            check(map.GetEntityByName("fresh_meat_onrope_1") == null, "destroying the weight destroys its attached props");
        }

        static Transform FindBone(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindBone(child, name);
                if (found != null) return found;
            }
            return null;
        }

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
            var method = typeof(HplScript).GetMethod(function, System.Reflection.BindingFlags.Instance |
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

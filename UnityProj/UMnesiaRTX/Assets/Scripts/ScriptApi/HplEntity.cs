using System;
using System.Collections.Generic;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    public enum HplEntityType { Prop, Area, Enemy, Rope, CommentaryIcon }

    /// <summary>Something with physics bodies that collide callbacks can test (iLuxCollideCallbackContainer).</summary>
    public interface IHplBodies
    {
        int BodyCount { get; }

        /// <summary>The colliders of body <paramref name="index"/> (HPL2 GetBody: areas and characters ignore the index).</summary>
        IReadOnlyList<Collider> GetBodyColliders(int index);
    }

    /// <summary>A physics body as scripts see it: HPL2 name "&lt;entity&gt;_&lt;body&gt;" and its colliders.</summary>
    public sealed class HplBody
    {
        public readonly string Name;
        public readonly HplEntity Entity;
        public readonly Transform Transform;
        /// <summary>Entity body data (null for areas).</summary>
        public readonly AmnesiaBody Data;
        public readonly Collider[] Colliders;

        public HplBody(string name, HplEntity entity, Transform transform, AmnesiaBody data, Collider[] colliders)
        {
            Name = name;
            Entity = entity;
            Transform = transform;
            Data = data;
            Colliders = colliders;
        }
    }

    /// <summary>Shape tests between bodies (iPhysicsWorld::CheckShapeCollision).</summary>
    public static class HplCollision
    {
        /// <summary>
        /// Exact test with the current transforms. No Collider.bounds pre-test like HPL2's bounding volumes: bounds
        /// only follow transforms after a physics sync, and bodies moved by code (the character) lag behind.
        /// </summary>
        public static bool BodiesCollide(IReadOnlyList<Collider> a, IReadOnlyList<Collider> b)
        {
            if (a == null || b == null) return false;
            foreach (var ca in a)
            {
                if (!Usable(ca)) continue;
                foreach (var cb in b)
                {
                    if (!Usable(cb) || ca == cb) continue;
                    Transform ta = ca.transform, tb = cb.transform;
                    if (Physics.ComputePenetration(ca, ta.position, ta.rotation, cb, tb.position, tb.rotation, out _, out _))
                        return true;
                }
            }
            return false;
        }

        public static bool TryGetBounds(IReadOnlyList<Collider> colliders, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            if (colliders == null) return false;
            foreach (var c in colliders)
            {
                if (!Usable(c)) continue;
                if (any) bounds.Encapsulate(c.bounds);
                else bounds = c.bounds;
                any = true;
            }
            return any;
        }

        static bool Usable(Collider c) => c != null && c.enabled && c.gameObject.activeInHierarchy;

        /// <summary>
        /// iLuxCollideCallbackContainer::CheckEntityCollision. HPL2 tests body i of the owner against body i of the
        /// other entity (its inner loop only repeats that test), so it is done the same way: areas and characters
        /// return their single body for any index; a prop without that index is skipped (C++ reads past its array).
        /// </summary>
        public static bool CheckEntityCollision(IHplBodies owner, IHplBodies other)
        {
            for (int i = 0; i < owner.BodyCount; i++)
            {
                if (other.BodyCount == 0) break;
                if (BodiesCollide(owner.GetBodyColliders(i), other.GetBodyColliders(i))) return true;
            }
            return false;
        }

        /// <summary>
        /// cLuxMapHelper::CheckLineOfSight: nothing solid between the points. Characters (their layer) and areas
        /// (triggers) do not block. Entity bodies block whatever their sub meshes are (HPL2 skips translucent ones).
        /// </summary>
        public static bool CheckLineOfSight(Vector3 start, Vector3 end) =>
            !Physics.Linecast(start, end, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// The collide callbacks of an entity or the player (iLuxCollideCallbackContainer, LuxTypes.cpp): the script
    /// function runs with (owner, entity, 1) when they start touching and (owner, entity, -1) when they stop,
    /// limited to one state if states is ±1, and is removed after running when asked to.
    /// </summary>
    public sealed class HplCollideCallbacks
    {
        sealed class Callback
        {
            public HplEntity entity;
            public string function;
            public bool deleteWhenColliding;
            public int states;
            public bool colliding;
        }

        readonly List<Callback> _callbacks = new();
        readonly List<Callback> _delete = new();
        bool _updating;

        public bool Any => _callbacks.Count > 0;

        public void Add(HplEntity entity, string function, bool removeAtCollide, int states)
        {
            if (_callbacks.Exists(c => c.entity == entity))
            {
                HplLog.Warn($"A callback with entity '{entity.Name}' already exists!");
                return;
            }
            _callbacks.Add(new Callback { entity = entity, function = function, deleteWhenColliding = removeAtCollide, states = states });
        }

        public void Remove(string entityName)
        {
            var callback = _callbacks.Find(c => c.entity.Name == entityName);
            if (callback == null) return;
            if (_updating) _delete.Add(callback);
            else _callbacks.Remove(callback);
        }

        public void RemoveInstantly(HplEntity entity) => _callbacks.RemoveAll(c => c.entity == entity);

        public void Clear()
        {
            _callbacks.Clear();
            _delete.Clear();
        }

        /// <summary>CheckCollisionCallback. Callbacks added by a callback are tested in the same pass (std::list).</summary>
        public void Check(string ownerName, IHplBodies owner, HplMap map)
        {
            _updating = true;
            for (int i = 0; i < _callbacks.Count; i++)
            {
                var callback = _callbacks[i];
                var entity = callback.entity;
                if (entity == null || !entity.Active) continue;

                bool collide = HplCollision.CheckEntityCollision(owner, entity);
                if (collide == callback.colliding) continue;

                int state = collide ? 1 : -1;
                callback.colliding = collide;
                if (state != callback.states && callback.states != 0) continue;

                map.RunScript(callback.function, ownerName, entity.Name, state);
                if (callback.deleteWhenColliding && !_delete.Contains(callback)) _delete.Add(callback);
            }
            _updating = false;

            foreach (var callback in _delete) _callbacks.Remove(callback);
            _delete.Clear();
        }
    }

    /// <summary>
    /// A map entity at runtime (iLuxEntity, LuxEntity.cpp): what scripts address by name. The scene object keeps
    /// the imported data (<see cref="AmnesiaEntity"/>, <see cref="AmnesiaArea"/>); this keeps the script state.
    /// </summary>
    public abstract class HplEntity : IHplBodies
    {
        public readonly string Name;
        public readonly int Id;
        public readonly GameObject GameObject;
        public HplMap Map { get; internal set; }
        public abstract HplEntityType Type { get; }
        public bool Active { get; private set; }

        public readonly List<HplBody> Bodies = new();
        public readonly HplCollideCallbacks CollideCallbacks = new();

        public string InteractCallback = "";
        public bool InteractCallbackRemove = true;
        public string LookAtCallback = "";
        public bool LookAtCallbackRemove = true;
        public bool IsLookedAt { get; private set; }
        public string CallbackFunc = "";
        public string ConnectionStateChangeCallback = "";
        public bool InteractionDisabled;
        public float MaxFocusDistance = 2f;
        public string CustomFocusCrossHair = "Default";

        public readonly List<(string name, HplEntity entity, bool invertStateSent, int statesUsed, string callback)> Connections = new();

        float _lookAtCount;

        protected HplEntity(GameObject gameObject, int id)
        {
            Name = gameObject.name;
            Id = id;
            GameObject = gameObject;
            Active = gameObject.activeSelf;
        }

        public virtual int BodyCount => Bodies.Count;

        public virtual IReadOnlyList<Collider> GetBodyColliders(int index) =>
            index >= 0 && index < Bodies.Count ? Bodies[index].Colliders : null;

        public void SetActive(bool active)
        {
            if (Active == active) return;
            Active = active;
            OnSetActive(active);
        }

        /// <summary>Bodies, mesh, lights, billboards, particles and sounds of the entity follow its active state.</summary>
        protected virtual void OnSetActive(bool active) => GameObject.SetActive(active);

        /// <summary>iLuxEntity::UpdateLogic (only for active entities).</summary>
        public void UpdateLogic(float timeStep)
        {
            CollideCallbacks.Check(Name, this, Map);
            UpdatePlayerLookAt(timeStep);
            OnUpdate(timeStep);
        }

        protected virtual void OnUpdate(float timeStep) { }

        public void SetPlayerInteractCallback(string callback, bool removeWhenInteracted)
        {
            InteractCallback = callback ?? "";
            InteractCallbackRemove = removeWhenInteracted;
        }

        public void SetPlayerLookAtCallback(string callback, bool removeWhenLookedAt)
        {
            LookAtCallback = callback ?? "";
            LookAtCallbackRemove = removeWhenLookedAt;
        }

        /// <summary>iLuxEntity::RunInteractCallbackFunc (called by interaction, step 4.3).</summary>
        public void RunInteractCallback()
        {
            if (InteractCallback == "") return;
            string callback = InteractCallback;
            if (InteractCallbackRemove) InteractCallback = "";
            Map.RunScript(callback, Name);
        }

        /// <summary>iLuxEntity::RunCallbackFunc: entity events like "OnPickup", "Break", "OnIgnite".</summary>
        public void RunCallbackFunc(string type)
        {
            if (CallbackFunc == "") return;
            Map.RunScript(CallbackFunc, Name, type);
        }

        /// <summary>
        /// iLuxEntity::UpdatePlayerLookAt, every 0.3 s: a body inside the view, under the screen center within 50 m
        /// and visible (one of five points on its bounding sphere toward the camera has a free line of sight).
        /// </summary>
        void UpdatePlayerLookAt(float timeStep)
        {
            if (LookAtCallback == "") return;
            if (_lookAtCount > 0f)
            {
                _lookAtCount -= timeStep;
                return;
            }
            _lookAtCount = 0.3f;

            var camera = Map.PlayerCamera;
            if (camera == null) return;
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            Transform cam = camera.transform;
            bool lookingAt = false;
            for (int i = 0; i < BodyCount && !lookingAt; i++)
            {
                if (!HplCollision.TryGetBounds(GetBodyColliders(i), out Bounds bv)) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, bv)) continue;

                Vector3 start = cam.position, end = bv.center;
                Vector3 dir = end - start;
                float sqrDist = dir.sqrMagnitude;
                if (sqrDist > 50f * 50f) continue;

                if (!bv.IntersectRay(new Ray(start, cam.forward), out float hit) || hit > 50f) continue;

                float radius = bv.extents.magnitude;
                if (sqrDist < radius * radius + 0.05f)
                {
                    lookingAt = true;
                    break;
                }

                dir.Normalize();
                float half = radius * 0.5f;
                Vector3 p = end - dir * radius;
                Vector3[] tests = { p, p + cam.up * half, p - cam.up * half, p + cam.right * half, p - cam.right * half };
                foreach (var test in tests)
                {
                    if (!HplCollision.CheckLineOfSight(start, test)) continue;
                    lookingAt = true;
                    break;
                }
            }

            if (lookingAt && !IsLookedAt)
            {
                string callback = LookAtCallback;
                if (LookAtCallbackRemove) LookAtCallback = "";
                Map.RunScript(callback, Name, 1);
            }
            else if (!lookingAt && IsLookedAt)
            {
                Map.RunScript(LookAtCallback, Name, -1);
            }
            IsLookedAt = lookingAt;
        }
    }

    /// <summary>A script-visible area (iLuxArea): one body, the trigger box of the area's size.</summary>
    public sealed class HplArea : HplEntity
    {
        public readonly AmnesiaArea Component;
        public string AreaType => Component.areaType;
        public override HplEntityType Type => AreaType == "Rope" ? HplEntityType.Rope : HplEntityType.Area;

        public HplArea(AmnesiaArea area) : base(area.gameObject, area.hplId)
        {
            Component = area;
            var colliders = area.GetComponents<Collider>();
            Bodies.Add(new HplBody(Name, this, area.transform, null, colliders));
        }

        public override int BodyCount => 1;
        public override IReadOnlyList<Collider> GetBodyColliders(int index) => Bodies[0].Colliders;
    }

    /// <summary>A prop, enemy or commentary icon from an .ent file (iLuxProp / iLuxEnemy).</summary>
    public sealed class HplProp : HplEntity
    {
        public readonly AmnesiaEntity Component;
        readonly HplEntityType _type;
        public override HplEntityType Type => _type;
        /// <summary>The .ent EntityType: Object, SwingDoor, Lamp, Enemy_Grunt, ...</summary>
        public string PropType => Component.entityType;
        public float Health = 100f;

        // Script-visible state of doors, lamps and levers until their behavior exists (step 4.3).
        public bool DoorLocked, DoorClosed = true, DoorDisableAutoClose, LampLit;
        public int LeverState;

        public HplProp(AmnesiaEntity entity, HplEntityType type) : base(entity.gameObject, entity.hplId)
        {
            Component = entity;
            _type = type;
            foreach (var body in entity.GetComponentsInChildren<AmnesiaBody>(true))
                Bodies.Add(new HplBody(Name + "_" + body.name, this, body.transform, body, body.GetComponentsInChildren<Collider>(true)));
            string health = entity.GetVariable("Health");
            if (!string.IsNullOrEmpty(health)) Health = Hpl.HplMath.Atof(health);
            DoorLocked = entity.GetVariable("Locked") == "true";
            string openAmount = entity.GetVariable("OpenAmount");
            DoorClosed = string.IsNullOrEmpty(openAmount) || Hpl.HplMath.Atof(openAmount) <= 0f;
            LampLit = entity.GetVariable("Lit") == "true";
        }

        /// <summary>
        /// Lux prop / enemy loaders by .ent type (LuxBase.cpp). Others (StaticProp, plain models) are not script
        /// entities in HPL2.
        /// </summary>
        public static bool TryGetType(string entityType, out HplEntityType type)
        {
            type = HplEntityType.Prop;
            switch (entityType)
            {
                case "Object": case "SwingDoor": case "Lever": case "Wheel": case "Lamp": case "Photocell":
                case "MoveObject": case "Item": case "Chest": case "LevelDoor": case "Button": case "OilBarrel":
                case "EmotionStone": case "NPC": case "MultiSlider": case "CritterBug": case "CritterSpider":
                    return true;
                case "Enemy_Grunt": case "Enemy_WaterLurker": case "Enemy_ManPig":
                    type = HplEntityType.Enemy;
                    return true;
                case "CommentaryIcon":
                    type = HplEntityType.CommentaryIcon;
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>Script messages, each text logged once (HPL2 logs every call; thunder lights would flood the console).</summary>
    public static class HplLog
    {
        static readonly HashSet<string> Seen = new();

        public static void Warn(string message)
        {
            if (Seen.Add("W" + message)) Debug.LogWarning("[Script] " + message);
        }

        /// <summary>
        /// An engine Error() from the original: HPL2 writes "ERROR: ..." to hpl.log and the game goes on (e.g. the
        /// level script asks for a light that is not in the map). Logged as a warning: a Unity error would pause
        /// Play mode when the Console has Error Pause on.
        /// </summary>
        public static void Error(string message)
        {
            if (Seen.Add("E" + message)) Debug.LogWarning("[Script] ERROR: " + message);
        }

        /// <summary>A mistake in our port (not in the original): a real Unity error.</summary>
        public static void PortError(string message)
        {
            if (Seen.Add("P" + message)) Debug.LogError("[Script] " + message);
        }

        public static void Info(string message) => Debug.Log("[Script] " + message);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Seen.Clear();
    }
}

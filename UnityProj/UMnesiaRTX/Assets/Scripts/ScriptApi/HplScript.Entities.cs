using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    // Entities, lights, props, collide callbacks, physics and enemies (LuxScriptHandler.cpp).
    // Props are RTXMnesia.Props types over the rigid bodies of RTXMnesia.Dynamics (steps 4.2, 4.3). Not yet: attached
    // props, sticky areas, multi sliders, buttons, interact connections (ropes / move objects), NPCs, the water lurker and
    // man pig (step 6).
    public abstract partial class HplScript
    {
        const string NeedsConnections = "interact connections";
        const string NeedsEnemies = "NPCs and the man pig (step 6)";

        // ------------------------------------------------------------------ lights

        protected void SetLightVisible(string asLightName, bool abVisible)
        {
            var light = Map.GetLight(asLightName);
            if (light == null)
            {
                HplLog.Error($"Could not find light '{asLightName}'");
                return;
            }
            light.SetVisible(abVisible);
        }

        /// <summary>Negative values keep the current color channel / radius; flicker stops; the light becomes visible.</summary>
        protected void FadeLightTo(string asLightName, float afR, float afG, float afB, float afA, float afRadius, float afTime)
        {
            var light = Map.GetLight(asLightName);
            if (light == null)
            {
                HplLog.Error($"Could not find light '{asLightName}'");
                return;
            }
            light.SetFlickerActive(false);
            Color current = light.CurrentColor;
            var color = new Color(afR >= 0f ? afR : current.r, afG >= 0f ? afG : current.g, afB >= 0f ? afB : current.b, afA >= 0f ? afA : current.a);
            light.SetVisible(true);
            light.FadeTo(color, afRadius >= 0f ? afRadius : light.CurrentRadius, afTime);
        }

        protected void SetLightFlickerActive(string asLightName, bool abActive)
        {
            var light = Map.GetLight(asLightName);
            if (light == null)
            {
                HplLog.Error($"Could not find light '{asLightName}'");
                return;
            }
            light.SetFlickerActive(abActive);
        }

        // ------------------------------------------------------------------ entities

        protected void SetEntityActive(string asName, bool abActive)
        {
            foreach (var e in GetEntities(asName)) e.SetActive(abActive);
        }

        protected void SetEntityVisible(string asName, bool abVisible)
        {
            foreach (var e in GetEntities(asName))
            {
                if (e is not HplProp) continue;
                foreach (var r in e.GameObject.GetComponentsInChildren<Renderer>(true)) r.enabled = abVisible;
            }
        }

        protected bool GetEntityExists(string asName) => Map.GetEntityByName(asName) != null;

        /// <summary>Moves the first body (enemies: their feet).</summary>
        protected void SetEntityPos(string asName, float afX, float afY, float afZ)
        {
            Vector3 position = HplSpace.Position(new Vector3(afX, afY, afZ));
            foreach (var e in GetEntities(asName))
            {
                if (e is Enemies.HplEnemy enemy)
                {
                    enemy.Body.SetFeetPosition(position);
                    continue;
                }
                if (e.Bodies.Count == 0) continue;
                var body = e.Bodies[0];
                if (body.Physics != null) body.Physics.SetPose(position, body.Physics.Rotation);
                else body.Transform.position = position;
            }
        }

        protected float GetEntityPosX(string asName) => HplSpace.Position(EntityPosition(asName)).x;
        protected float GetEntityPosY(string asName) => EntityPosition(asName).y;
        protected float GetEntityPosZ(string asName) => EntityPosition(asName).z;

        Vector3 EntityPosition(string name)
        {
            var e = GetEntity(name);
            if (e == null) return Vector3.zero;
            if (e.Bodies.Count == 0)
            {
                HplLog.Error($"Could not get position of entity '{name}' because it has no physics body!");
                return Vector3.zero;
            }
            var body = e.Bodies[0];
            return body.Physics != null ? body.Physics.Position : body.Transform.position;
        }

        protected void SetEntityCustomFocusCrossHair(string asName, string asCrossHair)
        {
            var crosshair = HplEntity.ParseCrosshair(asCrossHair);
            foreach (var e in GetEntities(asName)) e.CustomFocusCrossHair = crosshair;
        }

        /// <summary>The body by name in the entity, or its first body for "" (GetBodyInEntity); a pose in Unity space.</summary>
        static bool BodyPose(HplEntity entity, string bodyName, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            HplBody body = null;
            if (string.IsNullOrEmpty(bodyName)) body = entity.Bodies.Count > 0 ? entity.Bodies[0] : null;
            else body = entity.Bodies.Find(b => b.Name == entity.Name + "_" + bodyName);
            if (body == null)
            {
                HplLog.Error($"Could not find body '{bodyName}' in entity '{entity.Name}'!");
                return false;
            }
            position = body.Physics != null ? body.Physics.Position : body.Transform.position;
            rotation = body.Physics != null ? body.Physics.Rotation : body.Transform.rotation;
            return true;
        }

        /// <summary>A copy of the .ent template at the area (cLuxMap::CreateEntity with the area matrix).</summary>
        protected void CreateEntityAtArea(string asEntityName, string asEntityFile, string asAreaName, bool abFullGameSave)
        {
            if (GetEntity(asAreaName, HplEntityType.Area) is not HplArea area) return;
            Transform t = area.GameObject.transform;
            var created = Map.CreateEntity(asEntityName, asEntityFile, t.position, t.rotation, Vector3.one);
            if (created == null) HplLog.Error($"Could not create entity '{asEntityName}' from file '{asEntityFile}'!");
            else created.FullGameSave |= abFullGameSave;
        }

        protected void ReplaceEntity(string asName, string asBodyName, string asNewEntityName, string asNewEntityFile, bool abFullGameSave)
        {
            var entity = GetEntity(asName);
            if (entity == null || !BodyPose(entity, asBodyName, out var position, out var rotation)) return;
            Map.DestroyEntity(entity);
            var created = Map.CreateEntity(asNewEntityName, asNewEntityFile, position, rotation, Vector3.one);
            if (created != null) created.FullGameSave |= abFullGameSave;
            if (created == null)
                HplLog.Error($"Could not create entity '{asNewEntityName}' from file '{asNewEntityFile}'!");
        }

        protected void PlaceEntityAtEntity(string asName, string asTargetEntity, string asTargetBodyName, bool abUseRotation)
        {
            var entity = GetEntity(asName);
            if (entity == null) return;
            if (entity.Bodies.Count == 0)
            {
                HplLog.Error($"Entity '{asName}' has no body that can be placed at '{asTargetEntity}'!");
                return;
            }
            var target = GetEntity(asTargetEntity);
            if (target == null || !BodyPose(target, asTargetBodyName, out var position, out var rotation)) return;
            var body = entity.Bodies[0];
            if (body.Physics != null) body.Physics.SetPose(position, abUseRotation ? rotation : body.Physics.Rotation);
            else body.Transform.SetPositionAndRotation(position, abUseRotation ? rotation : body.Transform.rotation);
        }

        protected void SetEntityPlayerLookAtCallback(string asName, string asCallback, bool abRemoveWhenLookedAt)
        {
            foreach (var e in GetEntities(asName)) e.SetPlayerLookAtCallback(asCallback, abRemoveWhenLookedAt);
        }

        /// <summary>Runs when the player interacts with the entity (interaction, step 4.3).</summary>
        protected void SetEntityPlayerInteractCallback(string asName, string asCallback, bool abRemoveOnInteraction)
        {
            foreach (var e in GetEntities(asName)) e.SetPlayerInteractCallback(asCallback, abRemoveOnInteraction);
        }

        protected void SetEntityCallbackFunc(string asName, string asCallback)
        {
            foreach (var e in GetEntities(asName)) e.CallbackFunc = asCallback ?? "";
        }

        protected void SetEntityConnectionStateChangeCallback(string asName, string asCallback)
        {
            foreach (var e in GetEntities(asName)) e.ConnectionStateChangeCallback = asCallback ?? "";
        }

        protected void SetEntityInteractionDisabled(string asName, bool abDisabled)
        {
            foreach (var e in GetEntities(asName, HplEntityType.Prop)) e.InteractionDisabled = abDisabled;
        }

        protected bool GetEntitiesCollide(string asEntityA, string asEntityB)
        {
            var a = GetEntity(asEntityA);
            var b = a != null ? GetEntity(asEntityB) : null;
            return a != null && b != null && HplCollision.CheckEntityCollision(a, b);
        }

        // ------------------------------------------------------------------ collide callbacks

        protected void AddEntityCollideCallback(string asParentName, string asChildName, string asFunction, bool abDeleteOnCollide, int alStates)
        {
            var child = GetEntity(asChildName);
            if (child == null) return;
            if (asParentName == "Player")
            {
                Map.PlayerCollideCallbacks.Add(child, asFunction, abDeleteOnCollide, alStates);
                return;
            }
            foreach (var e in GetEntities(asParentName)) e.CollideCallbacks.Add(child, asFunction, abDeleteOnCollide, alStates);
        }

        protected void RemoveEntityCollideCallback(string asParentName, string asChildName)
        {
            GetEntity(asChildName); // only to warn if it does not exist, as the original
            if (asParentName == "Player")
            {
                Map.PlayerCollideCallbacks.Remove(asChildName);
                return;
            }
            foreach (var e in GetEntities(asParentName)) e.CollideCallbacks.Remove(asChildName);
        }

        // ------------------------------------------------------------------ props

        protected void SetPropEffectActive(string asName, bool abActive, bool abFadeAndPlaySounds)
        {
            foreach (var p in GetProps(asName)) p.SetEffectsActive(abActive, abFadeAndPlaySounds);
        }

        /// <summary>Active state now; the mesh fade in / dissolve comes with the dissolve materials.</summary>
        protected void SetPropActiveAndFade(string asName, bool abActive, float afFadeTime)
        {
            foreach (var p in GetProps(asName)) p.SetActive(abActive);
        }

        protected void SetPropStaticPhysics(string asName, bool abX)
        {
            foreach (var p in GetProps(asName)) p.SetStaticPhysics(abX);
        }

        protected bool GetPropIsInteractedWith(string asName) => GetEntity(asName, HplEntityType.Prop) is HplProp p && p.IsInteractedWith;

        /// <summary>Static movement around the axis (HPL2 space), optionally around an area (offset).</summary>
        protected void RotatePropToSpeed(string asName, float afAcc, float afGoalSpeed, float afAxisX, float afAxisY, float afAxisZ,
                                         bool abResetSpeed, string asOffsetArea)
        {
            HplArea area = asOffsetArea != "" ? GetEntity(asOffsetArea, HplEntityType.Area) as HplArea : null;
            var axis = new Vector3(afAxisX, afAxisY, afAxisZ);
            foreach (var p in GetProps(asName))
            {
                if (p.MainBody == null) continue;
                if (area != null)
                {
                    Vector3 areaPos = HplSpace.Position(area.GameObject.transform.position);
                    Vector3 local = HplMatrix.Mul(p.MainBody.HplMatrix.inverse, areaPos);
                    p.RotateAtSpeed(afAcc, afGoalSpeed, axis, abResetSpeed, true, areaPos, local);
                }
                else p.RotateAtSpeed(afAcc, afGoalSpeed, axis, abResetSpeed, false, Vector3.zero, Vector3.zero);
            }
        }

        protected void StopPropMovement(string asName)
        {
            foreach (var p in GetProps(asName)) p.StopMove();
        }

        protected void AttachPropToProp(string asPropName, string asAttachName, string asAttachFile, float fPosX, float fPosY, float fPosZ,
                                        float fRotX, float fRotY, float fRotZ) => NotPorted(nameof(AttachPropToProp), "attached props");

        protected void AddAttachedPropToProp(string asPropName, string asAttachName, string asAttachFile, float fPosX, float fPosY, float fPosZ,
                                             float fRotX, float fRotY, float fRotZ) => NotPorted(nameof(AddAttachedPropToProp), "attached props");

        protected void RemoveAttachedPropFromProp(string asPropName, string asAttachName) => NotPorted(nameof(RemoveAttachedPropFromProp), "attached props");

        protected void SetLampLit(string asName, bool abLit, bool abEffects)
        {
            foreach (var p in GetProps(asName, "Lamp")) ((Props.PropLamp)p).SetLit(abLit, abEffects);
        }

        protected void SetSwingDoorLocked(string asName, bool abLocked, bool abEffects)
        {
            foreach (var p in GetProps(asName, "SwingDoor")) ((Props.PropSwingDoor)p).SetLocked(abLocked, abEffects);
        }

        protected void SetSwingDoorClosed(string asName, bool abClosed, bool abEffects)
        {
            foreach (var p in GetProps(asName, "SwingDoor")) ((Props.PropSwingDoor)p).SetClosed(abClosed, abEffects);
        }

        protected void SetSwingDoorDisableAutoClose(string asName, bool abDisableAutoClose)
        {
            foreach (var p in GetProps(asName, "SwingDoor")) ((Props.PropSwingDoor)p).DisableAutoClose = abDisableAutoClose;
        }

        protected bool GetSwingDoorLocked(string asName) =>
            GetEntity(asName, HplEntityType.Prop, "SwingDoor") is Props.PropSwingDoor d && d.Locked;

        protected bool GetSwingDoorClosed(string asName) =>
            GetEntity(asName, HplEntityType.Prop, "SwingDoor") is Props.PropSwingDoor d && d.Closed;

        /// <summary>-1 closed, 1 open (past 70% of the max angle), 0 between (cLuxProp_SwingDoor::GetDoorState).</summary>
        protected int GetSwingDoorState(string asName) =>
            GetEntity(asName, HplEntityType.Prop, "SwingDoor") is Props.PropSwingDoor d ? d.GetDoorState() : 0;

        protected void SetLevelDoorLocked(string asName, bool abLocked)
        {
            foreach (var p in GetProps(asName, "LevelDoor")) ((Props.PropLevelDoor)p).Locked = abLocked;
        }

        protected void SetLevelDoorLockedSound(string asName, string asSound)
        {
            foreach (var p in GetProps(asName, "LevelDoor")) ((Props.PropLevelDoor)p).LockedSound = asSound;
        }

        protected void SetLevelDoorLockedText(string asName, string asTextCat, string asTextEntry)
        {
            foreach (var p in GetProps(asName, "LevelDoor")) ((Props.PropLevelDoor)p).SetLockedText(asTextCat, asTextEntry);
        }

        protected void SetPropObjectStuckState(string asName, int alState)
        {
            foreach (var p in GetProps(asName, "Object")) ((Props.PropObject)p).SetStuckState(alState);
        }

        /// <summary>The angle goes to cLuxProp_Wheel::SetAngle as given (the handler does not convert it).</summary>
        protected void SetWheelAngle(string asName, float afAngle, bool abAutoMove)
        {
            foreach (var p in GetProps(asName, "Wheel")) ((Props.PropWheel)p).SetAngle(afAngle, abAutoMove);
        }

        protected void SetWheelStuckState(string asName, int alState, bool abEffects)
        {
            foreach (var p in GetProps(asName, "Wheel")) ((Props.PropWheel)p).SetStuckState(alState, abEffects);
        }

        protected void SetLeverStuckState(string asName, int alState, bool abEffects)
        {
            foreach (var p in GetProps(asName, "Lever")) ((Props.PropLever)p).SetStuckState(alState, abEffects);
        }

        protected void SetWheelInteractionDisablesStuck(string asName, bool abX)
        {
            foreach (var p in GetProps(asName, "Wheel")) ((Props.PropWheel)p).InteractionDisablesStuck = abX;
        }

        protected void SetLeverInteractionDisablesStuck(string asName, bool abX)
        {
            foreach (var p in GetProps(asName, "Lever")) ((Props.PropLever)p).InteractionDisablesStuck = abX;
        }

        protected int GetLeverState(string asName) =>
            GetEntity(asName, HplEntityType.Prop, "Lever") is Props.PropLever l ? l.CurrentState : 0;

        protected void SetMultiSliderStuckState(string asName, int alStuckState, bool abEffects) => NotPorted(nameof(SetMultiSliderStuckState), "multi sliders");

        protected void SetMultiSliderCallback(string asName, string asCallback) => NotPorted(nameof(SetMultiSliderCallback), "multi sliders");

        protected void SetButtonSwitchedOn(string asName, bool abSwitchedOn, bool abEffects) => NotPorted(nameof(SetButtonSwitchedOn), "buttons");

        protected void SetAllowStickyAreaAttachment(bool abX) => NotPorted(nameof(SetAllowStickyAreaAttachment), "sticky areas");

        protected void AttachPropToStickyArea(string asAreaName, string asProp) => NotPorted(nameof(AttachPropToStickyArea), "sticky areas");

        protected void AttachBodyToStickyArea(string asAreaName, string asBody) => NotPorted(nameof(AttachBodyToStickyArea), "sticky areas");

        protected void DetachFromStickyArea(string asAreaName) => NotPorted(nameof(DetachFromStickyArea), "sticky areas");

        protected void SetNPCAwake(string asName, bool abAwake, bool abEffects) => NotPorted(nameof(SetNPCAwake), NeedsEnemies);

        protected void SetNPCFollowPlayer(string asName, bool abX) => NotPorted(nameof(SetNPCFollowPlayer), NeedsEnemies);

        protected void SetPropHealth(string asName, float afHealth)
        {
            foreach (var p in GetProps(asName)) p.SetHealth(afHealth);
        }

        protected void AddPropHealth(string asName, float afHealth)
        {
            foreach (var p in GetProps(asName)) p.SetHealth(p.Health + afHealth);
        }

        protected float GetPropHealth(string asName) => GetEntity(asName, HplEntityType.Prop) is HplProp p ? p.Health : 0f;

        protected void ResetProp(string asName)
        {
            foreach (var p in GetProps(asName)) p.ResetProperties();
        }

        protected void PlayPropAnimation(string asProp, string asAnimation, float afFadeTime, bool abLoop, string asCallback) =>
            NotPorted(nameof(PlayPropAnimation), "prop animations");

        protected void SetMoveObjectState(string asName, float afState)
        {
            foreach (var p in GetProps(asName, "MoveObject")) ((Props.PropMoveObject)p).MoveToState(afState);
        }

        protected void SetMoveObjectStateExt(string asName, float afState, float afAcc, float afMaxSpeed, float afSlowdownDist, bool abResetSpeed)
        {
            foreach (var p in GetProps(asName, "MoveObject")) ((Props.PropMoveObject)p).MoveToState(afState, afAcc, afMaxSpeed, afSlowdownDist, abResetSpeed);
        }

        // ------------------------------------------------------------------ physics

        /// <summary>"local": the vector turned by the body rotation (VecToCoordSystem); HPL2 space in, Unity out.</summary>
        static Vector3 VecToCoordSystem(Dynamics.HplPhysicsBody body, Vector3 hplVec, string coordSystem)
        {
            if (coordSystem != null && coordSystem.ToLowerInvariant() == "local")
                hplVec = HplMatrix.Mul3x3(HplMatrix.GetRotation(body.HplMatrix), hplVec);
            return HplSpace.Direction(hplVec);
        }

        protected void AddPropForce(string asName, float afX, float afY, float afZ, string asCoordSystem)
        {
            if (GetEntity(asName, HplEntityType.Prop) is not HplProp p) return;
            var v = new Vector3(afX, afY, afZ);
            foreach (var body in p.PhysicsBodies) body.AddForce(VecToCoordSystem(body, v, asCoordSystem));
        }

        protected void AddPropImpulse(string asName, float afX, float afY, float afZ, string asCoordSystem)
        {
            if (GetEntity(asName, HplEntityType.Prop) is not HplProp p) return;
            var v = new Vector3(afX, afY, afZ);
            foreach (var body in p.PhysicsBodies) body.AddImpulse(VecToCoordSystem(body, v, asCoordSystem));
        }

        protected void AddBodyForce(string asName, float afX, float afY, float afZ, string asCoordSystem)
        {
            var body = Map.GetBody(asName)?.Physics;
            if (body == null)
            {
                HplLog.Error($"Could not find body '{asName}'!");
                return;
            }
            body.AddForce(VecToCoordSystem(body, new Vector3(afX, afY, afZ), asCoordSystem));
        }

        protected void AddBodyImpulse(string asName, float afX, float afY, float afZ, string asCoordSystem)
        {
            var body = Map.GetBody(asName)?.Physics;
            if (body == null)
            {
                HplLog.Error($"Could not find body '{asName}'!");
                return;
            }
            body.AddImpulse(VecToCoordSystem(body, new Vector3(afX, afY, afZ), asCoordSystem));
        }

        protected void BreakJoint(string asName)
        {
            var joint = Map.PhysicsWorld.Joints.Find(j => j.Name == asName);
            if (joint == null)
            {
                HplLog.Error($"Could not find joint '{asName}'!");
                return;
            }
            joint.Break();
        }

        protected void SetBodyMass(string asName, float afMass)
        {
            var body = Map.GetBody(asName)?.Physics;
            if (body == null)
            {
                HplLog.Error($"Could not find body '{asName}'!");
                return;
            }
            body.Mass = afMass;
        }

        protected float GetBodyMass(string asName)
        {
            var body = Map.GetBody(asName)?.Physics;
            if (body == null)
            {
                HplLog.Error($"Could not find body '{asName}'!");
                return 0f;
            }
            return body.Mass;
        }

        // ------------------------------------------------------------------ connections

        protected void InteractConnectPropWithRope(string asName, string asPropName, string asRopeName, bool abInteractOnly, float afSpeedMul,
                                                   float afMinSpeed, float afMaxSpeed, bool abInvert, int alStatesUsed) =>
            NotPorted(nameof(InteractConnectPropWithRope), NeedsConnections);

        protected void InteractConnectPropWithMoveObject(string asName, string asPropName, string asMoveObjectName, bool abInteractOnly,
                                                         bool abInvert, int alStatesUsed) =>
            NotPorted(nameof(InteractConnectPropWithMoveObject), NeedsConnections);

        /// <summary>State changes of the main entity (levers, wheels, ...) reach the connected one from step 4.3.</summary>
        protected void ConnectEntities(string asName, string asMainEntity, string asConnectEntity, bool abInvertStateSent, int alStatesUsed,
                                       string asCallbackFunc)
        {
            var main = GetEntity(asMainEntity);
            var connect = main != null ? GetEntity(asConnectEntity) : null;
            if (main == null || connect == null) return;
            main.Connections.Add((asName, connect, abInvertStateSent, alStatesUsed, asCallbackFunc ?? ""));
        }

        protected void CreateRope(string asName, string asStartArea, string asEndArea, string asStartBody, string asEndBody,
                                  float afMinTotalLength, float afMaxTotalLength, float afSegmentLength, float afDamping, float afStrength,
                                  float afStiffness, string asMaterial, float afRadius, float afLengthTileAmount, float afLengthTileSize,
                                  string asSound, float afSoundStartSpeed, float afSoundStopSpeed, bool abAutoMove, float afAutoMoveAcc,
                                  float afAutoMoveMaxSpeed) { } // commented out in the original handler

        // ------------------------------------------------------------------ enemies

        /// <summary>The ported enemies among GetEntities (the man pig of A Machine for Pigs is not in this game).</summary>
        IEnumerable<Enemies.HplEnemy> GetEnemies(string name, string function)
        {
            foreach (var e in GetEntities(name, HplEntityType.Enemy))
            {
                if (e is Enemies.HplEnemy enemy) yield return enemy;
                else NotPorted(function, "the man pig (not in The Dark Descent)");
            }
        }

        protected void SetEnemyDisabled(string asName, bool abDisabled)
        {
            foreach (var e in GetEnemies(asName, nameof(SetEnemyDisabled))) e.SetDisabled(abDisabled);
        }

        protected void SetEnemyIsHallucination(string asName, bool abX)
        {
            foreach (var e in GetEnemies(asName, nameof(SetEnemyIsHallucination))) e.SetHallucination(abX);
        }

        protected void FadeEnemyToSmoke(string asName, bool abPlaySound)
        {
            foreach (var e in GetEnemies(asName, nameof(FadeEnemyToSmoke))) e.FadeToSmoke(abPlaySound);
        }

        /// <summary>The state test of the original is always true (|| instead of &amp;&amp;): every enemy goes hunting.</summary>
        protected void ShowEnemyPlayerPosition(string asName)
        {
            foreach (var e in GetEnemies(asName, nameof(ShowEnemyPlayerPosition)))
            {
                e.ShowPlayerPosition();
                var state = e.CurrentState;
                if (state != Enemies.EnemyState.Hunt || state != Enemies.EnemyState.AttackMeleeLong ||
                    state != Enemies.EnemyState.AttackMeleeShort || state != Enemies.EnemyState.BreakDoor)
                    e.ChangeState(Enemies.EnemyState.Hunt);
            }
        }

        protected void AlertEnemyOfPlayerPresence(string asName)
        {
            foreach (var e in GetEnemies(asName, nameof(AlertEnemyOfPlayerPresence)))
            {
                e.ShowPlayerPosition();
                var state = e.CurrentState;
                if (state != Enemies.EnemyState.Hunt && state != Enemies.EnemyState.AttackMeleeLong &&
                    state != Enemies.EnemyState.AttackMeleeShort && state != Enemies.EnemyState.BreakDoor)
                    e.ChangeState(Enemies.EnemyState.Search);
            }
        }

        protected void SetEnemyDisableTriggers(string asName, bool abX)
        {
            foreach (var e in GetEnemies(asName, nameof(SetEnemyDisableTriggers))) e.SetDisableTriggers(abX);
        }

        protected void AddEnemyPatrolNode(string asEnemyName, string asNodeName, float afWaitTime, string asAnimation)
        {
            foreach (var e in GetEnemies(asEnemyName, nameof(AddEnemyPatrolNode)))
            {
                var container = e.Pathfinder.NodeContainer;
                if (container == null)
                {
                    HplLog.Error($"There is no node container in enemy '{e.Name}'! (probably no nodes in map!)");
                    continue;
                }
                var node = container.GetNodeFromName(asNodeName);
                if (node == null)
                {
                    HplLog.Error($"Could not find node '{asNodeName}' for enemy '{e.Name}'");
                    continue;
                }
                e.AddPatrolNode(node, afWaitTime, asAnimation);
            }
        }

        protected void ClearEnemyPatrolNodes(string asEnemyName)
        {
            foreach (var e in GetEnemies(asEnemyName, nameof(ClearEnemyPatrolNodes))) e.ClearPatrolNodes();
        }

        protected void SetEnemySanityDecreaseActive(string asName, bool abX)
        {
            foreach (var e in GetEnemies(asName, nameof(SetEnemySanityDecreaseActive))) e.SetSanityDecreaseActive(abX);
        }

        protected void TeleportEnemyToNode(string asEnemyName, string asNodeName, bool abChangeY)
        {
            foreach (var e in GetEnemies(asEnemyName, nameof(TeleportEnemyToNode)))
            {
                var node = e.Pathfinder.NodeContainer?.GetNodeFromName(asNodeName);
                if (node == null)
                {
                    HplLog.Error($"Could not find node '{asNodeName}' for enemy '{e.Name}'");
                    continue;
                }
                Vector3 pos = node.Position;
                if (!abChangeY) pos.y = e.Body.FeetPosition.y;
                e.Body.SetFeetPosition(pos);
            }
        }

        protected void TeleportEnemyToEntity(string asEnemyName, string asTargetEntity, string asTargetBody, bool abChangeY)
        {
            foreach (var e in GetEnemies(asEnemyName, nameof(TeleportEnemyToEntity)))
            {
                var target = GetEntity(asTargetEntity);
                if (target == null)
                {
                    HplLog.Error($"Could not find entity '{asTargetEntity}' for enemy '{e.Name}' to be teleported to.");
                    continue;
                }
                Vector3 pos;
                if (target is Enemies.HplEnemy targetEnemy) pos = targetEnemy.Body.FeetPosition;
                else
                {
                    var body = target.Bodies.Find(b => b.Name == target.Name + "_" + asTargetBody) ?? (target.Bodies.Count > 0 ? target.Bodies[0] : null);
                    if (body == null) return;
                    pos = body.Transform.position;
                }
                if (!abChangeY) pos.y = e.Body.FeetPosition.y;
                e.Body.SetFeetPosition(pos);
            }
        }
        protected void ChangeManPigPose(string asName, string asPoseType) => NotPorted(nameof(ChangeManPigPose), NeedsEnemies);
        protected void SetTeslaPigFadeDisabled(string asName, bool abX) => NotPorted(nameof(SetTeslaPigFadeDisabled), NeedsEnemies);
        protected void SetTeslaPigSoundDisabled(string asName, bool abX) => NotPorted(nameof(SetTeslaPigSoundDisabled), NeedsEnemies);
        protected void SetTeslaPigEasyEscapeDisabled(string asName, bool abX) => NotPorted(nameof(SetTeslaPigEasyEscapeDisabled), NeedsEnemies);
        protected void ForceTeslaPigSighting(string asName) => NotPorted(nameof(ForceTeslaPigSighting), NeedsEnemies);
        protected string GetEnemyStateName(string asName)
        {
            if (GetEntity(asName, HplEntityType.Enemy) is Enemies.HplEnemy enemy) return enemy.CurrentStateName;
            HplLog.Error($"Can't find enemy '{asName}'!");
            return "";
        }
    }
}

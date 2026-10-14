using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    // Entities, lights, props, collide callbacks, physics and enemies (LuxScriptHandler.cpp).
    // Props are static until step 4.2 (entity physics); doors, levers, lamps etc. behave from step 4.3. Their
    // script state is kept, so Get* functions return what scripts set.
    public abstract partial class HplScript
    {
        const string NeedsPhysics = "entity physics (step 4.2)";
        const string NeedsInteraction = "prop interaction (step 4.3)";
        const string NeedsEnemies = "enemies (step 6)";

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

        /// <summary>Moves the entity so that its first body is at the position (bodies are not separate until step 4.2).</summary>
        protected void SetEntityPos(string asName, float afX, float afY, float afZ)
        {
            Vector3 position = HplSpace.Position(new Vector3(afX, afY, afZ));
            foreach (var e in GetEntities(asName))
            {
                if (e.Bodies.Count == 0) continue;
                e.GameObject.transform.position += position - e.Bodies[0].Transform.position;
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
            return e.Bodies[0].Transform.position;
        }

        protected void SetEntityCustomFocusCrossHair(string asName, string asCrossHair)
        {
            foreach (var e in GetEntities(asName)) e.CustomFocusCrossHair = asCrossHair;
        }

        protected void CreateEntityAtArea(string asEntityName, string asEntityFile, string asAreaName, bool abFullGameSave) =>
            NotPorted(nameof(CreateEntityAtArea), "entities created at runtime");

        protected void ReplaceEntity(string asName, string asBodyName, string asNewEntityName, string asNewEntityFile, bool abFullGameSave) =>
            NotPorted(nameof(ReplaceEntity), "entities created at runtime");

        protected void PlaceEntityAtEntity(string asName, string asTargetEntity, string asTargetBodyName, bool abUseRotation) =>
            NotPorted(nameof(PlaceEntityAtEntity), NeedsPhysics);

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

        protected void SetPropEffectActive(string asName, bool abActive, bool abFadeAndPlaySounds) => NotPorted(nameof(SetPropEffectActive), NeedsInteraction);

        /// <summary>Active state now; the mesh fade in / dissolve comes later.</summary>
        protected void SetPropActiveAndFade(string asName, bool abActive, float afFadeTime)
        {
            foreach (var e in GetEntities(asName, HplEntityType.Prop)) e.SetActive(abActive);
        }

        protected void SetPropStaticPhysics(string asName, bool abX) => NotPorted(nameof(SetPropStaticPhysics), NeedsPhysics);

        protected bool GetPropIsInteractedWith(string asName) => false;

        protected void RotatePropToSpeed(string asName, float afAcc, float afGoalSpeed, float afAxisX, float afAxisY, float afAxisZ,
                                         bool abResetSpeed, string asOffsetArea) => NotPorted(nameof(RotatePropToSpeed), NeedsPhysics);

        protected void StopPropMovement(string asName) { }

        protected void AttachPropToProp(string asPropName, string asAttachName, string asAttachFile, float fPosX, float fPosY, float fPosZ,
                                        float fRotX, float fRotY, float fRotZ) => NotPorted(nameof(AttachPropToProp), "entities created at runtime");

        protected void AddAttachedPropToProp(string asPropName, string asAttachName, string asAttachFile, float fPosX, float fPosY, float fPosZ,
                                             float fRotX, float fRotY, float fRotZ) => NotPorted(nameof(AddAttachedPropToProp), "entities created at runtime");

        protected void RemoveAttachedPropFromProp(string asPropName, string asAttachName) { }

        protected void SetLampLit(string asName, bool abLit, bool abEffects)
        {
            foreach (var p in GetProps(asName, "Lamp")) p.LampLit = abLit;
            NotPorted(nameof(SetLampLit), NeedsInteraction);
        }

        protected void SetSwingDoorLocked(string asName, bool abLocked, bool abEffects)
        {
            foreach (var p in GetProps(asName, "SwingDoor")) p.DoorLocked = abLocked;
            NotPorted(nameof(SetSwingDoorLocked), NeedsInteraction);
        }

        protected void SetSwingDoorClosed(string asName, bool abClosed, bool abEffects)
        {
            foreach (var p in GetProps(asName, "SwingDoor")) p.DoorClosed = abClosed;
            NotPorted(nameof(SetSwingDoorClosed), NeedsInteraction);
        }

        protected void SetSwingDoorDisableAutoClose(string asName, bool abDisableAutoClose)
        {
            foreach (var p in GetProps(asName, "SwingDoor")) p.DoorDisableAutoClose = abDisableAutoClose;
        }

        protected bool GetSwingDoorLocked(string asName) => GetEntity(asName, HplEntityType.Prop, "SwingDoor") is HplProp p && p.DoorLocked;

        protected bool GetSwingDoorClosed(string asName) => GetEntity(asName, HplEntityType.Prop, "SwingDoor") is HplProp p && p.DoorClosed;

        /// <summary>-1 closed, 1 open (70% or more), 0 between (cLuxMap::GetDoorState); open / closed only for now.</summary>
        protected int GetSwingDoorState(string asName) =>
            GetEntity(asName, HplEntityType.Prop, "SwingDoor") is HplProp p ? (p.DoorClosed ? -1 : 1) : 0;

        protected void SetLevelDoorLocked(string asName, bool abLocked)
        {
            foreach (var p in GetProps(asName, "LevelDoor")) p.DoorLocked = abLocked;
        }

        protected void SetLevelDoorLockedSound(string asName, string asSound) { }

        protected void SetLevelDoorLockedText(string asName, string asTextCat, string asTextEntry) { }

        protected void SetPropObjectStuckState(string asName, int alState) => NotPorted(nameof(SetPropObjectStuckState), NeedsInteraction);

        protected void SetWheelAngle(string asName, float afAngle, bool abAutoMove) => NotPorted(nameof(SetWheelAngle), NeedsInteraction);

        protected void SetWheelStuckState(string asName, int alState, bool abEffects) => NotPorted(nameof(SetWheelStuckState), NeedsInteraction);

        protected void SetLeverStuckState(string asName, int alState, bool abEffects)
        {
            foreach (var p in GetProps(asName, "Lever")) p.LeverState = alState;
            NotPorted(nameof(SetLeverStuckState), NeedsInteraction);
        }

        protected void SetWheelInteractionDisablesStuck(string asName, bool abX) { }

        protected void SetLeverInteractionDisablesStuck(string asName, bool abX) { }

        protected int GetLeverState(string asName) => GetEntity(asName, HplEntityType.Prop, "Lever") is HplProp p ? p.LeverState : 0;

        protected void SetMultiSliderStuckState(string asName, int alStuckState, bool abEffects) => NotPorted(nameof(SetMultiSliderStuckState), NeedsInteraction);

        protected void SetMultiSliderCallback(string asName, string asCallback) { }

        protected void SetButtonSwitchedOn(string asName, bool abSwitchedOn, bool abEffects) => NotPorted(nameof(SetButtonSwitchedOn), NeedsInteraction);

        protected void SetAllowStickyAreaAttachment(bool abX) { }

        protected void AttachPropToStickyArea(string asAreaName, string asProp) => NotPorted(nameof(AttachPropToStickyArea), NeedsPhysics);

        protected void AttachBodyToStickyArea(string asAreaName, string asBody) => NotPorted(nameof(AttachBodyToStickyArea), NeedsPhysics);

        protected void DetachFromStickyArea(string asAreaName) { }

        protected void SetNPCAwake(string asName, bool abAwake, bool abEffects) => NotPorted(nameof(SetNPCAwake), NeedsInteraction);

        protected void SetNPCFollowPlayer(string asName, bool abX) => NotPorted(nameof(SetNPCFollowPlayer), NeedsInteraction);

        protected void SetPropHealth(string asName, float afHealth)
        {
            foreach (var p in GetProps(asName)) p.Health = afHealth;
        }

        protected void AddPropHealth(string asName, float afHealth)
        {
            foreach (var p in GetProps(asName)) p.Health += afHealth;
        }

        protected float GetPropHealth(string asName) => GetEntity(asName, HplEntityType.Prop) is HplProp p ? p.Health : 0f;

        protected void ResetProp(string asName) => NotPorted(nameof(ResetProp), NeedsPhysics);

        protected void PlayPropAnimation(string asProp, string asAnimation, float afFadeTime, bool abLoop, string asCallback) =>
            NotPorted(nameof(PlayPropAnimation), "prop animations");

        protected void SetMoveObjectState(string asName, float afState) => NotPorted(nameof(SetMoveObjectState), NeedsInteraction);

        protected void SetMoveObjectStateExt(string asName, float afState, float afAcc, float afMaxSpeed, float afSlowdownDist, bool abResetSpeed) =>
            NotPorted(nameof(SetMoveObjectStateExt), NeedsInteraction);

        // ------------------------------------------------------------------ physics

        protected void AddPropForce(string asName, float afX, float afY, float afZ, string asCoordSystem) => NotPorted(nameof(AddPropForce), NeedsPhysics);

        protected void AddPropImpulse(string asName, float afX, float afY, float afZ, string asCoordSystem) => NotPorted(nameof(AddPropImpulse), NeedsPhysics);

        protected void AddBodyForce(string asName, float afX, float afY, float afZ, string asCoordSystem)
        {
            if (Map.GetBody(asName) == null) HplLog.Error($"Could not find body '{asName}'!");
            else NotPorted(nameof(AddBodyForce), NeedsPhysics);
        }

        protected void AddBodyImpulse(string asName, float afX, float afY, float afZ, string asCoordSystem)
        {
            if (Map.GetBody(asName) == null) HplLog.Error($"Could not find body '{asName}'!");
            else NotPorted(nameof(AddBodyImpulse), NeedsPhysics);
        }

        protected void BreakJoint(string asName) => NotPorted(nameof(BreakJoint), NeedsPhysics);

        protected void SetBodyMass(string asName, float afMass)
        {
            var body = Map.GetBody(asName);
            if (body?.Data != null) body.Data.mass = afMass;
            else HplLog.Error($"Could not find body '{asName}'!");
        }

        protected float GetBodyMass(string asName)
        {
            var body = Map.GetBody(asName);
            if (body?.Data != null) return body.Data.mass;
            HplLog.Error($"Could not find body '{asName}'!");
            return 0f;
        }

        // ------------------------------------------------------------------ connections

        protected void InteractConnectPropWithRope(string asName, string asPropName, string asRopeName, bool abInteractOnly, float afSpeedMul,
                                                   float afMinSpeed, float afMaxSpeed, bool abInvert, int alStatesUsed) =>
            NotPorted(nameof(InteractConnectPropWithRope), NeedsInteraction);

        protected void InteractConnectPropWithMoveObject(string asName, string asPropName, string asMoveObjectName, bool abInteractOnly,
                                                         bool abInvert, int alStatesUsed) =>
            NotPorted(nameof(InteractConnectPropWithMoveObject), NeedsInteraction);

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

        // ------------------------------------------------------------------ enemies (step 6)

        protected void SetEnemyDisabled(string asName, bool abDisabled) => NotPorted(nameof(SetEnemyDisabled), NeedsEnemies);
        protected void SetEnemyIsHallucination(string asName, bool abX) => NotPorted(nameof(SetEnemyIsHallucination), NeedsEnemies);
        protected void FadeEnemyToSmoke(string asName, bool abPlaySound) => NotPorted(nameof(FadeEnemyToSmoke), NeedsEnemies);
        protected void ShowEnemyPlayerPosition(string asName) => NotPorted(nameof(ShowEnemyPlayerPosition), NeedsEnemies);
        protected void AlertEnemyOfPlayerPresence(string asName) => NotPorted(nameof(AlertEnemyOfPlayerPresence), NeedsEnemies);
        protected void SetEnemyDisableTriggers(string asName, bool abX) => NotPorted(nameof(SetEnemyDisableTriggers), NeedsEnemies);
        protected void AddEnemyPatrolNode(string asEnemyName, string asNodeName, float afWaitTime, string asAnimation) => NotPorted(nameof(AddEnemyPatrolNode), NeedsEnemies);
        protected void ClearEnemyPatrolNodes(string asEnemyName) => NotPorted(nameof(ClearEnemyPatrolNodes), NeedsEnemies);
        protected void SetEnemySanityDecreaseActive(string asName, bool abX) => NotPorted(nameof(SetEnemySanityDecreaseActive), NeedsEnemies);
        protected void TeleportEnemyToNode(string asEnemyName, string asNodeName, bool abChangeY) => NotPorted(nameof(TeleportEnemyToNode), NeedsEnemies);
        protected void TeleportEnemyToEntity(string asEnemyName, string asTargetEntity, string asTargetBody, bool abChangeY) => NotPorted(nameof(TeleportEnemyToEntity), NeedsEnemies);
        protected void ChangeManPigPose(string asName, string asPoseType) => NotPorted(nameof(ChangeManPigPose), NeedsEnemies);
        protected void SetTeslaPigFadeDisabled(string asName, bool abX) => NotPorted(nameof(SetTeslaPigFadeDisabled), NeedsEnemies);
        protected void SetTeslaPigSoundDisabled(string asName, bool abX) => NotPorted(nameof(SetTeslaPigSoundDisabled), NeedsEnemies);
        protected void SetTeslaPigEasyEscapeDisabled(string asName, bool abX) => NotPorted(nameof(SetTeslaPigEasyEscapeDisabled), NeedsEnemies);
        protected void ForceTeslaPigSighting(string asName) => NotPorted(nameof(ForceTeslaPigSighting), NeedsEnemies);
        protected string GetEnemyStateName(string asName) => "";
    }
}

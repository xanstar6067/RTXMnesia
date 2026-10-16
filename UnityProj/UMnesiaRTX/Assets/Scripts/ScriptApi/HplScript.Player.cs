using RTXMnesia.Hpl;
using RTXMnesia.Player;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    // Player functions (LuxScriptHandler.cpp → cLuxPlayer). HPL2 positions and directions are converted with HplSpace.
    public abstract partial class HplScript
    {
        protected void SetPlayerActive(bool abActive) => Player.active = abActive;

        protected void ChangePlayerStateToNormal() { }

        protected void SetPlayerCrouching(bool abCrouch) => Player.MoveState.SetCrouch(abCrouch);

        /// <summary>As the original, local coordinates use Y for both the right and the up axis.</summary>
        protected void AddPlayerBodyForce(float afX, float afY, float afZ, bool abUseLocalCoords)
        {
            var body = Player.Body;
            Vector3 force = abUseLocalCoords
                ? body.Forward * afZ + body.Right * afY + Vector3.up * afY
                : HplSpace.Direction(new Vector3(afX, afY, afZ));
            body.AddForce(force);
        }

        protected void ShowPlayerCrossHairIcons(bool abX) => Player.showCrossHairIcons = abX;

        protected void SetPlayerPos(float afX, float afY, float afZ) =>
            Player.Body.SetFeetPosition(HplSpace.Position(new Vector3(afX, afY, afZ)));

        protected float GetPlayerPosX() => HplSpace.Position(Player.Body.FeetPosition).x;
        protected float GetPlayerPosY() => Player.Body.FeetPosition.y;
        protected float GetPlayerPosZ() => Player.Body.FeetPosition.z;

        protected void SetPlayerSanity(float afSanity) => Player.SetSanity(afSanity);
        protected void AddPlayerSanity(float afSanity) => Player.AddSanity(afSanity);
        protected float GetPlayerSanity() => Player.sanity;
        protected void SetPlayerHealth(float afHealth) => Player.SetHealth(afHealth);
        protected void AddPlayerHealth(float afHealth) => Player.AddHealth(afHealth);
        protected float GetPlayerHealth() => Player.health;
        protected void SetPlayerLampOil(float afOil) => Player.SetLampOil(afOil);
        protected void AddPlayerLampOil(float afOil) => Player.AddLampOil(afOil);
        protected float GetPlayerLampOil() => Player.lampOil;

        protected float GetPlayerSpeed() => Player.Body.GetVelocity(1f / 60f).magnitude;
        protected float GetPlayerYSpeed() => Player.Body.GetVelocity(1f / 60f).y;

        protected void MovePlayerForward(float afAmount) => Player.Body.Move(CharDir.Forward, afAmount);

        protected void SetPlayerPermaDeathSound(string asSound) { }

        protected void SetSanityDrainDisabled(bool abX) => Player.sanityDrainDisabled = abX;

        protected void GiveSanityBoost()
        {
            float s = Player.sanity;
            if (s < 25f) Player.AddSanity(100f - s);
            else if (s < 50f) Player.AddSanity(90f - s);
            else if (s < 75f) Player.AddSanity(80f - s);
            else Player.AddSanity(5f);
        }

        protected void GiveSanityBoostSmall()
        {
            float s = Player.sanity;
            if (s < 25f) Player.AddSanity(20f);
            else if (s < 50f) Player.AddSanity(15f);
            else if (s < 75f) Player.AddSanity(10f);
            else Player.AddSanity(5f);
        }

        protected void GiveSanityDamage(float afAmount, bool abUseEffect)
        {
            if (abUseEffect) Player.GiveSanityDamage(afAmount);
            else Player.LowerSanity(afAmount, false);
        }

        /// <summary>Health only for now; damage splash, red flash and head spin come with step 6.</summary>
        protected void GivePlayerDamage(float afAmount, string asType, bool abSpinHead, bool abLethal) =>
            Player.GiveDamage(afAmount, abLethal);

        protected void FadePlayerFOVMulTo(float afX, float afSpeed) => Player.FadeFOVMulTo(afX, afSpeed);

        protected void FadePlayerAspectMulTo(float afX, float afSpeed) => Player.FadeAspectMulTo(afX, afSpeed);

        /// <summary>Degrees in the script, radians in the player.</summary>
        protected void FadePlayerRollTo(float afX, float afSpeedMul, float afMaxSpeed) =>
            Player.FadeRollTo(afX * Mathf.Deg2Rad, afSpeedMul, afMaxSpeed * Mathf.Deg2Rad);

        /// <summary>
        /// Head offset relative to the body: x along its right, y up, z along its forward (CharacterBody.cpp), the
        /// same in Unity, so the values are used as they are.
        /// </summary>
        protected void MovePlayerHeadPos(float afX, float afY, float afZ, float afSpeed, float afSlowDownDist) =>
            Player.MoveHeadPosAdd(HeadPosAdd.Script, new Vector3(afX, afY, afZ), afSpeed, afSlowDownDist);

        /// <summary>The target is the first body of the entity (areas: their box) or the center of its mesh.</summary>
        protected void StartPlayerLookAt(string asEntityName, float afSpeedMul, float afMaxSpeed, string asAtTargetCallback)
        {
            var entity = GetEntity(asEntityName);
            if (entity == null) return;

            Vector3 target;
            if (entity.Bodies.Count > 0) target = entity.Bodies[0].Transform.position;
            else if (entity.GameObject.GetComponentInChildren<Renderer>() is Renderer r) target = r.bounds.center;
            else
            {
                HplLog.Error($"Entity {asEntityName} does not have body or mesh entity! Cannot make player look at it!");
                return;
            }
            Player.LookAt.SetTarget(target, afSpeedMul, afMaxSpeed, asAtTargetCallback);
            Player.LookAt.SetActive(true);
        }

        protected void StopPlayerLookAt() => Player.LookAt.SetActive(false);

        protected void SetPlayerMoveSpeedMul(float afMul) => Player.scriptMoveSpeedMul = afMul;
        protected void SetPlayerRunSpeedMul(float afMul) => Player.scriptRunSpeedMul = afMul;
        protected void SetPlayerLookSpeedMul(float afMul) => Player.lookSpeedMul = afMul;
        protected void SetPlayerJumpForceMul(float afMul) => Player.scriptJumpForceMul = afMul;
        protected void SetPlayerJumpDisabled(bool abX) => Player.jumpDisabled = abX;
        protected void SetPlayerCrouchDisabled(bool abX) => Player.crouchDisabled = abX;
        protected void SetPlayerFallDamageDisabled(bool abX) => Player.noFallDamage = abX;

        protected void TeleportPlayer(string asStartPosName)
        {
            var start = Map.GetPlayerStart(asStartPosName);
            if (start == null)
            {
                HplLog.Error($"Start pos '{asStartPosName}' could not be found!");
                return;
            }
            Player.PlaceAtStart(start);
        }

        protected void SetLanternActive(bool abX, bool abUseEffects) => Player.Lantern.SetActive(abX, abUseEffects);

        protected bool GetLanternActive() => Player.lanternActive;

        protected void SetLanternDisabled(bool abX) => Player.Lantern.SetDisabled(abX);

        protected void SetLanternLitCallback(string asCallback) => Map.LanternLitCallback = asCallback ?? "";
    }
}

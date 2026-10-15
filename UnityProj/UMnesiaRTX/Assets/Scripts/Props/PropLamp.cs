using RTXMnesia.Dynamics;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_Lamp (LuxProp_Lamp.cpp): lit / unlit through the prop effects; the player lights it with a
    /// tinderbox (sanity boost depending on the current sanity). Connection to a map light (ConnectedLight) adds the
    /// lamp's light color to it.
    /// </summary>
    public sealed class PropLamp : HplProp
    {
        bool _canBeLitByPlayer = true;
        string _connectionLight = "";
        float _connectionLightAmount;
        bool _connectionLightUseOnColor, _connectionLightUseSpec, _lightConnectionSetup;

        public bool Lit { get; private set; } = true;

        public PropLamp(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        protected override void LoadVariables()
        {
            _canBeLitByPlayer = VarBool("CanBeLitByPlayer", true);
            // Grab variables are loaded by the original but lamps are never grabbed (no interaction state uses them).
        }

        protected override void LoadInstanceVariables()
        {
            SetLit(InstanceBool("Lit", true), false);
            _connectionLight = InstanceString("ConnectedLight", "");
            _connectionLightAmount = InstanceFloat("ConnectionLightAmount", 0f);
            _connectionLightUseOnColor = InstanceBool("ConnectionLightUseOnColor", false);
            _connectionLightUseSpec = InstanceBool("ConnectionLightUseSpec", false);
        }

        bool CanBeIgnitedByPlayer => !Lit && _canBeLitByPlayer;

        public override bool CanInteract(HplPhysicsBody body) => (CanBeIgnitedByPlayer && !Lit) || InteractCallback != "";

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            if (Lit) return true;

            var player = Map.player;
            if (player.tinderboxes <= 0)
            {
                Map.hud.Messages.SetMessage(Map.hud.Translate("Game", "NoMoreTinderboxes"), 0f);
                return false;
            }

            // The emptier the sanity, the bigger the boost (squared).
            float increase = 1f - player.sanity / 100f;
            increase *= increase;
            float min = GameConfig.GetFloat("Player_Sanity", "LightLampMinSanityIncrease");
            float max = GameConfig.GetFloat("Player_Sanity", "LightLampMaxSanityIncrease");
            float add = min * (1f - increase) + max * increase;
            player.AddSanity(add, false);
            player.tinderboxes -= 1;
            // ui_use_tinderbox sound: step 6.
            RunCallbackFunc("OnIgnite");
            SetLit(true, true);
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            if (CustomFocusCrossHair != FocusCrosshair.Default && Lit) return CustomFocusCrossHair;
            return CanInteract(body) ? FocusCrosshair.Ignite : FocusCrosshair.Default;
        }

        public override string GetFocusText() =>
            CanInteract(MainBody) && !Lit ? "x " + Map.player.tinderboxes : "";

        public void SetLit(bool lit, bool useEffects)
        {
            if (Lit == lit) return;
            Lit = lit;
            SetEffectsActive(Lit, useEffects);
        }

        public override void OnConnectionStateChange(HplEntity entity, int state)
        {
            if (state > 0) SetLit(true, true);
            if (state < 0) SetLit(false, true);
        }

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (!_lightConnectionSetup) SetupLampLightConnection();
            UpdateLightConnection();
        }

        AmnesiaLight _connected;

        /// <summary>The map light (not one of an entity) with the connection name.</summary>
        void SetupLampLightConnection()
        {
            if (_connectionLight != "")
            {
                _connected = Map.GetLight(_connectionLight);
                if (_connected != null && _connected.GetComponentInParent<AmnesiaEntity>() != null) _connected = null;
                if (_connected == null) HplLog.Error($"Light '{_connectionLight}' that is connected to lamp '{Name}' does not exist!");
            }
            _lightConnectionSetup = true;
        }

        /// <summary>cLuxLampLightConnection::Update for one lamp: default color + lamp light × amount × effects alpha.</summary>
        void UpdateLightConnection()
        {
            if (_connected == null || Lights.Count == 0) return;
            Color lamp = _connectionLightUseOnColor ? LightData[0].onColor : Lights[0].CurrentColor;
            if (!_connectionLightUseSpec) lamp.a = 0f;
            _connected.SetColor(_connected.diffuseColor + lamp * _connectionLightAmount * EffectsAlpha);
        }
    }
}

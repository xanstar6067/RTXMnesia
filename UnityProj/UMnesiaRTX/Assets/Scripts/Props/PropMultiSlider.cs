using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_MultiSlider (LuxProp_MultiSlider.cpp): a slider joint with NumOfStates evenly spaced positions over
    /// its range. Moving the slide (InteractSlide) within StickToStateMaxDist of a state changes to it (sound,
    /// ChangeStateCallback(name, state)); let go, a PID force moves it to the current state. A stuck state narrows the
    /// limits to ±5 cm around its position (the end states to the position itself). On 20_sewer: the two water
    /// wheel cog panels.
    /// </summary>
    public sealed class PropMultiSlider : HplProp
    {
        readonly InteractDataSlide _slide = new();
        int _numOfStates = 2;
        float _stickToStateMaxDist;
        bool _canInteractWithStaticBody = true, _autoMoveToCurrentState = true;
        float _autoMoveSpeedFactor, _autoMoveMaxSpeed;
        string _changeStateSound = "", _stuckSound = "";
        HplJoint _sliderJoint;
        HplPhysicsBody _sliderBody;
        float[] _statePos = new float[0];
        float _defaultMinDist, _defaultMaxDist;
        float _stuckSoundTimer;
        readonly HplPid _autoMovePid = new(40f, 0f, 0f, 10); // derivative left out, as the original

        // Saved (cLuxProp_MultiSlider_SaveData).
        int _currentState = -1, _stuckState = -1;
        bool _interactionDisablesStuck;
        string _changeStateCallback = "";

        public PropMultiSlider(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        public int CurrentState => _currentState;
        public int StuckState => _stuckState;
        public int NumOfStates => _statePos.Length;
        public float StatePosition(int state) => _statePos[state];
        public float Distance => _sliderJoint != null ? _sliderJoint.GetDistance() : 0f;
        public bool InteractionDisablesStuck { get => _interactionDisablesStuck; set => _interactionDisablesStuck = value; }
        public void SetChangeStateCallback(string callback) => _changeStateCallback = callback ?? "";

        protected override string DefaultFocusDistanceKey => "MultiSlider_DefaultMaxFocusDist";

        protected override void LoadVariables()
        {
            _slide.slideMaxSpeed = VarFloat("SlideMaxSpeed", 5f);
            _slide.slideSlowDownFactor = VarFloat("SlideSlowDownFactor", 1f);
            _slide.slideSpeedFactor = VarFloat("SlideSpeedFactor", 1f);
            _slide.slideThrowImpulse = VarFloat("SlideThrowImpulse", 3f);
            _numOfStates = Mathf.RoundToInt(VarFloat("NumOfStates", 2f));
            if (_numOfStates < 2) Debug.LogWarning($"MultiSlider '{Name}' has fewer than 2 states! It will not function properly!");
            _stickToStateMaxDist = VarFloat("StickToStateMaxDist", 0.1f);
            _canInteractWithStaticBody = VarBool("CanInteractWithStaticBody", true);
            _autoMoveToCurrentState = VarBool("AutoMoveToCurrentState", true);
            _autoMoveSpeedFactor = VarFloat("AutoMoveSpeedFactor", 2f);
            _autoMoveMaxSpeed = VarFloat("AutoMoveMaxSpeed", 8f);
            _changeStateSound = VarString("ChangeStateSound", "");
            _stuckSound = VarString("StuckSound", "");
        }

        protected override void OnSetupAfterLoad()
        {
            if (Joints.Count == 0 || Joints[0].Type != HplJointType.Slider)
            {
                HplLog.Error($"MultiSlider {Name} does not have a hinge joint!");
                return;
            }
            _sliderJoint = Joints[0];
            _sliderBody = _sliderJoint.Child;
            _defaultMinDist = _sliderJoint.MinDistance;
            _defaultMaxDist = _sliderJoint.MaxDistance;

            int count = Mathf.Max(_numOfStates, 0);
            _statePos = new float[count];
            float add = count > 1 ? (_defaultMaxDist - _defaultMinDist) / (count - 1) : 0f;
            float pos = _defaultMinDist;
            for (int i = 0; i < count; i++)
            {
                _statePos[i] = pos;
                pos += add;
            }
        }

        protected override void LoadInstanceVariables()
        {
            SetStuckState(Mathf.RoundToInt(InstanceFloat("StuckState", -1f)), false);
            _interactionDisablesStuck = InstanceBool("InteractionDisablesStuck", false);
            _changeStateCallback = InstanceString("ChangeStateCallback", "");
        }

        public override InteractDataSlide GetSlideData() => _slide;

        public override bool CanInteract(HplPhysicsBody body) => body != null && !(body.IsStatic && !_canInteractWithStaticBody);

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            if (body.IsStatic && _canInteractWithStaticBody)
            {
                var dynamic = PhysicsBodies.Find(b => b.Mass > 0f);
                if (dynamic != null) body = dynamic;
            }
            // As the original: "!= 0", so being stuck in state 0 is never released by interaction.
            if (_stuckState != 0 && _interactionDisablesStuck) SetStuckState(-1, true);
            Map.hud.Hints.Add("EntitySlide", Map.hud.Translate("Hints", "EntitySlide"), 0f);
            PlayerStateVars.SetupInteraction(body, hplFocusPos);
            Map.player.States.ChangeState(PlayerStateType.InteractSlide);
            _autoMovePid.Reset();
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) =>
            body != null && body.IsStatic && !_canInteractWithStaticBody ? FocusCrosshair.Default : FocusCrosshair.Grab;

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (_sliderJoint == null) return;
            float dist = _sliderJoint.GetDistance();
            UpdateCheckStuckSound(timeStep);
            UpdateCheckNewState(dist);
            UpdateAutoMove(dist, timeStep);
        }

        public void SetStuckState(int state, bool effects)
        {
            if (_stuckState == state || _sliderJoint == null) return;
            _stuckState = state;
            _stuckSoundTimer = 0f;
            ApplyStuckLimits(effects);
        }

        void ApplyStuckLimits(bool effects)
        {
            if (_stuckState >= 0 && _stuckState < _statePos.Length)
            {
                ChangeState(_stuckState, effects);
                float pos = _statePos[_stuckState];
                float min = _stuckState == 0 ? pos : pos - 0.05f;
                float max = _stuckState == _statePos.Length - 1 ? pos : pos + 0.05f;
                _sliderJoint.SetMinDistance(min);
                _sliderJoint.SetMaxDistance(max);
            }
            else
            {
                _sliderJoint.SetMinDistance(_defaultMinDist);
                _sliderJoint.SetMaxDistance(_defaultMaxDist);
            }
            _sliderBody?.WakeUp();
        }

        /// <summary>The saved stuck state and current state are restored as fields: put the limits back.</summary>
        public override void AfterStateRestore()
        {
            if (_sliderJoint == null) return;
            int current = _currentState;
            ApplyStuckLimits(false);
            _currentState = current;
        }

        // Multiple states do not work with connections.
        public override void OnConnectionStateChange(HplEntity entity, int state) { }

        void UpdateCheckStuckSound(float timeStep)
        {
            if (_stuckState < 0 || !IsInteractedWith) return;
            if (_stuckSoundTimer > 0f)
            {
                _stuckSoundTimer -= timeStep;
                return;
            }
            if (_sliderJoint.Child.LinearVelocity.sqrMagnitude > 0.01f)
            {
                PlaySound("MultiSliderStuck", _stuckSound, true, true);
                _stuckSoundTimer = 1.5f;
            }
        }

        void UpdateCheckNewState(float pos)
        {
            if (_stuckState >= 0) return;
            for (int i = 0; i < _statePos.Length; i++)
            {
                if (i == _currentState) continue;
                if (Mathf.Abs(pos - _statePos[i]) <= _stickToStateMaxDist)
                {
                    ChangeState(i, true);
                    break;
                }
            }
        }

        void UpdateAutoMove(float pos, float timeStep)
        {
            if (IsInteractedWith || !_autoMoveToCurrentState || _stuckState != -1 || _currentState == -1) return;
            float goal = _statePos[_currentState];
            float wantedSpeed = _autoMoveSpeedFactor * (goal - pos);
            if (Mathf.Abs(wantedSpeed) < 0.003f) return;
            wantedSpeed = Mathf.Clamp(wantedSpeed, -_autoMoveMaxSpeed, _autoMoveMaxSpeed);

            Vector3 moveDir = _sliderJoint.HplPinDir;
            float sliderSpeed = Vector3.Dot(moveDir, _sliderBody.HplLinearVelocity);
            // cPidController<float>: the vector PID on x only gives the same output.
            float force = _autoMovePid.Output(new Vector3(wantedSpeed - sliderSpeed, 0f, 0f), timeStep).x * _sliderBody.Mass;
            _sliderBody.AddForceHpl(moveDir * force);
        }

        void ChangeState(int state, bool effects)
        {
            if (_currentState == state) return;
            _currentState = state;

            if (effects && _changeStateSound != "" && Map.sound != null && _sliderBody != null)
            {
                var sound = Map.sound.CreateSoundEntity("MultiSliderSound", _changeStateSound, true);
                if (sound != null) sound.Position = _sliderBody.Position;
            }
            if (_changeStateCallback != "") Map.RunScript(_changeStateCallback, Name, _currentState);
        }
    }
}

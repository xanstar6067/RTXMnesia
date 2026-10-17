using System;
using System.Collections.Generic;
using RTXMnesia.Dev;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTXMnesia.Player
{
    public enum HeadPosAdd { Main, Bob, ClimbLedge, Lean, Death, ScreenShake, Script, Hurt, InsanityCollapse }

    /// <summary>
    /// The player (port of cLuxPlayer + cLuxInputHandler, movement part): a <see cref="HplCharacterBody"/>
    /// with the normal move state, mouse look, lean, look-at and the head offsets that move the camera, plus the
    /// values scripts change (health, sanity, oil, FOV / aspect / roll fades).
    /// Logic runs at the HPL2 step (60 Hz): in FixedUpdate, or from the level's HplMap in the engine's order
    /// (<see cref="steppedByMap"/>). The camera is interpolated between steps and turned every frame.
    /// Settings come from game.cfg (<see cref="gameConfig"/>).
    /// Keys as in default_user_keys.cfg: WASD, Shift run, Space jump, Ctrl crouch, Q/E lean.
    /// F2 toggles a free camera, Esc opens the map's pause menu (or releases the mouse without a map).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AmnesiaPlayer : MonoBehaviour
    {
        public HplConfig gameConfig;
        public Camera playerCamera;
        [Tooltip("PlayerStart area to begin at (empty: PlayerStartArea_1 or the first one).")]
        public string startArea = "";

        [Serializable]
        public struct HeldInput
        {
            public bool forward, backward, right, left, leanLeft, leanRight;
        }

        [Tooltip("Read keyboard and mouse. Off: input comes from code (held keys, Move/Jump/Crouch/Run calls).")]
        public bool readDevices = true;
        [NonSerialized] public HeldInput held;

        [Header("User settings (defaults of cLuxInputHandler)")]
        public float mouseSensitivity = 1f;
        public bool invertMouse;
        public bool smoothMouse = true;

        [Header("Script / state multipliers")]
        [Tooltip("SetPlayerActive: off ignores all player input (cLuxInputHandler).")]
        public bool active = true;
        /// <summary>UI input lock, independent of SetPlayerActive and scripted effect pauses.</summary>
        public bool UiInputBlocked { get; private set; }
        int _uiInputFrame = -1;
        public float scriptMoveSpeedMul = 1f;
        public float scriptRunSpeedMul = 1f;
        public float eventMoveSpeedMul = 1f;
        public float eventRunSpeedMul = 1f;
        public float scriptJumpForceMul = 1f;
        public float lookSpeedMul = 1f;
        public bool jumpDisabled, crouchDisabled, noFallDamage;
        public bool showCrossHairIcons = true;
        public bool sanityDrainDisabled;
        [Tooltip("Lantern state for scripts until the lantern exists (step 4.4).")]
        public bool lanternActive, lanternDisabled;

        [Header("State")]
        public float health = 100f;
        public float sanity = 100f;
        public float lampOil = 100f;
        public int tinderboxes;

        [Tooltip("Set by HplMap: it calls StepLogic / StepPhysics in the engine's order instead of FixedUpdate.")]
        public bool steppedByMap;

        public HplCharacterBody Body { get; private set; }
        public PlayerMoveNormal MoveState { get; private set; }
        public PlayerLean Lean { get; private set; }
        public PlayerLookAt LookAt { get; private set; }
        /// <summary>Radians, Unity convention: yaw turns right, pitch looks down.</summary>
        public float Yaw => _yaw;
        public float Pitch => _pitch;
        /// <summary>Camera roll in radians, script roll plus lean roll.</summary>
        public float Roll => _roll + _leanRoll;
        public float FovMul => _fovMul;
        public float AspectMul => _aspectMul;
        public bool IsDead => health <= 0f;

        /// <summary>AddSanity with a positive amount and the effect (sanity gain flash).</summary>
        public event Action SanityGained;
        /// <summary>GiveSanityDamage (hint, hit effect).</summary>
        public event Action SanityHit;
        /// <summary>Sanity went below 0: the insanity collapse started (<see cref="InsanityCollapseHelper"/>).</summary>
        public event Action InsanityCollapse;
        /// <summary>Health reached 0 (death, step 6).</summary>
        public event Action Died;
        public Vector3 BodySize { get; private set; }
        public Vector3 BodyCrouchSize { get; private set; }
        public Vector3 CameraPosAdd { get; private set; }
        public float DefaultMass { get; private set; }
        public bool PressedMove { get; private set; }
        public bool PressingRun { get; private set; }
        public bool PressingJump { get; private set; }
        public Vector3 CameraRight => playerCamera != null ? playerCamera.transform.right : Body.Right;
        public float MoveSpeedMul => scriptMoveSpeedMul * eventMoveSpeedMul * interactionMoveSpeedMul * (Hurt != null ? Hurt.MoveSpeedMul : 1f) *
                                     (InsanityCollapseHelper != null ? InsanityCollapseHelper.SpeedMul : 1f) * (IsInWater ? WaterSpeedMul : 1f);

        // ------------------------------------------------------------------ water (cLuxArea_Liquid)

        /// <summary>Set by a liquid area while the body is in it (left set when the body leaves the area's bounds, as in HPL2).</summary>
        public bool IsInWater { get; set; }
        public float WaterSpeedMul { get; set; } = 1f;
        /// <summary>The step type of the liquid's material (step sounds while in water).</summary>
        public string WaterStepSound { get; set; } = "";
        public float RunSpeedMul => scriptRunSpeedMul * eventRunSpeedMul;

        // ------------------------------------------------------------------ interaction (step 4.3)

        [NonSerialized] public float interactionMoveSpeedMul = 1f;
        public PlayerStates States { get; private set; }
        /// <summary>The rotate key (R) is held: grabbed props turn with the mouse.</summary>
        public bool RotateHeld { get; set; }
        public ScriptApi.HplEntity EntityInFocus { get; set; }
        public Dynamics.HplPhysicsBody BodyInFocus { get; set; }
        public float FocusDistance { get; set; }
        /// <summary>The focus point, HPL2 space.</summary>
        public Vector3 FocusPos { get; set; }
        public string FocusText { get; private set; } = "";
        public string LastFocusText { get; private set; } = "";
        public float FocusTextAlpha { get; private set; }

        public void SetFocusText(string text)
        {
            if (FocusText == text) return;
            LastFocusText = FocusText;
            FocusText = text;
        }

        /// <summary>The camera of the last logic step (not interpolated).</summary>
        public Vector3 LogicCameraPosition => Body.CameraPosition;
        public Quaternion LogicCameraRotation => Quaternion.Euler(_pitch * Mathf.Rad2Deg, _yaw * Mathf.Rad2Deg, Roll * Mathf.Rad2Deg);

        // HPL2-space camera, as cCamera gives it to the interaction states.
        public Vector3 HplCameraPosition => HplSpace.Position(LogicCameraPosition);
        public Vector3 HplCameraForward => HplSpace.Direction(LogicCameraRotation * Vector3.forward);
        public Vector3 HplCameraUp => HplSpace.Direction(LogicCameraRotation * Vector3.up);
        public Vector3 HplCameraRight => HplSpace.Direction(LogicCameraRotation * Vector3.right);
        /// <summary>(pitch, yaw, roll) of the HPL2 camera: pitch = -pitch, yaw = π - yaw, roll the same.</summary>
        public Vector3 HplCameraAngles => new(-_pitch, Mathf.PI - _yaw, Roll);

        // View limits of the push state (cCamera pitch / yaw limits), in Unity angles.
        float _savedPitchMin, _savedPitchMax;
        float _pitchMin = -70f * Mathf.Deg2Rad, _pitchMax = 70f * Mathf.Deg2Rad;
        bool _yawLimited;
        float _yawMin, _yawMax;

        /// <summary>cLuxPlayerState_InteractPush: pitch within [pitch - min, pitch + max] (HPL2 sense), yaw ± range.</summary>
        public void PushViewLimits(float pitchMinRange, float pitchMaxRange, float yawRange)
        {
            _savedPitchMin = _pitchMin;
            _savedPitchMax = _pitchMax;
            // HPL2 pitch is minus Unity pitch: its [p - min, p + max] is [p - max, p + min] here.
            _pitchMin = _pitch - pitchMaxRange;
            _pitchMax = _pitch + pitchMinRange;
            _yawLimited = true;
            _yawMin = _yaw - yawRange;
            _yawMax = _yaw + yawRange;
        }

        public void PopViewLimits()
        {
            _pitchMin = _savedPitchMin;
            _pitchMax = _savedPitchMax;
            _yawLimited = false;
        }

        // ------------------------------------------------------------------ map change

        /// <summary>
        /// What a map change keeps: HPL2 has one cLuxPlayer for the whole game, here every map scene has its own.
        /// cLuxPlayer::OnMapLeave resets the FOV / aspect / roll fades and the state (Normal); the script settings,
        /// health, sanity, oil, lantern, crouching and head offsets stay.
        /// </summary>
        [Serializable]
        public sealed class Carry
        {
            public float moveSpeedMul, runSpeedMul, jumpForceMul, lookSpeedMul;
            public bool jumpDisabled, crouchDisabled, noFallDamage, showCrossHairIcons, sanityDrainDisabled;
            public bool lanternActive, lanternDisabled, crouching;
            public float health, sanity, lampOil;
            public int tinderboxes;
            public HeadOffset[] headOffsets;
            // cLuxPlayerHands: the held object, its state and fade.
            public string handObject;
            public PlayerHands.HandsState handState;
            public float handAlpha;
            // cLuxPlayerDeath: the death hint stays for the whole game.
            public string deathHintCat, deathHintEntry;
            public string permaDeathSound;
        }

        public Carry CaptureCarry() => new()
        {
            moveSpeedMul = scriptMoveSpeedMul, runSpeedMul = scriptRunSpeedMul, jumpForceMul = scriptJumpForceMul,
            lookSpeedMul = lookSpeedMul, jumpDisabled = jumpDisabled, crouchDisabled = crouchDisabled,
            noFallDamage = noFallDamage, showCrossHairIcons = showCrossHairIcons, sanityDrainDisabled = sanityDrainDisabled,
            lanternActive = lanternActive, lanternDisabled = lanternDisabled,
            crouching = MoveState != null && MoveState.Crouching,
            health = health, sanity = sanity, lampOil = lampOil, tinderboxes = tinderboxes,
            headOffsets = (HeadOffset[])_headOffsets.Clone(),
            handObject = Hands != null ? Hands.CurrentHandObject : "",
            handState = Hands != null ? Hands.State : PlayerHands.HandsState.Disabled,
            handAlpha = Hands != null ? Hands.HandObjectAlpha : 0f,
            deathHintCat = Death != null ? Death.HintCat : "", deathHintEntry = Death != null ? Death.HintEntry : "",
            permaDeathSound = Death != null ? Death.PermaDeathSound : "",
        };

        public void ApplyCarry(Carry c)
        {
            scriptMoveSpeedMul = c.moveSpeedMul;
            scriptRunSpeedMul = c.runSpeedMul;
            scriptJumpForceMul = c.jumpForceMul;
            lookSpeedMul = c.lookSpeedMul;
            jumpDisabled = c.jumpDisabled;
            crouchDisabled = c.crouchDisabled;
            noFallDamage = c.noFallDamage;
            showCrossHairIcons = c.showCrossHairIcons;
            sanityDrainDisabled = c.sanityDrainDisabled;
            lanternActive = c.lanternActive;
            lanternDisabled = c.lanternDisabled;
            health = c.health;
            sanity = c.sanity;
            lampOil = c.lampOil;
            tinderboxes = c.tinderboxes;
            Array.Copy(c.headOffsets, _headOffsets, Math.Min(c.headOffsets.Length, _headOffsets.Length));
            if (c.crouching && MoveState != null) MoveState.RestoreCrouch();
            Hands?.Restore(c.handObject, c.handState, c.handAlpha);
            Death?.SetHint(c.deathHintCat, c.deathHintEntry);
            if (Death != null) Death.PermaDeathSound = c.permaDeathSound ?? "";
        }

        // cLuxPlayer_SaveData beyond the carry: the event speeds, the script's FOV / aspect / roll fades, terror, water.
        static readonly string[] SavedFields =
        {
            nameof(eventMoveSpeedMul), nameof(eventRunSpeedMul),
            nameof(_fovMul), nameof(_fovMulGoal), nameof(_fovMulSpeed), nameof(_aspectMul), nameof(_aspectMulGoal), nameof(_aspectMulSpeed),
            nameof(_roll), nameof(_rollGoal), nameof(_rollSpeedMul), nameof(_rollMaxSpeed),
            "<Terror>k__BackingField", "<IsInWater>k__BackingField", "<WaterSpeedMul>k__BackingField", "<WaterStepSound>k__BackingField",
        };

        /// <summary>cLuxPlayer_SaveData::FromPlayer.</summary>
        public Save.PlayerSave CaptureSave() => new()
        {
            carry = CaptureCarry(),
            feet = Body.FeetPosition,
            yaw = _yaw,
            pitch = _pitch,
            active = active,
            fields = Save.HplFieldBag.CaptureNamed(this, SavedFields),
        };

        /// <summary>cLuxPlayer_SaveData::ToPlayer: the state is Normal (the original's TODO for the move mode too).</summary>
        public void ApplySave(Save.PlayerSave save)
        {
            ApplyCarry(save.carry);
            active = save.active;
            PlaceAt(save.feet, save.yaw);
            _pitch = Mathf.Clamp(save.pitch, _pitchMin, _pitchMax);
            Save.HplFieldBag.RestoreNamed(this, save.fields);
            UpdateCameraTransform(1f);
        }

        /// <summary>cLuxPlayer::DoAction: the state first; if it lets the action through, the lantern key toggles the lantern.</summary>
        public void DoAction(PlayerAction action, bool pressed)
        {
            if (States == null) return;
            if (States.Current.OnDoAction(action, pressed) && action == PlayerAction.Lantern && pressed)
                Lantern?.SetActive(!lanternActive, true);
        }

        public void Scroll(float amount) => States?.Current.OnScroll(amount);

        /// <summary>cLuxPlayer::PostUpdate (after the world update): the state's post update (grab forces).</summary>
        public void StepPostUpdate(float dt)
        {
            if (_freeCameraActive) return;
            States?.Current.PostUpdate(dt);
            Hands?.PostUpdate();
        }

        /// <summary>Step sound request: type and walk/run/sneak (sounds come with the sound system).</summary>
        public event Action<FootSound, string> FootSounded;

        [Serializable]
        public struct HeadOffset
        {
            public bool moving;
            public Vector3 add, goal;
            public float speed, slowDownDist;
        }

        readonly HeadOffset[] _headOffsets = new HeadOffset[Enum.GetValues(typeof(HeadPosAdd)).Length];

        float _yaw, _pitch; // radians, Unity convention: yaw turns right, pitch looks down
        float _fov;
        float _fovMul = 1f, _fovMulGoal = 1f, _fovMulSpeed;
        float _aspectMul = 1f, _aspectMulGoal = 1f, _aspectMulSpeed;
        float _roll, _rollGoal, _rollSpeedMul, _rollMaxSpeed;
        float _leanRoll, _leanRollGoal, _leanRollSpeedMul, _leanRollMaxSpeed;

        int _maxSmoothMousePos = 7;
        float _prevSmoothMousePosMul = 0.7f;
        readonly List<(float time, Vector2 perStep)> _mouseSamples = new();

        FlyCamera _freeCamera;
        bool _freeCameraActive;

        const float HplStep = 1f / 60f;

        void Awake()
        {
            if (gameConfig == null)
            {
                Debug.LogError("[RTXMnesia] AmnesiaPlayer has no game config (run Tools > RTXMnesia > Import > Level)");
                enabled = false;
                return;
            }
            var cfg = gameConfig;

            BodySize = cfg.GetVector3("Player_Body", "Size", new Vector3(0.8f, 1.8f, 0.8f));
            BodyCrouchSize = cfg.GetVector3("Player_Body", "CrouchSize", new Vector3(0.8f, 1.2f, 0.8f));
            CameraPosAdd = cfg.GetVector3("Player_Body", "CameraPosAdd");
            DefaultMass = cfg.GetFloat("Player_Body", "Mass", 70f);

            // cLuxPlayer::CreateCharacterBody
            if (!TryGetComponent(out HplCharacterBody body)) body = gameObject.AddComponent<HplCharacterBody>();
            Body = body;
            Body.Init(BodySize);
            Body.gravity = cfg.GetVector3("Player_Body", "GravityForce", new Vector3(0f, -9.8f, 0f));
            Body.mass = DefaultMass;
            Body.accurateClimbing = cfg.GetBool("Player_Body", "AccurateClimbing");
            Body.maxNoSlideSlopeAngle = cfg.GetFloat("Player_Body", "MaxNoSlideSlopeAngle", 45f);
            Body.maxPushMass = cfg.GetFloat("Player_Body", "MaxPushMass");
            Body.pushForce = cfg.GetFloat("Player_Body", "PushForce");
            Body.maxStepHeight = cfg.GetFloat("Player_Body", "MaxStepSize");
            Body.maxStepHeightInAir = cfg.GetFloat("Player_Body", "MaxStepSizeInAir");
            Body.stepClimbSpeed = cfg.GetFloat("Player_Body", "StepClimbSpeed");
            Body.cameraPosAdd = CameraPosAdd;
            Body.cameraSmoothPosNum = cfg.GetInt("Player_Body", "CameraSmoothPosNum");
            Body.AddExtraSize(BodyCrouchSize);

            _maxSmoothMousePos = cfg.GetInt("Input", "MaxSmoothMousePos", 7);
            _prevSmoothMousePosMul = cfg.GetFloat("Input", "PrevSmoothMousePosMul", 0.7f);

            _fov = cfg.GetFloat("Player_General", "FOV", 70f);
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = _fov;
                playerCamera.nearClipPlane = cfg.GetFloat("Player_General", "NearClipPlane", 0.05f);
                playerCamera.farClipPlane = cfg.GetFloat("Player_General", "FarClipPlane", 400f);
                _freeCamera = playerCamera.GetComponent<FlyCamera>();
                if (_freeCamera != null) _freeCamera.enabled = false;
            }

            MoveState = new PlayerMoveNormal(this, cfg);
            Lean = new PlayerLean(this);
            LookAt = new PlayerLookAt(this);
            MoveState.OnEnterState();
            States = new PlayerStates(this);
            Lantern = new PlayerLantern(this, cfg);
            Flashback = new PlayerFlashback(this, cfg);
            LightLevel = new PlayerLightLevel(this, cfg);
            InDarkness = new PlayerInDarkness(this, cfg);
            SanityHelper = new PlayerSanity(this, cfg);
            CamDirEffects = new PlayerCamDirEffects(this, cfg);
            InsanityCollapseHelper = new PlayerInsanityCollapse(this, cfg);
            HudEffect = new PlayerHudEffect();
            Hurt = new PlayerHurt(this, cfg);
            Death = new PlayerDeath(this, cfg);
            _headSpinDamageSpeed = cfg.GetFloat("Player_General", "HeadSpinDamageSpeed", 0f);
            _headSpinDeacc = cfg.GetFloat("Player_General", "HeadSpinDeacc", 0f);
            _autoKillYPos = cfg.GetFloat("Player_General", "AutoKillYPos", 0f);
            _terrorSoundName = cfg.GetString("Player_General", "TerrorSound", "");
            _terrorIncSpeed = cfg.GetFloat("Player_General", "TerrorIncSpeed", 0f);
            _terrorDecSpeed = cfg.GetFloat("Player_General", "TerrorDecSpeed", 0f);
            _maxPrevSpeeds = cfg.GetInt("Player_General", "MaxPrevSpeedsSaved", 0);
            // The hands are a world entity of Play mode (edit-mode simulations have no animation clock).
            if (Application.isPlaying) Hands = PlayerHands.Create(this, cfg);
        }

        /// <summary>cLuxPlayerHands: the hands and hand objects (null if not imported or outside Play mode).</summary>
        public PlayerHands Hands { get; private set; }
        /// <summary>cLuxPlayerLantern.</summary>
        public PlayerLantern Lantern { get; private set; }
        /// <summary>cLuxPlayerFlashback.</summary>
        public PlayerFlashback Flashback { get; private set; }
        /// <summary>cLuxPlayerLightLevel.</summary>
        public PlayerLightLevel LightLevel { get; private set; }
        /// <summary>cLuxPlayerInDarkness.</summary>
        public PlayerInDarkness InDarkness { get; private set; }
        /// <summary>cLuxPlayerSanity (hit, losing sanity, insanity visuals, low sanity, regain).</summary>
        public PlayerSanity SanityHelper { get; private set; }
        /// <summary>cLuxPlayerCamDirEffects (view sway at very low sanity).</summary>
        public PlayerCamDirEffects CamDirEffects { get; private set; }
        /// <summary>cLuxPlayerInsanityCollapse.</summary>
        public PlayerInsanityCollapse InsanityCollapseHelper { get; private set; }
        /// <summary>cLuxPlayerHudEffect (damage splashes, HUD flash).</summary>
        public PlayerHudEffect HudEffect { get; private set; }
        /// <summary>cLuxPlayerHurt (health regain, hurt noise, panting, slower movement).</summary>
        public PlayerHurt Hurt { get; private set; }
        /// <summary>cLuxPlayerDeath.</summary>
        public PlayerDeath Death { get; private set; }

        void Start()
        {
            // cLuxPlayer::OnMapEnter / CreateWorldEntities of the helpers: each map scene has its own player.
            InDarkness.CreateWorldEntities(transform);
            LightLevel.OnMapEnter();
            InDarkness.OnMapEnter();
            var start = FindStart(startArea);
            if (start != null) PlaceAtStart(start);
            else PlaceAt(transform.position, transform.eulerAngles.y * Mathf.Deg2Rad);
            if (!UiInputBlocked) LockCursor(true);
        }

        void OnDisable() => LockCursor(false);

        // ------------------------------------------------------------------ placement

        static AmnesiaArea FindStart(string name)
        {
            AmnesiaArea first = null, numberOne = null;
            foreach (var area in FindObjectsByType<AmnesiaArea>(FindObjectsInactive.Exclude))
            {
                if (area.areaType != "PlayerStart") continue;
                if (!string.IsNullOrEmpty(name) && area.name == name) return area;
                if (area.name == "PlayerStartArea_1") numberOne = area;
                first ??= area;
            }
            return numberOne ?? first;
        }

        /// <summary>cLuxPlayer::PlaceAtStartNode: the player looks along the area's +Z (HPL2 and Unity alike).</summary>
        /// <summary>Set the view (Unity angles: yaw from +Z towards +X, pitch down), as the ladder state turns the camera.</summary>
        public void SetLook(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = pitch;
            Body.Yaw = _yaw;
        }

        public void PlaceAtStart(AmnesiaArea area)
        {
            // cLuxPlayer::PlaceAtStartNode: reset, or the player splashes on the ground.
            IsInWater = false;
            Vector3 f = area.transform.forward;
            PlaceAt(area.transform.position + new Vector3(0f, 0.05f, 0f), Mathf.Atan2(f.x, f.z));
        }

        public void PlaceAt(Vector3 feet, float yaw)
        {
            Body.StopMovement();
            Body.SetFeetPosition(feet);
            _yaw = yaw;
            _pitch = 0f;
            Body.Yaw = _yaw;
            Body.Step(0.001f);
            Body.SetFeetPosition(Body.FeetPosition);
            UpdateCameraTransform(1f);
        }

        // ------------------------------------------------------------------ update

        void Update()
        {
            if (!readDevices) return;
            // Ignore the opening/closing click and key for this entire frame, including mouse look.
            if (UiInputBlocked || Time.frameCount == _uiInputFrame) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame) SetFreeCamera(!_freeCameraActive);
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame &&
                (ScriptApi.HplMap.Current == null || ScriptApi.HplMap.Current.GameUi == null)) LockCursor(false);
            bool cursorWasLocked = Cursor.lockState == CursorLockMode.Locked;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !cursorWasLocked) LockCursor(true);
            if (_freeCameraActive) return;

            // cLuxInputHandler::UpdateGamePlayerInput: a dead player's attack / interact / ignite / jump / crouch
            // keys only skip the death text; an inactive player gets no input at all.
            // Inputs are the configured ones (HplKeyBindings: default_user_keys.cfg / the key config screen); a mouse
            // button press does not count when it was the click that locked the mouse.
            bool Pressed(string action) => Ui.HplKeyBindings.WasPressed(action) && (cursorWasLocked || !MouseBound(action));
            if (IsDead)
            {
                held = default;
                bool pressed = Pressed("Jump") || Pressed("Crouch") || Pressed("Interact") || Pressed("Attack");
                if (pressed) Death?.OnPressButton();
                return;
            }
            if (!active)
            {
                held = default;
                return;
            }

            // Held inputs are applied in the next fixed step; presses are actions now (as cLuxInputHandler).
            held.forward = Ui.HplKeyBindings.IsHeld("Forward");
            held.backward = Ui.HplKeyBindings.IsHeld("Backward");
            held.right = Ui.HplKeyBindings.IsHeld("Right");
            held.left = Ui.HplKeyBindings.IsHeld("Left");
            held.leanRight = Ui.HplKeyBindings.IsHeld("LeanRight");
            held.leanLeft = Ui.HplKeyBindings.IsHeld("LeanLeft");
            PressedMove |= held.forward || held.backward || held.right || held.left;

            bool run = Ui.HplKeyBindings.IsHeld("Run");
            if (run != PressingRun) Run(run);
            if (Pressed("Jump")) Jump(true);
            if (!Ui.HplKeyBindings.IsHeld("Jump") && PressingJump) Jump(false);
            if (Pressed("Crouch")) Crouch(true);
            if (Pressed("Lantern")) DoAction(PlayerAction.Lantern, true);
            if (Ui.HplKeyBindings.WasReleased("Lantern")) DoAction(PlayerAction.Lantern, false);
            RotateHeld = Ui.HplKeyBindings.IsHeld("Rotate");
            // cLuxInputHandler: CrosshairToggle → cLuxPlayer::SetShowCrosshair(!GetShowCrosshair()).
            if (Pressed("CrosshairToggle")) ScriptApi.HplMap.Current?.GameUi?.ToggleCrosshair();

            // Interact, attack / throw; the scroll wheel moves a held object (grab depth).
            if (Pressed("Interact")) DoAction(PlayerAction.Interact, true);
            if (Ui.HplKeyBindings.WasReleased("Interact")) DoAction(PlayerAction.Interact, false);
            if (Pressed("Attack")) DoAction(PlayerAction.Attack, true);
            if (Ui.HplKeyBindings.WasReleased("Attack")) DoAction(PlayerAction.Attack, false);

            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                if (cursorWasLocked)
                {
                    float scroll = mouse.scroll.ReadValue().y;
                    if (scroll > 0f) Scroll(1f);
                    if (scroll < 0f) Scroll(-1f);
                }
                UpdateMouseLook(mouse.delta.ReadValue(), Time.unscaledDeltaTime);
            }
        }

        static bool MouseBound(string action)
        {
            var a = Ui.HplKeyBindings.Get(action);
            return a != null && (a.primary.StartsWith("MouseButton.", System.StringComparison.Ordinal) ||
                                 a.secondary.StartsWith("MouseButton.", System.StringComparison.Ordinal));
        }

        public void SetUiInputBlocked(bool blocked)
        {
            UiInputBlocked = blocked;
            _uiInputFrame = Time.frameCount;
            held = default;
            RotateHeld = false;
            PressedMove = false;
            _mouseSamples.Clear();
            if (MoveState == null) return;
            Run(false);
            Jump(false);
            // Releases made over a menu must not leave a door / grab interaction stuck after resuming.
            if (!blocked && readDevices)
            {
                if (!Ui.HplKeyBindings.IsHeld("Interact")) DoAction(PlayerAction.Interact, false);
                if (!Ui.HplKeyBindings.IsHeld("Attack")) DoAction(PlayerAction.Attack, false);
            }
        }


        void FixedUpdate()
        {
            if (steppedByMap) return;
            StepLogic(Time.fixedDeltaTime);
            StepPhysics(Time.fixedDeltaTime);
            StepPostUpdate(Time.fixedDeltaTime);
        }

        /// <summary>
        /// cLuxPlayer::Update: input of this step, move state, head offsets, helpers (lean, look-at), the collide
        /// callbacks (<paramref name="collideCallbacks"/>, from the map), camera effects. The character body moves
        /// in <see cref="StepPhysics"/> (the world update, after all modules).
        /// </summary>
        public void StepLogic(float dt, Action collideCallbacks = null)
        {
            if (_freeCameraActive) return;

            if (active)
            {
                if (held.forward) Move(CharDir.Forward, 1f);
                if (held.backward) Move(CharDir.Forward, -1f);
                if (held.right) Move(CharDir.Right, 1f);
                if (held.left) Move(CharDir.Right, -1f);
                if (held.leanRight) Lean.SetLean(1f);
                if (held.leanLeft) Lean.SetLean(-1f);
            }

            MoveState.Update(dt);
            States.Current.Update(dt);
            UpdateHeadPosAdd(dt);
            UpdateHeadSpin(dt);
            // Helpers in cLuxPlayer's order (HudEffect, LightLevel, InDarkness, Lean, Death, Hands, Lantern, Sanity,
            // LookAt, Flashback, Hurt, SpawnPS, CamDirEffects, InsanityCollapse).
            HudEffect.Update(dt);
            LightLevel.Update(dt);
            InDarkness.Update(dt);
            Lean.Update(dt);
            Death.Update(dt);
            Hands?.Update(dt);
            Lantern.Update(dt);
            SanityHelper.Update(dt);
            LookAt.Update(dt);
            Flashback.Update(dt);
            Hurt.Update(dt);
            CamDirEffects.Update(dt);
            InsanityCollapseHelper.Update(dt);
            PressedMove = false;
            // Too far down (fell out of the map): dead.
            if (Body.Position.y < _autoKillYPos) SetHealth(0f);
            collideCallbacks?.Invoke();
            UpdateCameraEffects(dt);
            UpdateTerror(dt);
            UpdateFocusText(dt);
            UpdateAvgSpeed(dt);
        }

        // ------------------------------------------------------------------ terror (cLuxPlayer)

        readonly HashSet<Enemies.HplEnemy> _terrorEnemies = new();
        readonly List<float> _prevSpeeds = new();
        RTXMnesia.Sound.SoundEntry _terrorSound;
        int _terrorSoundId = -1;

        /// <summary>cLuxPlayer::mfTerror: rises while an enemy hunts or is alert (TerrorIncSpeed), falls otherwise.</summary>
        public float Terror { get; private set; }
        /// <summary>cLuxPlayer::GetAvgSpeed: the average speed of the last MaxPrevSpeedsSaved steps.</summary>
        public float AvgSpeed { get; private set; }

        public void SetTerror(float x) => Terror = x;
        public void AddTerrorEnemy(Enemies.HplEnemy enemy) => _terrorEnemies.Add(enemy);
        public void RemoveTerrorEnemy(Enemies.HplEnemy enemy) => _terrorEnemies.Remove(enemy);
        public void ClearTerrorEnemies() => _terrorEnemies.Clear();
        public int TerrorEnemyCount => _terrorEnemies.Count;

        /// <summary>cLuxPlayer::StopTerrorSound (inventory and journal open).</summary>
        public void StopTerrorSound()
        {
            if (_terrorSound != null && sound != null && sound.IsValid(_terrorSound, _terrorSoundId)) _terrorSound.Stop();
            _terrorSound = null;
            _terrorSoundId = -1;
        }

        /// <summary>cLuxPlayer::UpdateTerror: the terror and its looping GUI sound at volume = terror.</summary>
        void UpdateTerror(float dt)
        {
            if (_terrorEnemies.Count == 0 || IsDead) Terror = Mathf.Max(0f, Terror - _terrorDecSpeed * dt);
            else Terror = Mathf.Min(1f, Terror + _terrorIncSpeed * dt);

            if (sound == null) return;
            if (Terror > 0f && _terrorSound == null)
            {
                _terrorSound = sound.PlayGui(_terrorSoundName, true, 1f);
                if (_terrorSound != null)
                {
                    _terrorSound.SetVolumeMul(0f);
                    _terrorSoundId = _terrorSound.Id;
                }
            }
            if (_terrorSound != null && sound.IsValid(_terrorSound, _terrorSoundId))
            {
                _terrorSound.SetVolumeMul(Terror);
                if (Terror <= 0f)
                {
                    _terrorSound.Stop();
                    _terrorSound = null;
                    _terrorSoundId = -1;
                }
            }
        }

        void UpdateAvgSpeed(float dt)
        {
            _prevSpeeds.Add(Body.GetVelocity(dt).magnitude);
            if (_prevSpeeds.Count > _maxPrevSpeeds) _prevSpeeds.RemoveAt(0);
            float sum = 0f;
            foreach (float s in _prevSpeeds) sum += s;
            AvgSpeed = sum / _prevSpeeds.Count;
        }

        /// <summary>cLuxPlayer::UpdateFocusText: the focus text fades in and out at 2 per second.</summary>
        void UpdateFocusText(float dt)
        {
            if (FocusText == "") FocusTextAlpha = Mathf.Max(0f, FocusTextAlpha - dt * 2f);
            else FocusTextAlpha = Mathf.Min(1f, FocusTextAlpha + dt * 2f);
        }

        public void StepPhysics(float dt)
        {
            if (_freeCameraActive) return;
            Body.Step(dt);
        }

        void LateUpdate()
        {
            if (_freeCameraActive || UiInputBlocked) return;
            float alpha = Time.fixedDeltaTime > 0f ? Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime) : 1f;
            UpdateCameraTransform(alpha);
            Hands?.UpdateTransform(playerCamera != null ? playerCamera.transform : null);
        }

        /// <summary>Turn the view (radians; yaw turns right, pitch looks down), within the pitch limits.</summary>
        public void AddLook(float yaw, float pitch)
        {
            _yaw += yaw;
            _pitch = Mathf.Clamp(_pitch + pitch, _pitchMin, _pitchMax);
            Body.Yaw = _yaw;
        }

        /// <summary>Put the camera at the latest step (no interpolation).</summary>
        public void SnapCamera() => UpdateCameraTransform(1f);

        void UpdateCameraTransform(float alpha)
        {
            if (playerCamera == null) return;
            playerCamera.transform.SetPositionAndRotation(
                Body.InterpolatedCameraPosition(alpha),
                Quaternion.Euler(_pitch * Mathf.Rad2Deg, _yaw * Mathf.Rad2Deg, (_roll + _leanRoll) * Mathf.Rad2Deg));
            // FOV is vertical in both engines; the aspect multiplier squeezes the view (FadePlayerAspectMulTo).
            playerCamera.fieldOfView = _fov * _fovMul;
            if (_aspectMul == 1f) playerCamera.ResetAspect();
            else playerCamera.aspect = (float)playerCamera.pixelWidth / Mathf.Max(1, playerCamera.pixelHeight) * _aspectMul;
        }

        // ------------------------------------------------------------------ input actions

        public void Move(CharDir dir, float mul)
        {
            PressedMove = true;
            if (States != null && !States.Current.OnMove(dir, mul)) return;
            Body.Move(dir, mul);
        }

        public void Run(bool pressed)
        {
            PressingRun = pressed;
            if (States != null && !States.Current.OnRun(pressed)) return;
            MoveState.OnRun(pressed);
        }

        public void Jump(bool pressed)
        {
            if (pressed && jumpDisabled) return;
            PressingJump = pressed;
            if (States != null && !States.Current.OnJump(pressed)) return;
            MoveState.OnJump(pressed);
        }

        public void Crouch(bool pressed)
        {
            if (pressed && crouchDisabled) return;
            if (States != null && !States.Current.OnCrouch(pressed)) return;
            MoveState.OnCrouch(pressed);
        }

        /// <summary>
        /// cLuxPlayer::AddYaw with an HPL2 amount (positive turns left): the state may take it (rotating a held
        /// prop, turning a door), else the camera turns by 2 × amount, within the yaw limits if set.
        /// </summary>
        public void AddYaw(float amount)
        {
            amount *= lookSpeedMul;
            if (States != null && !States.Current.OnAddYaw(amount)) return;
            _yaw -= amount * 2f;
            if (_yawLimited) _yaw = Mathf.Clamp(_yaw, _yawMin, _yawMax);
            Body.Yaw = _yaw;
        }

        /// <summary>cLuxPlayer::AddPitch with an HPL2 amount (positive looks up).</summary>
        public void AddPitch(float amount)
        {
            amount *= lookSpeedMul;
            if (States != null && !States.Current.OnAddPitch(amount)) return;
            _pitch = Mathf.Clamp(_pitch - amount * 2f, _pitchMin, _pitchMax);
        }

        /// <summary>
        /// cLuxInputHandler: pixels × sensitivity / (1.7 × screen height), smoothed, then AddYaw(-x) / AddPitch(y)
        /// (screen y grows upwards here, downwards in HPL2). The smoothing weights samples by 0.7^(age in 60 Hz
        /// steps) over 7 steps, as the original per engine step, so it behaves the same at any frame rate.
        /// </summary>
        void UpdateMouseLook(Vector2 pixels, float frameTime)
        {
            Vector2 rel = pixels * mouseSensitivity / (1.7f * Mathf.Max(1, Screen.height));
            Vector2 final = smoothMouse ? SmoothMouse(rel, frameTime) : rel;
            if (invertMouse) final.y = -final.y;
            // cLuxPlayerCamDirEffects::AddAndGetYawAdd / AddAndGetPitchAdd (view sway at very low sanity).
            Vector2 look = CamDirEffects != null ? CamDirEffects.AddAndGetLookAdd(new Vector2(-final.x, final.y), frameTime * 60f) : new Vector2(-final.x, final.y);
            AddYaw(look.x);
            AddPitch(look.y);
        }

        Vector2 SmoothMouse(Vector2 rel, float frameTime)
        {
            float steps = Mathf.Max(frameTime, 1e-4f) / HplStep;
            float now = Time.unscaledTime;
            _mouseSamples.Insert(0, (now, rel / steps));
            float window = _maxSmoothMousePos * HplStep;
            while (_mouseSamples.Count > 1 && now - _mouseSamples[^1].time >= window) _mouseSamples.RemoveAt(_mouseSamples.Count - 1);

            Vector2 sum = Vector2.zero;
            float weightSum = 0f;
            foreach (var (time, perStep) in _mouseSamples)
            {
                float weight = Mathf.Pow(_prevSmoothMousePosMul, (now - time) / HplStep);
                sum += perStep * weight;
                weightSum += weight;
            }
            return sum / weightSum * steps;
        }

        // ------------------------------------------------------------------ head and camera

        public void MoveHeadPosAdd(HeadPosAdd type, Vector3 goal, float speed, float slowDownDist)
        {
            ref var h = ref _headOffsets[(int)type];
            h.moving = true;
            h.goal = goal;
            h.speed = speed;
            h.slowDownDist = slowDownDist;
        }

        public void SetHeadPosAdd(HeadPosAdd type, Vector3 add) => _headOffsets[(int)type].add = add;

        public Vector3 GetHeadPosAdd(HeadPosAdd type) => _headOffsets[(int)type].add;

        void UpdateHeadPosAdd(float dt)
        {
            Vector3 sum = CameraPosAdd;
            for (int i = 0; i < _headOffsets.Length; i++)
            {
                ref var h = ref _headOffsets[i];
                if (h.moving)
                {
                    Vector3 dir = h.goal - h.add;
                    float dist = dir.magnitude;
                    if (dist < 0.001f)
                    {
                        h.moving = false;
                        h.add = h.goal;
                    }
                    else
                    {
                        float speed = h.speed;
                        if (dist < h.slowDownDist) speed *= dist / h.slowDownDist;
                        h.add += dir / dist * (speed * dt);
                    }
                }
                sum += h.add;
            }
            Body.cameraPosAdd = sum;
        }

        /// <summary>Roll in radians (HPL2 and Unity camera roll have the same visual sense).</summary>
        public void FadeRollTo(float roll, float speedMul, float maxSpeed)
        {
            _rollGoal = roll;
            _rollSpeedMul = speedMul;
            _rollMaxSpeed = maxSpeed;
        }

        public void SetRoll(float roll) => _roll = _rollGoal = roll;

        public void FadeFOVMulTo(float mul, float speed)
        {
            _fovMulGoal = mul;
            _fovMulSpeed = speed;
        }

        public void FadeAspectMulTo(float mul, float speed)
        {
            _aspectMulGoal = mul;
            _aspectMulSpeed = speed;
        }

        public void FadeLeanRollTo(float roll, float speedMul, float maxSpeed)
        {
            _leanRollGoal = roll;
            _leanRollSpeedMul = speedMul;
            _leanRollMaxSpeed = maxSpeed;
        }

        // cLuxPlayer::UpdateCamera: FOV and aspect multipliers fade proportionally (no speed limit), roll and
        // lean roll with a speed limit.
        void UpdateCameraEffects(float dt)
        {
            FadeMul(ref _fovMul, _fovMulGoal, _fovMulSpeed, dt);
            FadeMul(ref _aspectMul, _aspectMulGoal, _aspectMulSpeed, dt);
            Fade(ref _roll, _rollGoal, _rollSpeedMul, _rollMaxSpeed, dt);
            Fade(ref _leanRoll, _leanRollGoal, _leanRollSpeedMul, _leanRollMaxSpeed, dt);
        }

        static void FadeMul(ref float value, float goal, float speedMul, float dt)
        {
            if (value == goal) return;
            value += dt * (goal - value) * speedMul;
            if (Mathf.Abs(goal - value) < 0.004f) value = goal;
        }

        static void Fade(ref float value, float goal, float speedMul, float maxSpeed, float dt)
        {
            if (value == goal) return;
            float speed = Mathf.Clamp((goal - value) * speedMul, -maxSpeed, maxSpeed);
            value += dt * speed;
            if (Mathf.Abs(goal - value) < 0.004f) value = goal;
        }

        // ------------------------------------------------------------------ misc

        /// <summary>
        /// cLuxPlayer::GiveDamage: damage splash, red HUD flash, head spin; a non lethal hit leaves at least 10 health.
        /// The strength is only used by the enemies' attacks (step 6).
        /// </summary>
        public void GiveDamage(float amount, int strength, DamageType type, bool spinHead, bool lethal)
        {
            if (health <= 0f) return;
            health -= amount;
            HudEffect.AddDamageSplash(type);
            HudEffect.Flash(new Color(0.6f, 0f, 0f, 0.5f), 0f, 0.25f);
            if (spinHead) SpinHead(_headSpinDamageSpeed);
            if (!lethal && health < 10f) health = 10f;
            if (health <= 0f) StartDeath();
        }

        public void SetHealth(float x)
        {
            if (health <= 0f && x <= 0f) return;
            health = x;
            if (health <= 0f) StartDeath();
        }

        public void AddHealth(float x)
        {
            if ((health >= 100f && x > 0f) || health < 0f) return;
            health = Mathf.Min(100f, health + x);
            if (health <= 0f) StartDeath();
        }

        void StartDeath()
        {
            Death.Start();
            Died?.Invoke();
        }

        float _headSpinDamageSpeed, _headSpinDeacc, _autoKillYPos;
        string _terrorSoundName = "";
        float _terrorIncSpeed, _terrorDecSpeed;
        int _maxPrevSpeeds;
        Vector2 _headSpinSpeed; // HPL2 (yaw, pitch) per second

        /// <summary>cLuxPlayer::SpinHead: a random direction, mostly sideways and up.</summary>
        public void SpinHead(float speed)
        {
            _headSpinSpeed = new Vector2(HplMath.RandRectf(-1f, 1f), HplMath.RandRectf(0f, 0.5f));
            if (_headSpinSpeed.x == 0f && _headSpinSpeed.y == 0f) _headSpinSpeed.x = 1f;
            else _headSpinSpeed.Normalize();
            _headSpinSpeed *= speed;
        }

        /// <summary>cLuxPlayer::UpdateHeadSpin: turns the camera directly (HPL2 angles), slowing by HeadSpinDeacc.</summary>
        void UpdateHeadSpin(float dt)
        {
            if (_headSpinSpeed == Vector2.zero) return;
            // HPL2 pitch / yaw are minus the Unity ones; the camera limits apply.
            _pitch = Mathf.Clamp(_pitch - _headSpinSpeed.y * dt, _pitchMin, _pitchMax);
            _yaw -= _headSpinSpeed.x * dt;
            if (_yawLimited) _yaw = Mathf.Clamp(_yaw, _yawMin, _yawMax);
            Body.Yaw = _yaw;
            float length = Mathf.Max(0f, _headSpinSpeed.magnitude - dt * _headSpinDeacc);
            _headSpinSpeed = _headSpinSpeed.normalized * length;
        }

        /// <summary>
        /// cLuxPlayer::SetCurrentHandObjectDrawn: draws / holsters the equipped hand object item. Only items of type
        /// HandObject are equipped (the lantern is its own type) and the game has none, so this does nothing.
        /// </summary>
        public void SetCurrentHandObjectDrawn(bool drawn) { }

        bool SanityDisabled => ScriptApi.HplMap.Current != null && ScriptApi.HplMap.Current.disableInsanity;

        public void SetSanity(float x) => sanity = SanityDisabled ? 100f : x;

        /// <summary>cLuxPlayer::AddSanity: capped at 100, below 0 starts the insanity collapse.</summary>
        public void AddSanity(float x, bool showEffect = true)
        {
            if (SanityDisabled) { sanity = 100f; return; }
            if (sanity >= 100f && x > 0f) return;
            sanity = Mathf.Min(100f, sanity + x);
            if (sanity < 0f)
            {
                sanity = 0f;
                StartInsanityCollapse();
            }
            if (x > 0f && showEffect) SanityGained?.Invoke();
        }

        /// <summary>cLuxPlayer::GiveSanityDamage: hint and hit effect, then <see cref="LowerSanity"/>.</summary>
        public void GiveSanityDamage(float amount)
        {
            if (SanityDisabled) return;
            SanityHit?.Invoke();
            LowerSanity(amount, true);
            SanityHelper?.StartHit();
        }

        void StartInsanityCollapse()
        {
            InsanityCollapseHelper?.Start();
            InsanityCollapse?.Invoke();
        }

        public void LowerSanity(float amount, bool useEffect)
        {
            if (SanityDisabled) return;
            if (health <= 0f) return;
            sanity -= amount;
            if (sanity < 0f)
            {
                sanity = 0f;
                StartInsanityCollapse();
            }
            if (useEffect) SanityHelper?.SetSanityLost();
        }

        public void SetLampOil(float x) => lampOil = x;

        public void AddLampOil(float x)
        {
            if (lampOil >= 100f && x > 0f) return;
            lampOil = Mathf.Clamp(lampOil + x, 0f, 100f);
        }

        /// <summary>The map's sound handler and physics materials (set by the map).</summary>
        [NonSerialized] public Sound.HplSoundHandler sound;
        [NonSerialized] public HplSurfaces surfaces;

        /// <summary>The player's sounds (jump, crouch, fall damage): PlayGuiSoundData as a world sound.</summary>
        public void PlaySound(string soundName)
        {
            if (sound != null) sound.PlayGuiSoundData(soundName, RTXMnesia.Sound.SoundEntryType.World);
        }

        /// <summary>
        /// cLuxMoveState_Normal::FootSound: step_&lt;walk|run|sneak&gt;_&lt;step type&gt; of the material under the
        /// feet (the body the gravity collided with; "rock" without one) or of the water the player is in; enemies hear it.
        /// </summary>
        public void OnFootSound(FootSound type, string moveType)
        {
            FootSounded?.Invoke(type, moveType);
            if (sound == null) return;
            string step = "rock";
            var ground = Body != null ? Body.GroundCollider : null;
            if (IsInWater) step = WaterStepSound;
            else if (ground != null && surfaces != null)
            {
                var body = HplPhysics.BodyOf(ground);
                var pm = ground.sharedMaterial;
                var surface = surfaces.GetOrDefault(body != null ? body.material : pm != null ? pm.name : null);
                if (surface != null) step = surface.stepType;
            }
            string name = type == FootSound.Step ? $"step_{moveType}_{step}" : $"step_run_{step}";
            sound.PlayGuiSoundData(name, RTXMnesia.Sound.SoundEntryType.World, out var data, out _);
            // Enemies hear the step at the feet (cLuxMap::BroadcastEnemySoundMessage with the .snt volume and distances).
            var map = ScriptApi.HplMap.Current;
            if (map != null && data != null)
                map.BroadcastEnemySoundMessage(Body.FeetPosition + new Vector3(0f, 0.1f, 0f), data.volume, data.minDistance, data.maxDistance);
        }

        void SetFreeCamera(bool active)
        {
            if (_freeCamera == null) return;
            _freeCameraActive = active;
            _freeCamera.enabled = active;
            if (!active) UpdateCameraTransform(1f);
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}

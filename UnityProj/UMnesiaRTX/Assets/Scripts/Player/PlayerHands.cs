using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>
    /// cLuxPlayerHands (LuxPlayerHands.cpp) with iLuxHandObject (LuxHandObject.cpp) and cLuxHandObject_LightSource
    /// (LuxHandObject_LightSource.cpp). The hands model follows the camera with its rotation smoothed over the last
    /// HandsRotationSmoothNum steps (squared rising weights) and the bob / shake / hurt head offsets × HandsPosAddMul
    /// taken back. A hand object hangs on the hands' attach bone at its .ho offset; changing it holsters the current
    /// one (animation), then draws the new one. A light source fades its lights, billboards and illumination in and
    /// out and swings on a pendulum (gravity along the view pitch, the player's speed, friction, angle limits).
    /// Resources: <see cref="HplHandsData"/> (HandsConverter).
    /// </summary>
    public sealed class PlayerHands
    {
        public enum HandsState { Disabled, Holster, Draw, Idle, HandObject }

        sealed class HandObject
        {
            public HplHandsData.HandObject Data;
            public Transform Entity;
            public readonly List<AmnesiaLight> Lights = new();
            public readonly List<AmnesiaBillboard> Billboards = new();
            public readonly List<(Renderer renderer, Material material, Color emissive)> Illumination = new();
            public readonly List<(Transform node, Vector3 position, Quaternion rotation, Vector3 scale)> SubMeshes = new();
            public Color[] DefaultColors, FadeOutColors;
            public bool[] DefaultFlicker;
            /// <summary>Near-field copies of the lights for the hands' rendering layer, with their intensity scale.</summary>
            public readonly List<(Light main, Light copy, float scale)> HandLights = new();
            public float SwayAngle, SwayVel;
            public bool Created;
        }

        static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        static bool s_warned;

        readonly AmnesiaPlayer _player;
        readonly Transform _root, _hands;
        readonly Animation _animation;
        readonly Dictionary<string, HandObject> _objects = new();
        readonly List<Vector3> _camRotations = new();
        readonly int _maxCamRotations;
        readonly float _posAddMul;
        readonly MaterialPropertyBlock _block = new();
        HandObject _current;
        string _currentAnim = "";
        Quaternion _smoothedRotation = Quaternion.identity;

        public HandsState State { get; private set; } = HandsState.Disabled;
        /// <summary>cLuxPlayerHands::mfHandObjectAlpha (not reset when the object changes).</summary>
        public float HandObjectAlpha { get; private set; }
        public string CurrentHandObject => _current != null ? _current.Data.name : "";
        public string CurrentAnimation => _currentAnim;
        public Transform Root => _root;
        /// <summary>The hands model and its legacy animation (for checks).</summary>
        public Transform HandsModel => _hands;
        public Animation HandsAnimation => _animation;

        PlayerHands(AmnesiaPlayer player, HplHandsData data, HplConfig config)
        {
            _player = player;
            _maxCamRotations = config.GetInt("Player_General", "HandsRotationSmoothNum", 0);
            _posAddMul = config.GetFloat("Player_General", "HandsPosAddMul", 0f);
            // CreateHandEntity: a world entity, outside the map hierarchy (the map does not collect it).
            var go = Object.Instantiate(data.gameObject);
            go.name = "HPL Player Hands (runtime)";
            go.hideFlags = HideFlags.DontSave;
            var instance = go.GetComponent<HplHandsData>();
            _root = go.transform;
            _hands = instance.hands;
            _animation = instance.handsAnimation;
            _hands.gameObject.SetActive(false);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (r.GetComponent<AmnesiaBillboard>() == null) r.renderingLayerMask = HplLighting.HandsLayerMask;
            foreach (var o in instance.handObjects)
            {
                if (o.entity == null) continue;
                o.entity.gameObject.SetActive(false);
                _objects[o.name] = new HandObject { Data = o, Entity = o.entity };
            }
        }

        /// <summary>The hands of the player, or null (with one warning) if the resources are not imported.</summary>
        public static PlayerHands Create(AmnesiaPlayer player, HplConfig config)
        {
            var data = Resources.Load<HplHandsData>(HplHandsData.ResourceName);
            if (data == null)
            {
                if (!s_warned) Debug.LogWarning("[RTXMnesia] Player hands not imported (Tools > RTXMnesia > Import > Player hands and hand objects): the lantern has no model");
                s_warned = true;
                return null;
            }
            return new PlayerHands(player, data, config);
        }

        /// <summary>The map's sound and particles for the hand objects' lights (flicker effects).</summary>
        public void SetWorld(Sound.HplSoundHandler sound, Particles.HplParticleWorld particles)
        {
            foreach (var o in _objects.Values)
                foreach (var light in o.Entity.GetComponentsInChildren<AmnesiaLight>(true))
                {
                    light.sound = sound;
                    light.particles = particles;
                }
        }

        // ------------------------------------------------------------------ hand object switching

        /// <summary>cLuxPlayerHands::SetActiveHandObject: "" puts the current one away.</summary>
        public void SetActiveHandObject(string name)
        {
            if (string.IsNullOrEmpty(name)) SetCurrentHandObject(null);
            else if (_objects.TryGetValue(name, out var o)) SetCurrentHandObject(o);
            else HplLogError($"Could not load hand object file: '{name}'");
        }

        void SetCurrentHandObject(HandObject o)
        {
            if (_current == o) return;
            if (_current != null)
            {
                PlayAnim(_current.Data.animHolster, false);
                State = HandsState.Holster;
            }
            else if (o != null)
            {
                CreateAndAttach(o);
                // Shown before the animation starts: a legacy Animation of an inactive object does not play.
                _hands.gameObject.SetActive(true);
                PlayAnim(o.Data.animDraw, false);
                State = HandsState.Draw;
            }
            _current = o;
        }

        /// <summary>
        /// cLuxPlayerHands::OnMapEnter after a map change (the hands are one for the whole game): a held object is
        /// shown again at once in its idle animation, with its fade as it was.
        /// </summary>
        public void Restore(string handObject, HandsState state, float alpha)
        {
            HandObjectAlpha = alpha;
            if (string.IsNullOrEmpty(handObject) || !_objects.TryGetValue(handObject, out var o)) return;
            _current = o;
            State = state;
            CreateAndAttach(o);
            HandObjectAlpha = alpha;
            _hands.gameObject.SetActive(true);
            PlayAnim(o.Data.animIdle, true, true);
            UpdateLightSource(o, 0f, true);
            SyncHandLights(o);
        }

        /// <summary>cLuxPlayerHands::CreateAndAttachHandObject (+ iLuxHandObject::CreateEntity, LightSource setup).</summary>
        void CreateAndAttach(HandObject o)
        {
            foreach (var other in _objects.Values) other.Entity.gameObject.SetActive(false);
            if (!o.Created)
            {
                o.Created = true;
                var bone = FindBone(_hands, o.Data.attachBone);
                if (bone == null) HplLogError($"Could not find bone '{o.Data.attachBone}' in player hands model!");
                else
                {
                    o.Entity.SetParent(bone, false);
                    // m_mtxOffset = R(OffsetRotation, XYZ) · S(OffsetScale), translated to OffsetPosition (HPL space).
                    o.Entity.SetLocalPositionAndRotation(HplSpace.Position(o.Data.offsetPosition),
                                                         HplSpace.Rotation(o.Data.offsetRotation * Mathf.Deg2Rad));
                    o.Entity.localScale = o.Data.offsetScale;
                }
                o.Lights.AddRange(o.Entity.GetComponentsInChildren<AmnesiaLight>(true));
                o.Billboards.AddRange(o.Entity.GetComponentsInChildren<AmnesiaBillboard>(true));
                foreach (var light in o.Lights) light.InitRuntime();
                foreach (var bb in o.Billboards)
                {
                    if (string.IsNullOrEmpty(bb.connectLight)) continue;
                    var light = o.Lights.Find(l => l.name == bb.connectLight);
                    if (light != null) light.AttachBillboard(bb);
                }
                foreach (var light in o.Lights) CreateHandLight(o, light, bone);
                o.DefaultColors = new Color[o.Lights.Count];
                o.FadeOutColors = new Color[o.Lights.Count];
                o.DefaultFlicker = new bool[o.Lights.Count];
                for (int i = 0; i < o.Lights.Count; i++)
                {
                    o.DefaultColors[i] = o.Lights[i].CurrentColor;
                    o.DefaultFlicker[i] = o.Lights[i].FlickerActive;
                    o.Lights[i].SetFlickerActive(false);
                }
                foreach (var r in o.Entity.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is not MeshRenderer || r.GetComponent<AmnesiaBillboard>() != null) continue;
                    var m = r.sharedMaterial;
                    if (m != null && m.HasProperty(EmissiveColorId)) o.Illumination.Add((r, m, m.GetColor(EmissiveColorId)));
                    if (r.GetComponent<MeshFilter>() != null)
                        o.SubMeshes.Add((r.transform, r.transform.localPosition, r.transform.localRotation, r.transform.localScale));
                }
            }
            o.Entity.gameObject.SetActive(true);
            HandObjectAlpha = 0f;
        }

        /// <summary>
        /// The hand-held light lights the world, not the hands; a copy lights only the hands
        /// (HplLighting.HandsRenderingLayer). HDRP's falloff is 1/(d² + r²) with r the light's shape radius: r = the
        /// light's distance from the attach bone keeps the whole hand within 2× (HPL2's falloff is nearly flat
        /// there), and the intensity gives HPL2's level at that distance.
        /// </summary>
        void CreateHandLight(HandObject o, AmnesiaLight light, Transform bone)
        {
            if (!light.TryGetComponent(out Light main)) return;
            light.handHeld = true;
            HplLighting.SetLightLayers(main, HplLighting.LightLayersMask & ~HplLighting.HandsLayerMask);
            var go = new GameObject(light.name + " (hands)");
            go.transform.SetParent(light.transform, false);
            var copy = go.AddComponent<Light>();
            copy.type = main.type;
            copy.shadows = LightShadows.None;
            float reference = Mathf.Max(0.0001f, light.radius * HplLighting.ReferenceDistance);
            float distance = bone != null ? Mathf.Max(0.01f, Vector3.Distance(light.transform.position, bone.position)) : reference;
            if (main.TryGetComponent(out UnityEngine.Rendering.HighDefinition.HDAdditionalLightData mainData))
            {
                var data = go.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
                data.affectDiffuse = mainData.affectDiffuse;
                data.affectSpecular = mainData.affectSpecular;
            }
            copy.shapeRadius = distance;
            copy.lightUnit = main.lightUnit;
            HplLighting.SetLightLayers(copy, HplLighting.HandsLayerMask);
            o.HandLights.Add((main, copy, 2f * distance * distance / (reference * reference)));
        }

        static void SyncHandLights(HandObject o)
        {
            foreach (var (main, copy, scale) in o.HandLights)
            {
                copy.enabled = main.enabled;
                copy.color = main.color;
                copy.intensity = main.intensity * scale;
                copy.range = main.range;
            }
        }

        static Transform FindBone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        // ------------------------------------------------------------------ animation

        /// <summary>
        /// cMeshEntity::PlayFadeToName with a 0.2 s fade (a non-looping one holds its last frame). HPL2 normalizes the
        /// weights of the active animations (cMeshEntity::GetAnimationWeightMul), so an animation fading in while no
        /// other is active is the full pose at once; Unity's cross fade from nothing would blend from the rest pose
        /// (the hands popped out of the import pose at the start of the draw).
        /// </summary>
        void PlayAnim(string name, bool loop, bool instant = false)
        {
            _currentAnim = name ?? "";
            if (_animation == null || string.IsNullOrEmpty(name)) return;
            var state = _animation[name];
            if (state == null)
            {
                HplLogError($"Hands animation '{name}' does not exist");
                return;
            }
            state.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
            bool othersActive = false;
            foreach (AnimationState other in _animation)
                if (other != state && other.enabled && other.weight > 0f) othersActive = true;
            state.time = 0f;
            if (instant || !othersActive)
            {
                _animation.Stop();
                state.time = 0f;
                _animation.Play(name, PlayMode.StopAll);
            }
            else _animation.CrossFade(name, 0.2f, PlayMode.StopAll);
        }

        bool AnimOver()
        {
            if (_animation == null || _currentAnim == "") return true;
            var state = _animation[_currentAnim];
            return state == null || (state.wrapMode != WrapMode.Loop && state.time >= state.length);
        }

        // ------------------------------------------------------------------ update

        /// <summary>cLuxPlayerHands::Update.</summary>
        public void Update(float timeStep)
        {
            switch (State)
            {
                case HandsState.Disabled:
                    return;
                case HandsState.Holster:
                    if (AnimOver())
                    {
                        if (_current != null)
                        {
                            CreateAndAttach(_current);
                            PlayAnim(_current.Data.animDraw, false);
                            State = HandsState.Draw;
                        }
                        else
                        {
                            foreach (var o in _objects.Values) o.Entity.gameObject.SetActive(false);
                            // Unity rewinds the states of a deactivated object; a stale holster at its first frame
                            // would blend into the next draw.
                            if (_animation != null) _animation.Stop();
                            _hands.gameObject.SetActive(false);
                            State = HandsState.Disabled;
                        }
                    }
                    break;
                case HandsState.Draw:
                    if (AnimOver())
                    {
                        PlayAnim(_current.Data.animIdle, true);
                        State = HandsState.Idle;
                    }
                    break;
                case HandsState.HandObject:
                    if (_current != null && AnimOver())
                    {
                        PlayAnim(_current.Data.animIdle, true);
                        State = HandsState.Idle;
                        return;
                    }
                    break;
            }

            // The current object might not be drawn yet: all active ones update.
            foreach (var o in _objects.Values)
                if (o.Created && o.Entity.gameObject.activeSelf)
                {
                    if (o.Data.type == "LightSource") UpdateLightSource(o, timeStep, false);
                    foreach (var light in o.Lights) light.UpdateLight(timeStep);
                    SyncHandLights(o);
                }
        }

        /// <summary>cLuxPlayerHands::PostUpdate → UpdatePlayerHandsPos: one smoothing sample per step.</summary>
        public void PostUpdate()
        {
            _camRotations.Add(new Vector3(_player.Pitch, _player.Yaw, _player.Roll));
            if (_camRotations.Count > _maxCamRotations) _camRotations.RemoveAt(0);
            Vector3 sum = Vector3.zero;
            float total = 0f;
            for (int i = 0; i < _camRotations.Count; i++)
            {
                float weight = (i + 1f) / _camRotations.Count;
                weight *= weight;
                sum += _camRotations[i] * weight;
                total += weight;
            }
            Vector3 r = total > 0f ? sum / total : Vector3.zero;
            // The camera's Euler order (ZXY in HPL2 and Unity alike); the hands model faces like HPL2's camera (-Z).
            _smoothedRotation = Quaternion.Euler(r.x * Mathf.Rad2Deg, r.y * Mathf.Rad2Deg, r.z * Mathf.Rad2Deg) * Quaternion.Euler(0f, 180f, 0f);
        }

        /// <summary>Every frame after the camera moved: the hands at the (interpolated) camera.</summary>
        public void UpdateTransform(Transform camera)
        {
            if (State == HandsState.Disabled || camera == null) return;
            Vector3 add = _player.GetHeadPosAdd(HeadPosAdd.Bob) + _player.GetHeadPosAdd(HeadPosAdd.ScreenShake) + _player.GetHeadPosAdd(HeadPosAdd.Hurt);
            Vector3 world = camera.right * add.x + camera.up * add.y + camera.forward * add.z;
            _hands.SetPositionAndRotation(camera.position + world * _posAddMul, _smoothedRotation);
        }

        /// <summary>cLuxHandObject_LightSource::Update (and the alpha setup when restored after a map change).</summary>
        void UpdateLightSource(HandObject o, float timeStep, bool force)
        {
            if (o.Data.hasSwayPhysics && timeStep > 0f) UpdateSway(o, timeStep);

            bool update = force, done = false;
            float fadeIn = o.Data.fadeInTime == 0f ? 1000f : 1f / o.Data.fadeInTime;
            float fadeOut = o.Data.fadeOutTime == 0f ? 1000f : 1f / o.Data.fadeOutTime;
            bool holster = State == HandsState.Holster;
            if (holster)
            {
                for (int i = 0; i < o.Lights.Count; i++)
                    if (o.DefaultFlicker[i])
                    {
                        o.Lights[i].SetFlickerActive(false);
                        o.Lights[i].StopFading();
                    }
                HandObjectAlpha = Mathf.Max(0f, HandObjectAlpha - fadeOut * timeStep);
                update = true;
            }
            else if (HandObjectAlpha < 1f)
            {
                HandObjectAlpha += fadeIn * timeStep;
                if (HandObjectAlpha > 1f)
                {
                    HandObjectAlpha = 1f;
                    done = true;
                }
                update = true;
            }
            else if (force) done = true;

            if (!holster)
                for (int i = 0; i < o.Lights.Count; i++) o.FadeOutColors[i] = o.Lights[i].CurrentColor;

            if (update)
            {
                float alpha = HandObjectAlpha;
                foreach (var (renderer, _, emissive) in o.Illumination)
                {
                    renderer.GetPropertyBlock(_block);
                    _block.SetColor(EmissiveColorId, emissive * alpha);
                    renderer.SetPropertyBlock(_block);
                }
                for (int i = 0; i < o.Lights.Count; i++)
                    o.Lights[i].SetColor((holster ? o.FadeOutColors[i] : o.DefaultColors[i]) * alpha);
                foreach (var bb in o.Billboards)
                {
                    Color c = bb.color;
                    c.a = alpha;
                    bb.SetColor(c);
                }
            }
            if (done)
                for (int i = 0; i < o.Lights.Count; i++)
                    if (o.DefaultFlicker[i]) o.Lights[i].SetFlickerActive(true);
        }

        /// <summary>cLuxHandObject_LightSource::UpdateSwayPhysics, in HPL2 angles.</summary>
        void UpdateSway(HandObject o, float timeStep)
        {
            var d = o.Data;
            Vector3 cam = _player.HplCameraAngles;
            float downAngle = -cam.x + cam.z * d.swayCameraRollMul;
            downAngle = Mathf.Clamp(downAngle, d.swayDownAngleLimits.x * Mathf.Deg2Rad, d.swayDownAngleLimits.y * Mathf.Deg2Rad);
            float angle = o.SwayAngle - downAngle;
            o.SwayVel += -Mathf.Sin(angle) * d.swayGravity * timeStep;

            float playerSpeed = _player.Body.GetVelocity(1f / 60f).magnitude;
            if (_player.Body.GetMoveSpeed(CharDir.Forward) < 0f) playerSpeed = -playerSpeed;
            o.SwayVel += -playerSpeed * d.swayPlayerSpeedMul;

            o.SwayVel = Mathf.Clamp(o.SwayVel, -d.maxSwayVel, d.maxSwayVel);
            o.SwayVel -= o.SwayVel * d.swayFriction * timeStep;

            o.SwayAngle += o.SwayVel * timeStep;
            float min = d.swayAngleLimits.x * Mathf.Deg2Rad, max = d.swayAngleLimits.y * Mathf.Deg2Rad;
            if (o.SwayAngle < min) { o.SwayVel = 0f; o.SwayAngle = min; }
            if (o.SwayAngle > max) { o.SwayVel = 0f; o.SwayAngle = max; }

            // MatrixRotate(pin · angle, XYZ) × each sub mesh's default matrix, except the skipped one (the handle).
            Quaternion sway = HplSpace.Rotation(d.swayPinDir.normalized * o.SwayAngle);
            foreach (var (node, position, rotation, scale) in o.SubMeshes)
            {
                if (node.name == d.skipSwaySubMesh) continue;
                node.SetLocalPositionAndRotation(sway * position, sway * rotation);
                node.localScale = scale;
            }
        }

        static void HplLogError(string message) => ScriptApi.HplLog.Error(message);
    }
}

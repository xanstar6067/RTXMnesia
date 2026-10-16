using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// A liquid area (cLuxArea_Liquid, LuxArea_Liquid.cpp; the variables of cLuxAreaLoader_Liquid): every proper step
    /// the bodies whose bounds touch the area's bounds are tested against its box. Bodies with mass that enter get
    /// buoyancy (surface at the top of the box, density, viscosities) and a splash; waves near the camera move their
    /// surface; a body in the player's hands floats not. The player in it is slowed and steps with the liquid's step
    /// type (a splash on entering); an enemy in it knows the surface for its step splashes. A body or character that
    /// leaves the area's bounds is no longer iterated, so its flags stay set (as in HPL2: the player start resets the
    /// player's).
    /// </summary>
    public sealed class HplLiquidArea
    {
        readonly HplArea _area;
        readonly float _density, _linearViscosity, _angularViscosity;
        readonly HplSurfaces.Surface _material;
        readonly bool _hasWaves;
        readonly float _waveAmp, _waveFreq, _playerSpeedMul, _maxWaveDistanceSqr;
        readonly float _surfaceY;
        float _timeCount;
        readonly Collider[] _shape = new Collider[1];
        readonly List<Collider> _bodyShapes = new();

        public float SurfaceY => _surfaceY;
        public HplSurfaces.Surface Material => _material;

        public HplLiquidArea(HplArea area, AmnesiaArea data, HplSurfaces surfaces)
        {
            _area = area;
            _density = Var(data, "Density", 0f);
            _linearViscosity = Var(data, "LinearViscosity", 0f);
            _angularViscosity = Var(data, "AngularViscosity", 0f);
            string material = data.GetVariable("PhysicsMaterial") ?? "";
            _material = surfaces != null ? surfaces.Get(material) : null;
            if (_material == null) HplLog.Error($"Could not load physics material '{material}' for liquid area '{area.Name}'!");
            _hasWaves = data.GetVariable("HasWaves") == "true";
            _waveAmp = Var(data, "WaveAmp", 0f);
            _waveFreq = Var(data, "WaveFreq", 0f);
            _playerSpeedMul = Var(data, "PlayerSpeedMul", 0.8f);
            float maxWaveDistance = Var(data, "MaxWaveDistance", 25f);
            _maxWaveDistanceSqr = maxWaveDistance * maxWaveDistance;
            // SetupAfterLoad: the plane at the top of the box (its rotation is not taken).
            _surfaceY = data.transform.position.y + data.size.y / 2f;
        }

        static float Var(AmnesiaArea data, string name, float fallback)
        {
            string s = data.GetVariable(name);
            return s == null ? fallback : HplMath.Atof(s);
        }

        /// <summary>cLuxArea_Liquid::OnUpdate.</summary>
        public void Update(HplMap map, float timeStep)
        {
            if (timeStep < Time.fixedDeltaTime * 0.8f) return;
            var areaShapes = _area.GetBodyColliders(0);
            if (!HplCollision.TryGetBounds(areaShapes, out Bounds areaBounds)) return;
            _timeCount += timeStep;

            // Rigid bodies (GetBodiesInBV: the bodies whose bounds overlap the area's).
            var bodies = map.PhysicsWorld.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (!body.Collide || !body.Active) continue;
                if (body.IsStatic) continue;
                if (!body.TryGetBounds(out Bounds bounds) || !bounds.Intersects(areaBounds)) continue;
                _bodyShapes.Clear();
                _bodyShapes.AddRange(body.Colliders);
                bool inside = HplCollision.BodiesCollide(_bodyShapes, areaShapes);
                DoBuoyancyOnBody(map, body, inside);
            }

            // Characters: the player, then the enemies.
            var player = map.player;
            if (player != null && player.Body != null && player.Body.isActiveAndEnabled && player.Body.Shapes.Count > 0)
            {
                var body = player.Body;
                if (new Bounds(body.Position, body.Size).Intersects(areaBounds))
                {
                    _shape[0] = body.Shapes[body.ActiveSize];
                    bool inside = HplCollision.BodiesCollide(_shape, areaShapes);
                    if (inside)
                    {
                        if (!player.IsInWater)
                        {
                            player.IsInWater = true;
                            player.WaterSpeedMul = _playerSpeedMul;
                            if (_material != null) player.WaterStepSound = _material.stepType;
                            SplashEffect(map, body.ForceVelocity.magnitude, body.Position);
                        }
                    }
                    else player.IsInWater = false;
                }
            }
            foreach (var enemy in map.Enemies)
            {
                var body = enemy.Body;
                if (body == null || !body.isActiveAndEnabled || body.Shapes.Count == 0) continue;
                if (!new Bounds(body.Position, body.Size).Intersects(areaBounds)) continue;
                _shape[0] = body.Shapes[body.ActiveSize];
                if (HplCollision.BodiesCollide(_shape, areaShapes))
                {
                    if (!enemy.IsInWater)
                    {
                        enemy.IsInWater = true;
                        enemy.WaterSurfaceY = _surfaceY;
                        enemy.WaterSurface = _material;
                    }
                }
                else enemy.IsInWater = false;
            }
        }

        void DoBuoyancyOnBody(HplMap map, HplPhysicsBody body, bool inside)
        {
            if (!inside)
            {
                body.BuoyancyActive = false;
                return;
            }
            var player = map.player;
            if (player != null && player.States != null && !player.States.Current.AllowBuoyancy(body))
            {
                body.BuoyancyActive = false;
                return;
            }
            if (!body.BuoyancyActive)
            {
                body.BuoyancySurfaceY = _surfaceY;
                body.BuoyancyDensity = _density;
                body.BuoyancyLinearViscosity = _linearViscosity;
                body.BuoyancyAngularViscosity = _angularViscosity;
                SplashEffect(map, body.LinearVelocity.magnitude, body.WorldCenterOfMass);
                body.BuoyancyActive = true;
            }

            // Waves: the surface moves with the HPL2 x and z of the mass centre.
            var camera = map.PlayerCamera;
            if (camera == null) return;
            Vector3 com = body.WorldCenterOfMass;
            if (_hasWaves && (camera.transform.position - body.Position).sqrMagnitude < _maxWaveDistanceSqr)
            {
                Vector3 hpl = HplSpace.Position(com);
                float addX = Mathf.Sin(_timeCount * _waveFreq + hpl.x * 15f) * _waveAmp * 0.3f;
                float addZ = Mathf.Sin(_timeCount * _waveFreq + hpl.z * 15f) * _waveAmp * 0.3f;
                body.BuoyancySurfaceY = _surfaceY + addX + addZ;
                body.WakeUp();
            }
        }

        /// <summary>cLuxArea_Liquid::SplashEffect: the impact of the liquid's material at the speed, on the surface.</summary>
        void SplashEffect(HplMap map, float speed, Vector3 position)
        {
            if (_material == null) return;
            var impact = _material.GetImpactDataFromSpeed(speed);
            if (impact == null) return;
            position.y = _surfaceY + 0.01f;
            if (!string.IsNullOrEmpty(impact.psName) && map.ParticleWorld != null)
            {
                var system = map.ParticleWorld.Create("Splash", impact.psName);
                if (system != null) system.LocalMatrix = Matrix4x4.Translate(position);
            }
            if (!string.IsNullOrEmpty(impact.soundName) && map.sound != null)
            {
                var sound = map.sound.CreateSoundEntity("Splash", impact.soundName, true);
                if (sound == null) return;
                sound.IsSaved = false;
                sound.Position = position;
            }
        }
    }
}

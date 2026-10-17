using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// A rope of the map (cLuxRope, LuxArea_Rope.cpp; made from a Rope area by cLuxAreaRopeLoader): its properties
    /// come from the area variables, or from the RopeFile settings when it loads. After the world is loaded
    /// (AfterWorldLoad, once) it creates the physics rope from the area to the EndPosNode, attaches the start / end
    /// bodies (a body name, else a prop's main body) and the drawn rope. The entity itself has no bodies, no
    /// interaction and its active state changes nothing (OnSetActive is empty in HPL2).
    /// </summary>
    public sealed class HplRope : HplEntity
    {
        public readonly AmnesiaArea Component;
        public override HplEntityType Type => HplEntityType.Rope;

        bool _ropeCreated;
        Vector3 _startPos, _endPos;   // HPL2 space
        string _endPosNode, _startBody, _endBody;
        float _minTotalLength, _maxTotalLength, _segmentLength, _damping, _strength, _stiffness;
        string _material;
        float _radius, _lengthTileAmount, _lengthTileSize;
        string _sound;
        float _soundStartSpeed, _soundStopSpeed;
        bool _autoMove;
        float _autoMoveAcc, _autoMoveMaxSpeed;

        public HplPhysicsRope Physics { get; private set; }
        public HplRopeGraphics Graphics { get; private set; }

        public HplRope(AmnesiaArea area) : base(area.gameObject, area.hplId)
        {
            Component = area;
            // The area box is only a marker in the editor.
            foreach (var c in area.GetComponents<Collider>()) c.enabled = false;

            _startPos = HplSpace.Position(area.transform.position);
            _endPosNode = Var("EndPosNode");
            _startBody = Var("StartBody");
            _endBody = Var("EndBody");
            _autoMove = Var("AutoMove") == "true";
            _autoMoveAcc = VarFloat("AutoMoveAcc");
            _autoMoveMaxSpeed = VarFloat("AutoMoveMaxSpeed");
            _minTotalLength = VarFloat("MinTotalLength");
            _maxTotalLength = VarFloat("MaxTotalLength");

            string file = Var("RopeFile");
            List<HplVariable> settings = null;
            if (file != "")
            {
                settings = HplRopeAssets.Instance != null ? HplRopeAssets.Instance.GetFile(file) : null;
                if (settings == null) HplLog.Error($"Could not load '{file}' for rope '{Name}'. Using set variables instead!");
            }
            string Setting(string name) => settings != null ? HplVariable.Find(settings, name) ?? "" : Var(name);
            float SettingFloat(string name) => HplMath.Atof(Setting(name));
            _segmentLength = SettingFloat("SegmentLength");
            _damping = SettingFloat("Damping");
            _strength = SettingFloat("Strength");
            _stiffness = SettingFloat("Stiffness");
            _material = Setting("Material");
            _radius = SettingFloat("Radius");
            _lengthTileAmount = SettingFloat("LengthTileAmount");
            _lengthTileSize = SettingFloat("LengthTileSize");
            _sound = Setting("Sound");
            _soundStartSpeed = SettingFloat("SoundStartSpeed");
            _soundStopSpeed = SettingFloat("SoundStopSpeed");
        }

        string Var(string name) => Component.GetVariable(name) ?? "";
        float VarFloat(string name) => HplMath.Atof(Var(name));

        public override int BodyCount => 0;
        public override IReadOnlyList<Collider> GetBodyColliders(int index) => null;
        public override Transform AttachTransform => GameObject.transform;

        /// <summary>cLuxRope::OnSetActive does nothing: the rope keeps hanging and being drawn.</summary>
        protected override void OnSetActive(bool active) { }

        /// <summary>cLuxRope::AfterWorldLoad: the physics and drawn rope are created once.</summary>
        public void AfterWorldLoad(Transform graphicsParent)
        {
            if (_ropeCreated) return;
            _ropeCreated = true;

            var node = Map.GetPosNode(_endPosNode);
            if (node == null)
            {
                HplLog.Error($"Could not find end pos '{_endPosNode}' for rope '{Name}'");
                return;
            }
            _endPos = HplSpace.Position(node.transform.position);

            HplPhysicsBody startBody = _startBody != "" ? GetBody(_startBody) : null;
            HplPhysicsBody endBody = _endBody != "" ? GetBody(_endBody) : null;

            var rope = Map.PhysicsWorld.CreateRope(Name, _startPos, _endPos);
            rope.MinTotalLength = _minTotalLength;
            rope.MaxTotalLength = _maxTotalLength;
            rope.SetSegmentLength(_segmentLength);
            rope.SetDamping(_damping);
            rope.Strength = _strength;
            rope.Stiffness = _stiffness;
            rope.MotorSound = _sound;
            rope.MotorSoundStartSpeed = _soundStartSpeed;
            rope.MotorSoundStopSpeed = _soundStopSpeed;
            rope.SetAutoMoveActive(_autoMove);
            rope.AutoMoveAcc = _autoMoveAcc;
            rope.AutoMoveMaxSpeed = _autoMoveMaxSpeed;
            rope.UniqueId = Id;
            if (startBody != null)
            {
                rope.SetAttachedStartBody(startBody);
                if (startBody.Mass != 0f) rope.StartParticle.InvMass = 1f;
            }
            if (endBody != null) rope.SetAttachedEndBody(endBody);
            Physics = rope;

            int maxSegments = _segmentLength > 0f ? (int)(_maxTotalLength / _segmentLength) + 4 : 4;
            var material = HplRopeAssets.Instance != null ? HplRopeAssets.Instance.GetMaterial(_material) : null;
            if (material == null) HplLog.Warn($"Rope material '{_material}' of '{Name}' is not imported (Import > Rope resources)");
            Graphics = new HplRopeGraphics(Name, rope, maxSegments, graphicsParent, material)
            {
                Radius = _radius,
                LengthTileAmount = _lengthTileAmount,
                LengthTileSize = _lengthTileSize,
            };
        }

        /// <summary>cLuxRope::GetBody: a body by name, else the main body of a prop of that name.</summary>
        HplPhysicsBody GetBody(string name)
        {
            var body = Map.GetBody(name)?.Physics;
            if (body != null) return body;
            if (Map.GetEntityByName(name) is not HplProp prop || prop.Type != HplEntityType.Prop)
            {
                HplLog.Error($"Could not find body or entity with the name '{name}'");
                return null;
            }
            return prop.MainBody;
        }

        public void DestroyGraphics() => Graphics?.Destroy();

        // ------------------------------------------------------------------ save (cEngineRope_SaveData)

        public Save.SavedRope CaptureSave()
        {
            var r = new Save.SavedRope();
            var rope = Physics;
            if (rope == null) return r;
            r.created = true;
            r.totalLength = rope.TotalLength;
            r.minTotalLength = rope.MinTotalLength;
            r.maxTotalLength = rope.MaxTotalLength;
            r.segmentLength = rope.SegmentLength;
            r.damping = rope.Damping;
            r.strength = rope.Strength;
            r.stiffness = rope.Stiffness;
            r.maxIterations = rope.MaxIterations;
            r.motorActive = rope.MotorActive;
            r.motorWantedLength = rope.MotorWantedLength;
            r.motorSpeedMul = rope.MotorSpeedMul;
            r.motorMinSpeed = rope.MotorMinSpeed;
            r.motorMaxSpeed = rope.MotorMaxSpeed;
            r.autoMove = rope.AutoMoveActive;
            r.autoMoveSpeed = rope.AutoMoveSpeed;
            r.autoMoveAcc = rope.AutoMoveAcc;
            // FromRope saves the acceleration as the max speed too.
            r.autoMoveMaxSpeed = rope.AutoMoveAcc;
            foreach (var p in rope.Particles)
                r.particles.Add(new Save.SavedRopeParticle { position = p.Position, prevPosition = p.PrevPosition, smoothPosition = p.SmoothPosition, invMass = p.InvMass });
            for (int i = 0; i < 2; i++)
            {
                var a = rope.GetAttachment(i);
                string body = a.Body != null ? a.Body.Name : "";
                if (i == 0) { r.startBody = body; r.startBodyLocalPos = a.BodyLocalPos; }
                else { r.endBody = body; r.endBodyLocalPos = a.BodyLocalPos; }
            }
            return r;
        }

        /// <summary>
        /// cEngineRope_SaveData::CreateRope + ToRope: the saved particles, lengths, motor and bodies. ToRope sets the
        /// auto move acceleration twice (once with the saved "max speed", itself the acceleration), so the max speed
        /// stays the new rope's default 1 — kept.
        /// </summary>
        public void RestoreSave(Save.SavedRope r, Transform graphicsParent)
        {
            if (r == null || !r.created) return;
            if (Physics == null) AfterWorldLoad(graphicsParent);
            var rope = Physics;
            if (rope == null) return;
            rope.MaxIterations = r.maxIterations;
            rope.RestoreLengths(r.totalLength, r.segmentLength);
            rope.MinTotalLength = r.minTotalLength;
            rope.MaxTotalLength = r.maxTotalLength;
            rope.SetDamping(r.damping);
            rope.Strength = r.strength;
            rope.Stiffness = r.stiffness;
            rope.SetMotorActive(r.motorActive);
            rope.MotorWantedLength = r.motorWantedLength;
            rope.MotorSpeedMul = r.motorSpeedMul;
            rope.MotorMinSpeed = r.motorMinSpeed;
            rope.MotorMaxSpeed = r.motorMaxSpeed;
            rope.SetAutoMoveActive(r.autoMove);
            rope.AutoMoveSpeed = r.autoMoveSpeed;
            rope.AutoMoveAcc = r.autoMoveAcc;
            rope.AutoMoveAcc = r.autoMoveMaxSpeed;
            rope.AutoMoveMaxSpeed = 1f;
            var particles = new List<(Vector3, Vector3, Vector3, float)>();
            foreach (var p in r.particles) particles.Add((p.position, p.prevPosition, p.smoothPosition, p.invMass));
            if (particles.Count >= 2) rope.Restore(particles);
            rope.RestoreAttachment(0, Map.GetBody(r.startBody)?.Physics, r.startBodyLocalPos);
            rope.RestoreAttachment(1, Map.GetBody(r.endBody)?.Physics, r.endBodyLocalPos);
            rope.SetSleeping(false);
        }
    }
}

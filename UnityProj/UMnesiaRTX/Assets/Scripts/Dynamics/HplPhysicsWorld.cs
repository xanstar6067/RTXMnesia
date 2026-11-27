using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Dynamics
{
    /// <summary>
    /// The map's rigid body world (port of iPhysicsWorld::Update, PhysicsWorld.cpp): creates Rigidbodies and joints
    /// from the imported <see cref="AmnesiaBody"/> / <see cref="AmnesiaJoint"/> data and runs one step around the
    /// PhysX simulation: bodies before (force callback, static movement), simulate, joints after (pivots, breaking),
    /// bodies after. The character body is moved by the player before this (HplCharacterBody).
    ///
    /// In play mode Unity simulates after FixedUpdate (Rigidbody interpolation needs that); the map calls
    /// <see cref="BeforeSimulate"/> in FixedUpdate and <see cref="AfterSimulate"/> after the physics step. In edit
    /// mode (simulation tools) <see cref="Step"/> runs all three with Physics.Simulate.
    /// </summary>
    public sealed class HplPhysicsWorld
    {
        /// <summary>Layer for bodies with CollideNonCharacter = false: no contacts with anything, queries still see it.</summary>
        public const int NoCollideLayer = 30;

        public readonly List<HplPhysicsBody> Bodies = new();
        public readonly List<HplJoint> Joints = new();
        /// <summary>The verlet ropes (iPhysicsWorld::mlstRopes).</summary>
        public readonly List<HplPhysicsRope> Ropes = new();
        readonly Dictionary<Rigidbody, HplPhysicsBody> _byRigidbody = new();
        readonly Dictionary<AmnesiaBody, HplPhysicsBody> _byData = new();
        readonly List<Collider> _characterShapes = new();
        readonly Collider[] _overlaps = new Collider[64];
        static bool _layersSet;

        /// <summary>Impact, scrape and roll sounds (set its Sound and Surfaces).</summary>
        public readonly HplSurfaceSounds SurfaceSounds;

        public HplPhysicsWorld()
        {
            SurfaceSounds = new HplSurfaceSounds(this);
            if (_layersSet) return;
            _layersSet = true;
            for (int layer = 0; layer < 32; layer++) Physics.IgnoreLayerCollision(NoCollideLayer, layer, true);
        }

        /// <summary>iPhysicsWorld::FadeoutAllLoopSounds: frozen props must not keep scraping / creaking in UI.</summary>
        public void FadeOutLoopSounds(float speed)
        {
            SurfaceSounds.FadeOutLoopSounds(speed);
            foreach (var joint in Joints) joint.FadeOutLoopSound(speed);
        }

        /// <summary>Creates the bodies of an entity (body name "&lt;entity&gt;_&lt;body&gt;") and then its joints.</summary>
        public List<HplPhysicsBody> AddEntity(Transform entity, HplSurfaces surfaces, bool interpolate)
        {
            var created = new List<HplPhysicsBody>();
            foreach (var data in entity.GetComponentsInChildren<AmnesiaBody>(true))
            {
                if (_byData.ContainsKey(data)) continue;
                var body = new HplPhysicsBody(this, entity.name + "_" + data.name, data, surfaces, interpolate);
                Bodies.Add(body);
                _byRigidbody[body.Rigidbody] = body;
                _byData[data] = body;
                created.Add(body);
                UpdateCharacterCollision(body);
            }
            foreach (var data in entity.GetComponentsInChildren<AmnesiaJoint>(true))
            {
                if (data.childBody == null || !_byData.TryGetValue(data.childBody, out var child)) continue;
                HplPhysicsBody parent = null;
                if (data.parentBody != null) _byData.TryGetValue(data.parentBody, out parent);
                Joints.Add(new HplJoint(this, data, parent, child));
            }
            return created;
        }

        public HplPhysicsBody BodyOf(Rigidbody rb) => rb != null && _byRigidbody.TryGetValue(rb, out var b) ? b : null;

        public HplPhysicsBody BodyOf(Collider c) => c != null ? BodyOf(c.attachedRigidbody) : null;

        public HplPhysicsBody BodyOf(AmnesiaBody data) => data != null && _byData.TryGetValue(data, out var b) ? b : null;

        /// <summary>The character's shapes: bodies with CollideCharacter = false get no contacts with them.</summary>
        public void SetCharacterShapes(IEnumerable<Collider> shapes)
        {
            _characterShapes.Clear();
            _characterShapes.AddRange(shapes);
            foreach (var body in Bodies) UpdateCharacterCollision(body);
        }

        internal void UpdateCharacterCollision(HplPhysicsBody body)
        {
            foreach (var shape in _characterShapes)
                foreach (var c in body.Colliders)
                    if (shape != null && c != null) Physics.IgnoreCollision(shape, c, !body.CollideCharacter);
        }

        /// <summary>Deepest authored overlap of a jointed body that is left without contacts (Newton allowed this much).</summary>
        public const float MaxRestOverlap = 0.005f;
        readonly List<(Collider own, Collider other)> _restOverlaps = new();
        /// <summary>The collider pairs <see cref="IgnoreRestOverlaps"/> left without contacts (jointed body's, other).</summary>
        public IReadOnlyList<(Collider own, Collider other)> RestOverlaps => _restOverlaps;

        /// <summary>
        /// Authored rest overlaps (deviation, 2026-11-27): Newton let a body rest a few millimetres inside another
        /// without pushing it out. PhysX pushes it out; for a hinge or slider child the joint holds it in place, the
        /// normal force grows and friction locks the joint: the panel of work_desk_door.ent sits 1 mm into the bottom
        /// shelf of its desk (another entity) and the door did not open. So, in the loaded pose (before a save is
        /// applied), the colliders of a dynamic hinge / slider child get no contacts with the non-dynamic colliders
        /// they already overlap by at most <see cref="MaxRestOverlap"/>. The joint limits still hold the body.
        /// </summary>
        public void IgnoreRestOverlaps()
        {
            Physics.SyncTransforms();
            var pairs = new HashSet<(Collider, Collider)>(_restOverlaps);
            foreach (var joint in Joints)
            {
                if (joint.Type != HplJointType.Hinge && joint.Type != HplJointType.Slider) continue;
                var child = joint.Child;
                if (child.IsStatic) continue;
                foreach (var own in child.Colliders)
                {
                    if (own == null || !own.enabled || own.isTrigger || own.gameObject.layer == NoCollideLayer) continue;
                    var bounds = own.bounds;
                    int count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents + Vector3.one * MaxRestOverlap, _overlaps,
                                                           Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                    for (int i = 0; i < count; i++)
                    {
                        var other = _overlaps[i];
                        var rb = other.attachedRigidbody;
                        if (rb == child.Rigidbody || (rb != null && !rb.isKinematic) || other.gameObject.layer == NoCollideLayer) continue;
                        if (other.gameObject.layer == HplPhysics.CharacterLayer || _characterShapes.Contains(other) || pairs.Contains((own, other))) continue;
                        if (!Physics.ComputePenetration(own, own.transform.position, own.transform.rotation,
                                                        other, other.transform.position, other.transform.rotation, out _, out float depth)
                            || depth > MaxRestOverlap) continue;
                        Physics.IgnoreCollision(own, other, true);
                        pairs.Add((own, other));
                        _restOverlaps.Add((own, other));
                    }
                }
            }
        }

        /// <summary>A collider turned off and on again forgets IgnoreCollision: set the rest overlaps of the body again.</summary>
        internal void ReapplyRestOverlaps(HplPhysicsBody body)
        {
            foreach (var (own, other) in _restOverlaps)
                if (own != null && other != null && (Array.IndexOf(body.Colliders, own) >= 0 || Array.IndexOf(body.Colliders, other) >= 0))
                    Physics.IgnoreCollision(own, other, true);
        }

        /// <summary>iPhysicsWorld::EnableBodiesInBV for a static body moving through others (UpdateBeforeSimulate).</summary>
        internal void WakeBodiesNear(HplPhysicsBody body)
        {
            if (!body.TryGetBounds(out Bounds bounds)) return;
            int count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents * 1.2f, _overlaps, Quaternion.identity,
                                                   Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var rb = _overlaps[i].attachedRigidbody;
                if (rb != null && rb != body.Rigidbody && !rb.isKinematic) rb.WakeUp();
            }
        }

        /// <summary>iPhysicsWorld::CreateRope.</summary>
        public HplPhysicsRope CreateRope(string name, Vector3 hplStart, Vector3 hplEnd)
        {
            var rope = new HplPhysicsRope(name, this, hplStart, hplEnd) { Sound = SurfaceSounds.Sound };
            Ropes.Add(rope);
            return rope;
        }

        /// <summary>iPhysicsWorld::GetRope: by name, exact.</summary>
        public HplPhysicsRope GetRope(string name) => Ropes.Find(r => r.Name == name);

        public HplPhysicsRope GetRopeFromUniqueId(int id) => Ropes.Find(r => r.UniqueId == id);

        // iPhysicsWorld::Update: the ropes before the character and rigid bodies (they only add forces), and after
        // the bodies' after-simulation update.
        public void BeforeSimulate(float timeStep)
        {
            foreach (var rope in Ropes) rope.UpdateBeforeSimulate(timeStep);
            foreach (var body in Bodies) body.BeforeSimulate(timeStep);
            foreach (var joint in Joints) joint.BeforeSimulate();
            SurfaceSounds.BeforeSimulate();
        }

        public void AfterSimulate(float timeStep)
        {
            for (int i = 0; i < Joints.Count;)
            {
                var joint = Joints[i];
                if (joint.CheckBreakage())
                {
                    joint.Destroy();
                    Joints.RemoveAt(i);
                }
                else i++;
            }
            foreach (var body in Bodies) body.AfterSimulate(timeStep);
            foreach (var joint in Joints) joint.UpdateSound();
            SurfaceSounds.AfterSimulate(timeStep);
            foreach (var rope in Ropes) rope.UpdateAfterSimulate();
        }

        /// <summary>A whole step with an explicit simulation (edit mode, Physics.simulationMode = Script).</summary>
        public void Step(float timeStep)
        {
            BeforeSimulate(timeStep);
            Physics.Simulate(timeStep);
            AfterSimulate(timeStep);
        }

        /// <summary>Removes destroyed bodies and every joint they are part of.</summary>
        public void RemoveBodies(IEnumerable<HplPhysicsBody> bodies)
        {
            foreach (var body in new List<HplPhysicsBody>(bodies))
            {
                foreach (var joint in new List<HplJoint>(body.Joints)) DestroyJoint(joint);
                // ~iPhysicsBody: attached verlet containers let go of it.
                foreach (var rope in Ropes) rope.RemoveAttachedBody(body);
                SurfaceSounds.OnBodyRemoved(body);
                Bodies.Remove(body);
                _byRigidbody.Remove(body.Rigidbody);
                _byData.Remove(body.Data);
            }
        }

        public void DestroyJoint(HplJoint joint)
        {
            joint.Destroy();
            Joints.Remove(joint);
        }
    }
}

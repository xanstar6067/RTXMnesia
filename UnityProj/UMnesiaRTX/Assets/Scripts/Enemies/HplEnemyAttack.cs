using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>cLuxMapHelper::ShapeDamage (LuxMapHelper.cpp) for enemy and critter attacks (enemies not checked; lethal for the player unless told).</summary>
    public static class HplEnemyAttack
    {
        static readonly Collider[] Overlaps = new Collider[128];
        static readonly RaycastHit[] Hits = new RaycastHit[64];

        sealed class HitBody
        {
            public HplPhysicsBody physics;
            public Collider staticCollider;
            public bool player;
            public readonly List<Collider> colliders = new();
            public Vector3 Position => physics != null ? physics.Position : staticCollider != null ? staticCollider.transform.position : colliders[0].transform.position;
        }

        /// <summary>
        /// Every body the box touches (static geometry too: its hit surface effect plays) with no non-character body
        /// between <paramref name="origin"/> and it: characters get a velocity push (force × 0.1), dynamic bodies an
        /// impulse (force / mass, capped), then the hit effect and damage (random in [min, max]). True if anything was hit.
        /// </summary>
        public static bool ShapeDamage(HplMap map, HplEnemy attacker, Vector3 center, Quaternion rotation, Vector3 halfExtents, Vector3 origin,
                                       float minDamage, float maxDamage, float force, float maxImpulse, int strength, float hitSpeed,
                                       DamageType damageType, bool checkPlayer, bool checkProps, ref bool hitPlayer,
                                       float sphereRadius = 0f, bool lethalForPlayer = true)
        {
            hitPlayer = false;
            if (attacker != null && Dev.HplDebugSession.EnemiesPaused) return false;
            var player = map.player;
            int mask = Physics.DefaultRaycastLayers | (1 << HplPhysics.CharacterLayer);
            // A sphere shape (critters) or a box (enemies).
            int count = sphereRadius > 0f
                ? Physics.OverlapSphereNonAlloc(center, sphereRadius, Overlaps, mask, QueryTriggerInteraction.Ignore)
                : Physics.OverlapBoxNonAlloc(center, halfExtents, Overlaps, rotation, mask, QueryTriggerInteraction.Ignore);
            if (count == 0) return false;

            // GetBodiesInBV + CheckShapeCollision: the colliders grouped by their body.
            var bodies = new List<HitBody>();
            var byPhysics = new Dictionary<HplPhysicsBody, HitBody>();
            HitBody playerBody = null;
            for (int i = 0; i < count; i++)
            {
                var c = Overlaps[i];
                if (c.gameObject.layer == HplPhysics.CharacterLayer)
                {
                    bool isPlayer = player != null && player.Body != null && IsShapeOf(player.Body, c);
                    if (!isPlayer || !checkPlayer) continue; // other characters are enemies: not checked
                    if (playerBody == null) bodies.Add(playerBody = new HitBody { player = true });
                    playerBody.colliders.Add(c);
                    continue;
                }
                var physics = map.PhysicsWorld.BodyOf(c);
                if (physics != null)
                {
                    if (!physics.Active) continue;
                    if (physics.UserData != null && !checkProps) continue;
                    if (!byPhysics.TryGetValue(physics, out var hb)) { byPhysics[physics] = hb = new HitBody { physics = physics }; bodies.Add(hb); }
                    hb.colliders.Add(c);
                    continue;
                }
                var data = HplPhysics.BodyOf(c);
                if (data != null && !data.collideNonCharacter && !data.collideCharacter) continue;
                var sb = new HitBody { staticCollider = c };
                sb.colliders.Add(c);
                bodies.Add(sb);
            }

            bool hit = false;
            foreach (var body in bodies)
            {
                Vector3 bodyPos = body.player ? player.Body.Position : body.Position;
                if (AttackRayBlocked(origin, bodyPos, body)) continue;
                hit = true;

                HplEntity entity = body.physics != null ? body.physics.UserData as HplEntity : null;

                // The average of the contact points: the closest points of the body to the box center here.
                Vector3 hitPos = Vector3.zero;
                foreach (var c in body.colliders) hitPos += c.ClosestPoint(center);
                hitPos /= body.colliders.Count;

                if (body.player || (body.physics != null && !body.physics.IsStatic))
                {
                    Vector3 dir = (hitPos - origin).normalized;
                    if (body.player) player.Body.ForceVelocity += dir * force * 0.1f;
                    else
                    {
                        float impulse = Mathf.Min(force / body.physics.Mass, maxImpulse);
                        body.physics.AddImpulseAtPosition(dir * impulse, hitPos);
                    }
                }

                if (!body.player)
                {
                    var surface = body.physics != null ? body.physics.Surface : StaticSurface(map, body.staticCollider);
                    if (surface != null)
                    {
                        HplSurfaces.Effect hitData = null;
                        foreach (var h in surface.hits) if (hitSpeed >= h.minSpeed) { hitData = h; break; }
                        if (hitData != null)
                        {
                            if (hitData.soundName != "") PlaySoundAt(map, hitData.soundName, hitPos);
                            if (hitData.psName != "")
                            {
                                var ps = map.ParticleWorld.Create("HitPS", hitData.psName);
                                if (ps != null) ps.LocalMatrix = Matrix4x4.Translate(hitPos);
                            }
                        }
                    }
                }

                float damage = HplMath.RandRectf(minDamage, maxDamage);
                if (entity != null) entity.GiveDamage(damage, strength);
                else if (body.player)
                {
                    player.GiveDamage(damage, strength, damageType, true, lethalForPlayer);
                    hitPlayer = true;
                }
            }
            return hit;
        }

        static bool IsShapeOf(HplCharacterBody body, Collider c)
        {
            foreach (var s in body.Shapes) if (s == c) return true;
            return false;
        }

        static HplSurfaces.Surface StaticSurface(HplMap map, Collider c)
        {
            if (map.surfaces == null || c == null) return null;
            var pm = c.sharedMaterial;
            return map.surfaces.GetOrDefault(pm != null ? pm.name : null);
        }

        static void PlaySoundAt(HplMap map, string file, Vector3 position)
        {
            var sound = map.sound != null ? map.sound.CreateSoundEntity("HitSound", file, true) : null;
            if (sound != null) sound.Position = position;
        }

        /// <summary>cLuxAttackRayCallback: characters, non-colliding bodies and the hit body itself do not block.</summary>
        static bool AttackRayBlocked(Vector3 origin, Vector3 target, HitBody body)
        {
            Vector3 delta = target - origin;
            float length = delta.magnitude;
            if (length < 1e-6f) return false;
            int count = Physics.RaycastNonAlloc(origin, delta / length, Hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = Hits[i].collider;
                if (body.colliders.Contains(c)) continue;
                if (body.physics != null && c.attachedRigidbody != null && c.attachedRigidbody == body.physics.Rigidbody) continue;
                var data = HplPhysics.BodyOf(c);
                if (data != null && !data.collideCharacter && !data.collideNonCharacter) continue;
                return true;
            }
            return false;
        }
    }
}

using System.Collections.Generic;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// Collision queries the way HPL2 does them for characters (PhysicsWorld.cpp, CharacterBody.cpp):
    /// a shape is tested against the world at a given pose and the push-back vector is the per-axis
    /// largest push of all contacts. Bodies with CollideCharacter = false and triggers are ignored.
    /// Character shapes live on <see cref="CharacterLayer"/>, which the default query masks skip.
    /// </summary>
    public static class HplPhysics
    {
        /// <summary>Built-in "Ignore Raycast" layer: default raycasts and overlaps skip it, collisions don't.</summary>
        public const int CharacterLayer = 2;

        public delegate void CollisionCallback(Collider other, Vector3 normal, float depth);

        const int MaxPenetrationIterations = 4;

        static readonly Collider[] Overlaps = new Collider[128];
        static readonly RaycastHit[] Hits = new RaycastHit[64];
        static readonly Dictionary<Collider, AmnesiaBody> Bodies = new();

        /// <summary>The entity body a collider belongs to (null for static objects and level geometry).</summary>
        public static AmnesiaBody BodyOf(Collider c)
        {
            if (!Bodies.TryGetValue(c, out var body))
            {
                body = c.GetComponentInParent<AmnesiaBody>();
                Bodies[c] = body;
            }
            return body;
        }

        public static bool CollidesWithCharacter(Collider c)
        {
            var body = BodyOf(c);
            return body == null || body.collideCharacter;
        }

        /// <summary>
        /// iPhysicsWorld::CheckShapeWorldCollision: is <paramref name="shape"/> at this pose inside anything?
        /// PhysX returns one separation per collider; a collider is re-tested a few times so concave
        /// geometry (a corner of one mesh) gives the push of all its contacts, like Newton's contact list.
        /// </summary>
        public static bool CheckShapeWorldCollision(Collider shape, Vector3 position, Quaternion rotation, Vector3 halfExtents,
                                                    out Vector3 pushBack, CollisionCallback callback = null)
        {
            pushBack = Vector3.zero;
            bool collided = false;
            int count = Physics.OverlapBoxNonAlloc(position, halfExtents, Overlaps, rotation,
                                                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider other = Overlaps[i];
                if (other == shape || !CollidesWithCharacter(other)) continue;

                Transform t = other.transform;
                Vector3 total = Vector3.zero;
                for (int it = 0; it < MaxPenetrationIterations; it++)
                {
                    if (!Physics.ComputePenetration(shape, position + total, rotation, other, t.position, t.rotation,
                                                    out Vector3 direction, out float distance) || distance <= 0f)
                        break;
                    if (it == 0) callback?.Invoke(other, direction, distance);
                    total += direction * distance;
                }
                if (total == Vector3.zero) continue;
                if (total.x != 0f || total.z != 0f) total = LiftOverLowEdge(shape, position, rotation, other, total);

                collided = true;
                if (Mathf.Abs(pushBack.x) < Mathf.Abs(total.x)) pushBack.x = total.x;
                if (Mathf.Abs(pushBack.y) < Mathf.Abs(total.y)) pushBack.y = total.y;
                if (Mathf.Abs(pushBack.z) < Mathf.Abs(total.z)) pushBack.z = total.z;
            }
            return collided;
        }

        /// <summary>
        /// Port adjustment for edges at the bottom of the shape lower than <see cref="LowEdgeHeight"/>: a 1.7 cm
        /// carpet, a threshold. A PhysX mesh also pushes along the normal of such low side faces, and the character
        /// stops at the edge (the step climbing of CharacterBody.cpp only sees steps above 2.5 cm). Newton pushed the
        /// shape up onto them. When the shape is free of <paramref name="other"/> that little higher, the push is
        /// the lift that frees it (a binary search to 0.1 mm).
        /// </summary>
        static Vector3 LiftOverLowEdge(Collider shape, Vector3 position, Quaternion rotation, Collider other, Vector3 push)
        {
            Transform t = other.transform;
            bool Inside(float lift) => Physics.ComputePenetration(shape, position + new Vector3(0f, lift, 0f), rotation,
                                                                  other, t.position, t.rotation, out _, out float d) && d > 0f;
            if (Inside(LowEdgeHeight)) return push;
            float low = 0f, high = LowEdgeHeight;
            for (int i = 0; i < 9; i++)
            {
                float mid = (low + high) * 0.5f;
                if (Inside(mid)) low = mid;
                else high = mid;
            }
            return new Vector3(0f, high, 0f);
        }

        const float LowEdgeHeight = 0.03f;

        /// <summary>Closest hit along start → end that a character collides with (cCharacterBodyRay).</summary>
        public static bool Raycast(Vector3 start, Vector3 end, out float distance, out Vector3 normal, out Collider collider)
        {
            distance = float.MaxValue;
            normal = Vector3.up;
            collider = null;
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 1e-6f) return false;

            int count = Physics.RaycastNonAlloc(start, delta / length, Hits, length,
                                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit.distance >= distance || !CollidesWithCharacter(hit.collider)) continue;
                distance = hit.distance;
                normal = hit.normal;
                collider = hit.collider;
            }
            return collider != null;
        }
    }
}

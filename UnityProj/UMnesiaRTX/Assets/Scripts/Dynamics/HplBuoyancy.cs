using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Dynamics
{
    /// <summary>
    /// Newton's buoyancy (NewtonBodyAddBuoyancyForce → dgBody::AddBuoyancyForce, called by cPhysicsBodyNewton::
    /// OnUpdateCallback): the volume of the body's shapes below the surface plane and its centroid
    /// (dgCollision::CalculateVolumeIntegral, summed over a compound), the lift −gravity · density · volume at that
    /// centroid, and damping. The damping clamps of the original are GetMax(GetMin(x, 1), 10) and
    /// GetMax(GetMin(x, 0.25), 2), so they are always 10 and 2: the force loses 10 · velocity and the torque 2 · angular
    /// velocity, whatever the viscosities of the liquid. Bodies of 10 g and less (mass ≤ 0.01) get nothing.
    ///
    /// The submerged part is computed on triangle meshes of the colliders (boxes exactly, spheres and capsules as
    /// low polygon meshes, mesh colliders by their mesh): each triangle is clipped by the plane and closed into
    /// tetrahedra with an apex on the plane (the cap of the cut lies in the plane and adds no volume).
    /// </summary>
    public static class HplBuoyancy
    {
        const float LinearDamp = 10f, AngularDamp = 2f;

        /// <summary>The forces of one step on a body whose buoyancy is active (plane: y = <paramref name="surfaceY"/>).</summary>
        public static void Apply(HplPhysicsBody body, float surfaceY, float density)
        {
            var rb = body.Rigidbody;
            if (rb.isKinematic || rb.mass <= 0.01f) return;
            if (!SubmergedVolume(body.Colliders, surfaceY, out float volume, out Vector3 center)) return;

            Vector3 force = -Physics.gravity * (density * volume);
            Vector3 torque = Vector3.Cross(center - rb.worldCenterOfMass, force);
            force -= rb.linearVelocity * LinearDamp;
            torque -= rb.angularVelocity * AngularDamp;
            rb.AddForce(force, ForceMode.Force);
            rb.AddTorque(torque, ForceMode.Force);
        }

        /// <summary>The volume below y = surfaceY of the colliders and its centroid; false below 1e-4 m³ (Newton's limit).</summary>
        public static bool SubmergedVolume(IReadOnlyList<Collider> colliders, float surfaceY, out float volume, out Vector3 center)
        {
            volume = 0f;
            Vector3 moment = Vector3.zero;
            foreach (var c in colliders)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                var tris = LocalTriangles(c);
                if (tris == null) continue;
                Matrix4x4 m = c.transform.localToWorldMatrix;
                // The apex of the tetrahedra: a point of the plane under the collider.
                Vector3 apex = c.transform.position;
                apex.y = surfaceY;
                float v = 0f;
                Vector3 mo = Vector3.zero;
                for (int i = 0; i < tris.Length; i += 3)
                    ClipAndAccumulate(m.MultiplyPoint3x4(tris[i]), m.MultiplyPoint3x4(tris[i + 1]), m.MultiplyPoint3x4(tris[i + 2]),
                                      surfaceY, apex, ref v, ref mo);
                // The winding of a mesh gives the sign; each closed shape counts positive.
                if (v < 0f)
                {
                    v = -v;
                    mo = -mo;
                }
                volume += v;
                moment += mo;
            }
            center = volume > 0f ? moment / volume : Vector3.zero;
            return volume > 1e-4f;
        }

        static readonly Vector3[] Poly = new Vector3[4];

        static void ClipAndAccumulate(Vector3 a, Vector3 b, Vector3 c, float y, Vector3 apex, ref float volume, ref Vector3 moment)
        {
            // Sutherland-Hodgman against the half space below the plane: a triangle gives at most a quad.
            int n = 0;
            Vector3 prev = c;
            bool prevIn = c.y < y;
            for (int i = 0; i < 3; i++)
            {
                Vector3 cur = i == 0 ? a : i == 1 ? b : c;
                bool curIn = cur.y < y;
                if (curIn != prevIn)
                {
                    float t = (y - prev.y) / (cur.y - prev.y);
                    Poly[n++] = Vector3.Lerp(prev, cur, t);
                }
                if (curIn) Poly[n++] = cur;
                prev = cur;
                prevIn = curIn;
            }
            for (int i = 1; i + 1 < n; i++)
            {
                Vector3 p0 = Poly[0] - apex, p1 = Poly[i] - apex, p2 = Poly[i + 1] - apex;
                float v = Vector3.Dot(p0, Vector3.Cross(p1, p2)) / 6f;
                volume += v;
                moment += v * (apex + (p0 + p1 + p2) / 4f);
            }
        }

        static readonly Dictionary<Collider, Vector3[]> Cache = new();

        /// <summary>Triangles (three points each) of a collider in its transform's local space.</summary>
        static Vector3[] LocalTriangles(Collider c)
        {
            if (Cache.TryGetValue(c, out var tris)) return tris;
            switch (c)
            {
                case BoxCollider box: tris = Box(box.center, box.size); break;
                case SphereCollider sphere: tris = Ellipsoid(sphere.center, Vector3.one * sphere.radius, 0f, 1); break;
                case CapsuleCollider capsule:
                {
                    float half = Mathf.Max(0f, capsule.height * 0.5f - capsule.radius);
                    tris = Ellipsoid(capsule.center, Vector3.one * capsule.radius, half, capsule.direction);
                    break;
                }
                case MeshCollider mesh when mesh.sharedMesh != null && mesh.sharedMesh.isReadable:
                {
                    var vertices = mesh.sharedMesh.vertices;
                    var indices = mesh.sharedMesh.triangles;
                    tris = new Vector3[indices.Length];
                    for (int i = 0; i < indices.Length; i++) tris[i] = vertices[indices[i]];
                    break;
                }
                default:
                {
                    // Unreadable mesh: its bounds box.
                    var b = c is MeshCollider mc && mc.sharedMesh != null ? mc.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.zero);
                    tris = b.size == Vector3.zero ? null : Box(b.center, b.size);
                    break;
                }
            }
            Cache[c] = tris;
            return tris;
        }

        static Vector3[] Box(Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            var p = new Vector3[8];
            for (int i = 0; i < 8; i++)
                p[i] = center + new Vector3((i & 1) != 0 ? h.x : -h.x, (i & 2) != 0 ? h.y : -h.y, (i & 4) != 0 ? h.z : -h.z);
            int[] faces =
            {
                0, 2, 3, 1, // -z
                4, 5, 7, 6, // +z
                0, 1, 5, 4, // -y
                2, 6, 7, 3, // +y
                0, 4, 6, 2, // -x
                1, 3, 7, 5, // +x
            };
            var tris = new Vector3[36];
            for (int f = 0; f < 6; f++)
            {
                int q = f * 4, t = f * 6;
                tris[t] = p[faces[q]]; tris[t + 1] = p[faces[q + 1]]; tris[t + 2] = p[faces[q + 2]];
                tris[t + 3] = p[faces[q]]; tris[t + 4] = p[faces[q + 2]]; tris[t + 5] = p[faces[q + 3]];
            }
            return tris;
        }

        /// <summary>A latitude / longitude sphere (8 × 6), stretched along the axis by ±half (a capsule).</summary>
        static Vector3[] Ellipsoid(Vector3 center, Vector3 radius, float half, int direction)
        {
            const int segments = 8, rings = 6;
            var tris = new List<Vector3>();
            Vector3 Point(int ring, int seg)
            {
                float theta = Mathf.PI * ring / rings, phi = 2f * Mathf.PI * seg / segments;
                // Local frame with "up" along the axis.
                float s = Mathf.Sin(theta);
                Vector3 local = new(s * Mathf.Cos(phi), Mathf.Cos(theta), s * Mathf.Sin(phi));
                Vector3 p = Vector3.Scale(local, radius);
                p.y += local.y >= 0f ? half : -half;
                if (direction == 0) p = new Vector3(p.y, p.x, p.z);
                else if (direction == 2) p = new Vector3(p.x, p.z, p.y);
                return center + p;
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    Vector3 a = Point(r, s), b = Point(r, s + 1), c = Point(r + 1, s + 1), d = Point(r + 1, s);
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            return tris.ToArray();
        }

        /// <summary>The cache of shapes goes with the scene (colliders are destroyed with it).</summary>
        public static void ClearCache() => Cache.Clear();
    }
}

using System;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>Places the player next to a point of interest of the loaded map, looking at it.</summary>
    public static class HplDebugTeleport
    {
        public enum Result { Visible, Hidden, Failed }

        static readonly float[] Distances = { 0.5f, 0.8f, 1.2f, 1.7f, 2.4f, 3.2f };
        const int Angles = 16;

        /// <summary>
        /// Free standing spots on rings around the target bounds: the nearest one with a free line from the eye to the
        /// target; else the nearest free one (a closed container, a secret room). <paramref name="targetRoot"/>: its
        /// colliders do not hide the target.
        /// </summary>
        public static Result Find(HplMap map, Bounds target, Transform targetRoot, out Vector3 feet)
        {
            feet = default;
            var player = map.player;
            if (player == null || player.Body == null) return Result.Failed;
            Physics.SyncTransforms();
            Vector3 size = player.Body.Size;
            float radius = size.x * 0.5f, height = size.y;
            Vector3 center = target.center;
            // Large objects (shelves, pianos): the rings start outside their horizontal extent.
            float inner = Mathf.Min(2.5f, Mathf.Max(target.extents.x, target.extents.z)) + radius;
            bool hidden = false;
            Vector3 hiddenFeet = default;
            // From where the player stands now towards the target first, so a repeated teleport looks the same way.
            Vector3 from = player.Body.FeetPosition - center;
            float startAngle = Mathf.Atan2(from.x, from.z);
            foreach (float distance in Distances)
            {
                for (int i = 0; i < Angles; i++)
                {
                    float a = startAngle + (i % 2 == 0 ? i / 2 : -(i + 1) / 2) * (Mathf.PI * 2f / Angles);
                    Vector3 side = center + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (inner + distance);
                    if (!Floor(map, side, center.y, radius, height, out Vector3 spot)) continue;
                    Vector3 eye = spot + Vector3.up * (height * 0.9f);
                    if (Sees(map, eye, center, targetRoot))
                    {
                        feet = spot;
                        return Result.Visible;
                    }
                    if (!hidden) { hidden = true; hiddenFeet = spot; }
                }
            }
            feet = hiddenFeet;
            return hidden ? Result.Hidden : Result.Failed;
        }

        static bool Floor(HplMap map, Vector3 at, float targetY, float radius, float height, out Vector3 feet)
        {
            feet = default;
            var hits = Physics.RaycastAll(new Vector3(at.x, targetY + 1.2f, at.z), Vector3.down, 8f,
                                          Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.normal.y < 0.7f || hit.point.y > targetY + 0.4f || IsPlayer(map, hit.collider)) continue;
                // Loose props are no floor: a table top or a book would put the player on the furniture.
                if (hit.collider.attachedRigidbody != null && !hit.collider.attachedRigidbody.isKinematic) continue;
                Vector3 spot = hit.point + Vector3.up * 0.05f;
                if (!Free(map, spot, radius, height)) continue;
                feet = spot;
                return true;
            }
            return false;
        }

        static bool Free(HplMap map, Vector3 feet, float radius, float height)
        {
            Vector3 low = feet + Vector3.up * (radius + 0.05f), high = feet + Vector3.up * Mathf.Max(radius + 0.05f, height - radius);
            foreach (var c in Physics.OverlapCapsule(low, high, radius * 0.95f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!IsPlayer(map, c) && c.gameObject.layer != Dynamics.HplPhysicsWorld.NoCollideLayer) return false;
            return true;
        }

        static bool Sees(HplMap map, Vector3 eye, Vector3 point, Transform targetRoot)
        {
            Vector3 dir = point - eye;
            float length = dir.magnitude;
            if (length < 0.01f) return true;
            foreach (var hit in Physics.RaycastAll(eye, dir / length, length - 0.15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsPlayer(map, hit.collider) || hit.collider.gameObject.layer == Dynamics.HplPhysicsWorld.NoCollideLayer) continue;
                if (targetRoot != null && hit.collider.transform.IsChildOf(targetRoot)) continue;
                return false;
            }
            return true;
        }

        static bool IsPlayer(HplMap map, Collider collider)
        {
            var shapes = map.player.Body.Shapes;
            for (int i = 0; i < shapes.Count; i++)
                if (shapes[i] == collider) return true;
            return collider.transform.IsChildOf(map.player.transform);
        }

        /// <summary>Feet at the spot, the view on <paramref name="lookAt"/>.</summary>
        public static void Place(AmnesiaPlayer player, Vector3 feet, Vector3 lookAt)
        {
            Vector3 dir = lookAt - (feet + Vector3.up * (player.Body.Size.y * 0.9f));
            float yaw = Mathf.Atan2(dir.x, dir.z);
            float pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude);
            player.DebugPlace(feet, yaw, pitch);
        }

        public static void PlaceAtStart(AmnesiaPlayer player, AmnesiaArea area) => player.PlaceAtStart(area);
    }
}

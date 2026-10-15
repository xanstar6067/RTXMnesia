using UnityEngine;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// HPL2 (right-handed, Y up) → Unity (left-handed, Y up). Unity's Collada import mirrors X, so all
    /// placement data from .map/.ent files is mirrored the same way: position (x, y, z) → (-x, y, z),
    /// rotation matrix M → S·M·S with S = diag(-1, 1, 1). Triangle winding stays as is.
    ///
    /// The mirror maps both ways, so every conversion here is its own inverse. Polar vectors (positions,
    /// directions, velocities, forces) are mirrored; axial vectors (angular velocity, torque, rotation axes
    /// with their angle) become (x, -y, -z), because a mirror reverses the sense of rotation.
    /// </summary>
    public static class HplSpace
    {
        public static Vector3 Position(Vector3 hpl) => new Vector3(-hpl.x, hpl.y, hpl.z);

        public static Vector3 Direction(Vector3 hpl) => new Vector3(-hpl.x, hpl.y, hpl.z);

        /// <summary>Angular velocity / torque between the spaces (either way).</summary>
        public static Vector3 Axial(Vector3 v) => new Vector3(v.x, -v.y, -v.z);

        /// <summary>A rotation between the spaces (either way): S·R·S.</summary>
        public static Quaternion Mirror(Quaternion q) => new Quaternion(q.x, -q.y, -q.z, q.w);

        /// <summary>A transform matrix between the spaces (either way): S·M·S.</summary>
        public static Matrix4x4 Mirror(Matrix4x4 m)
        {
            m.m01 = -m.m01; m.m02 = -m.m02; m.m03 = -m.m03;
            m.m10 = -m.m10; m.m20 = -m.m20; m.m30 = -m.m30;
            return m;
        }

        /// <summary>The HPL2 matrix of a Unity pose (no scale).</summary>
        public static Matrix4x4 ToHpl(Vector3 position, Quaternion rotation) =>
            Mirror(Matrix4x4.TRS(position, rotation, Vector3.one));

        /// <summary>
        /// HPL2 Euler angles in radians, order XYZ (Math.cpp MatrixRotate: M = Rz·Ry·Rx).
        /// Mirroring keeps the X angle and negates Y and Z.
        /// </summary>
        public static Quaternion Rotation(Vector3 hplEulerRadians)
        {
            const float deg = Mathf.Rad2Deg;
            return Quaternion.AngleAxis(-hplEulerRadians.z * deg, Vector3.forward)
                   * Quaternion.AngleAxis(-hplEulerRadians.y * deg, Vector3.up)
                   * Quaternion.AngleAxis(hplEulerRadians.x * deg, Vector3.right);
        }

        /// <summary>HPL2 lights and cameras look down their local -Z, Unity ones down +Z.</summary>
        public static Quaternion LookRotation(Vector3 hplEulerRadians) =>
            Rotation(hplEulerRadians) * Quaternion.Euler(0f, 180f, 0f);

        /// <summary>HPL2 texture coordinates have v = 0 at the top of the image, Unity at the bottom.</summary>
        public static Vector2 Uv(Vector2 hpl) => new Vector2(hpl.x, 1f - hpl.y);
    }
}

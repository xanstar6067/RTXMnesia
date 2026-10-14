using System;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>
    /// Port of cLuxPlayerLookAt (LuxPlayerHelpers.cpp): turns the camera towards a point (StartPlayerLookAt).
    /// The math runs in HPL2 terms, because the speed limit is one-sided (Min only) and so not mirror-symmetric:
    /// HPL2 yaw = π − our yaw, HPL2 pitch = −our pitch, positions mirrored in x.
    /// </summary>
    public class PlayerLookAt
    {
        readonly AmnesiaPlayer _player;
        Vector3 _target = Vector3.up;
        float _speedMul = 1f, _maxSpeed = 9999f;
        Vector2 _currentSpeed;
        string _atTargetCallback = "";

        public bool Active { get; private set; }

        /// <summary>Runs every step while the camera is within the target (the script function name).</summary>
        public event Action<string> AtTarget;

        public PlayerLookAt(AmnesiaPlayer player) => _player = player;

        public void SetTarget(Vector3 target, float speedMul, float maxSpeed, string atTargetCallback)
        {
            _target = target;
            _speedMul = speedMul;
            _maxSpeed = maxSpeed;
            _atTargetCallback = atTargetCallback ?? "";
        }

        public void SetActive(bool active)
        {
            Active = active;
            if (!active) _currentSpeed = Vector2.zero;
        }

        public void Update(float timeStep)
        {
            if (!Active) return;

            Vector3 goal = GetAngleFromPoints3D(HplSpace.Position(_player.Body.CameraPosition), HplSpace.Position(_target));
            float pitch = -_player.Pitch;
            float yaw = Mathf.PI - _player.Yaw;

            float distPitch = GetAngleDistanceRad(pitch, goal.x);
            float distYaw = GetAngleDistanceRad(yaw, goal.y);

            var wanted = new Vector2(Mathf.Min(distPitch * _speedMul, _maxSpeed), Mathf.Min(distYaw * _speedMul, _maxSpeed));
            _currentSpeed += (wanted - _currentSpeed) * timeStep * 10f;

            // Camera AddPitch / AddYaw in HPL2 terms (with the camera's pitch limits), body yaw follows.
            _player.AddLook(-_currentSpeed.y * timeStep, -_currentSpeed.x * timeStep);

            if (distPitch * distPitch + distYaw * distYaw < 0.01f && _atTargetCallback != "")
                AtTarget?.Invoke(_atTargetCallback);
        }

        // ------------------------------------------------------------------ cMath (Math.cpp), HPL2 space

        static float GetAngleFromPoints2D(Vector2 start, Vector2 goal)
        {
            float dx = goal.x - start.x;
            float dy = goal.y - start.y;
            if (dx == 0f) dx = 0.00001f;
            if (dy == 0f) dy = 0.00001f;

            if (dx >= 0f && dy < 0f) return Mathf.Atan(dx / -dy);
            if (dx >= 0f && dy >= 0f) return Mathf.Atan(dy / dx) + Mathf.PI * 0.5f;
            if (dx < 0f && dy >= 0f) return Mathf.Atan(-dx / dy) + Mathf.PI;
            return Mathf.Atan(-dy / -dx) + Mathf.PI * 0.5f + Mathf.PI;
        }

        static float GetAngleFromPoints2DSimple(Vector2 delta)
        {
            const float epsilon = 0.0001f; // kEpsilonf
            if (delta.x == 0f) delta.x = -epsilon;
            if (delta.y == 0f) delta.y = epsilon;
            if (delta.x <= 0f) return -Mathf.Atan(delta.y / delta.x);
            return -1000f; // "Error in GetAngle 3D code!" in the original
        }

        /// <summary>cMath::GetAngleFromPoints3D: x = pitch, y = yaw of the direction start → goal.</summary>
        static Vector3 GetAngleFromPoints3D(Vector3 start, Vector3 goal)
        {
            var angle = Vector3.zero;
            angle.y = -GetAngleFromPoints2D(new Vector2(start.x, start.z), new Vector2(goal.x, goal.z));

            // MatrixRotateY(-angle.y) × delta (right-handed rotation about Y).
            float a = -angle.y;
            Vector3 d = goal - start;
            float rotatedZ = -Mathf.Sin(a) * d.x + Mathf.Cos(a) * d.z;
            angle.x = Wrap(GetAngleFromPoints2DSimple(new Vector2(rotatedZ, d.y)), 0f, Mathf.PI * 2f);
            return angle;
        }

        static float Wrap(float x, float min, float max)
        {
            if (x >= min && x <= max) return x;
            max -= min;
            float offset = min;
            x -= offset;
            float numOfMax = Mathf.Floor(Mathf.Abs(x / max));
            if (x >= max) x -= numOfMax * max;
            else if (x < 0f) x = (numOfMax + 1f) * max + x;
            return x + offset;
        }

        static float GetAngleDistanceRad(float angle1, float angle2)
        {
            const float max = Mathf.PI * 2f;
            angle1 = Wrap(angle1, 0f, max);
            angle2 = Wrap(angle2, 0f, max);
            if (angle1 == angle2) return 0f;
            float dist1 = angle2 - angle1;
            float dist2 = max - Mathf.Abs(dist1);
            if (dist1 > 0f) dist2 = -dist2;
            return Mathf.Abs(dist1) < Mathf.Abs(dist2) ? dist1 : dist2;
        }
    }
}

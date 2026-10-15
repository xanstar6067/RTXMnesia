using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// The parts of HPL2 cMath (math/Math.cpp) that gameplay code uses on matrices, for ports that compute in HPL2
    /// space (see <see cref="HplSpace"/> to convert). cMatrixf and Unity's Matrix4x4 have the same layout:
    /// m[row][col], column vectors, translation in column 3. GetRight / GetUp / GetForward return rows, like
    /// cMatrixf (for a rotation, the rows of its inverse are its axes).
    /// </summary>
    public static class HplMatrix
    {
        public const float Epsilon = 0.0001f; // kEpsilonf

        public static Matrix4x4 RotateX(float a)
        {
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            var m = Matrix4x4.identity;
            m.m11 = c; m.m12 = -s;
            m.m21 = s; m.m22 = c;
            return m;
        }

        public static Matrix4x4 RotateY(float a)
        {
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            var m = Matrix4x4.identity;
            m.m00 = c; m.m02 = s;
            m.m20 = -s; m.m22 = c;
            return m;
        }

        public static Matrix4x4 RotateZ(float a)
        {
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            var m = Matrix4x4.identity;
            m.m00 = c; m.m01 = -s;
            m.m10 = s; m.m11 = c;
            return m;
        }

        /// <summary>cMath::MatrixRotate(v, eEulerRotationOrder_XYZ) = Rz·Ry·Rx.</summary>
        public static Matrix4x4 RotateXYZ(Vector3 v) => RotateZ(v.z) * RotateY(v.y) * RotateX(v.x);

        public static Matrix4x4 Translate(Vector3 v) => Matrix4x4.Translate(v);

        public static Vector3 GetRight(Matrix4x4 m) => new(m.m00, m.m01, m.m02);
        public static Vector3 GetUp(Matrix4x4 m) => new(m.m10, m.m11, m.m12);
        public static Vector3 GetForward(Matrix4x4 m) => new(m.m20, m.m21, m.m22);
        public static Vector3 GetTranslation(Matrix4x4 m) => new(m.m03, m.m13, m.m23);

        public static Matrix4x4 SetTranslation(Matrix4x4 m, Vector3 t)
        {
            m.m03 = t.x; m.m13 = t.y; m.m23 = t.z;
            return m;
        }

        /// <summary>cMatrixf::GetRotation: the 3x3 part, no translation.</summary>
        public static Matrix4x4 GetRotation(Matrix4x4 m) => SetTranslation(m, Vector3.zero);

        /// <summary>cMath::MatrixMul(m, v): a point.</summary>
        public static Vector3 Mul(Matrix4x4 m, Vector3 v) => m.MultiplyPoint3x4(v);

        /// <summary>cMath::MatrixMul3x3(m, v): a direction.</summary>
        public static Vector3 Mul3x3(Matrix4x4 m, Vector3 v) => m.MultiplyVector(v);

        /// <summary>cMath::Vector3Angle (unit vectors).</summary>
        public static float Vector3Angle(Vector3 a, Vector3 b)
        {
            float cos = Vector3.Dot(a, b);
            if (Mathf.Abs(cos - 1f) <= Epsilon) return 0f;
            return Mathf.Acos(cos);
        }

        public static Vector3 Vector3MaxLength(Vector3 v, float maxLength)
        {
            float length = v.magnitude;
            return length > maxLength ? v / length * maxLength : v;
        }

        /// <summary>cMath::MatrixToEulerAngles, order XYZ.</summary>
        public static Vector3 ToEulerAngles(Matrix4x4 m) =>
            new(Mathf.Atan2(m.m21, m.m22), -Mathf.Asin(Mathf.Clamp(m.m20, -1f, 1f)), Mathf.Atan2(m.m10, m.m00));

        /// <summary>
        /// cMath::MatrixEulerAngleDistance: the X, Y, Z rotations (in that order, local to B) that turn A's forward
        /// and up onto B's, expressed in B's frame. Kept as in Math.cpp.
        /// </summary>
        public static Vector3 EulerAngleDistance(Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 localA = b.transpose * a;
            Matrix4x4 localATrans = localA.transpose;
            Vector3 fwd = GetForward(localATrans);
            Vector3 up = GetUp(localATrans);
            Vector3 output = Vector3.zero;

            if (Mathf.Abs(fwd.y) < 0.0001f && Mathf.Abs(fwd.z) < 0.0001f) output.x = 0f;
            else
            {
                var yz = new Vector2(fwd.y, fwd.z).normalized;
                output.x = Mathf.Acos(Mathf.Clamp(yz.y, -1f, 1f));
                if (yz.x < 0f) output.x = -output.x;
            }

            Matrix4x4 rot = RotateX(output.x);
            fwd = Mul3x3(rot, fwd);

            if (Mathf.Abs(fwd.x) < 0.0001f && Mathf.Abs(fwd.z) < 0.0001f) output.y = 0f;
            else
            {
                var xz = new Vector2(fwd.x, fwd.z).normalized;
                output.y = Mathf.Acos(Mathf.Clamp(xz.y, -1f, 1f));
                if (xz.x > 0f) output.y = -output.y;
            }

            rot = RotateY(output.y) * rot;
            up = Mul3x3(rot, up);

            if (Mathf.Abs(up.y) < 0.0001f && Mathf.Abs(up.x) < 0.0001f) output.z = 0f;
            else
            {
                var xy = new Vector2(up.x, up.y).normalized;
                output.z = Mathf.Acos(Mathf.Clamp(xy.y, -1f, 1f));
                if (xy.x < 0f) output.z = -output.z;
            }

            return Mul3x3(b, output);
        }

        /// <summary>cMath::MatrixSlerp (shortest path): position lerped, rotation slerped.</summary>
        public static Matrix4x4 Slerp(float t, Matrix4x4 a, Matrix4x4 b)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            Vector3 pos = GetTranslation(a) * (1f - t) + GetTranslation(b) * t;
            Quaternion q = Quaternion.Slerp(a.rotation, b.rotation, t);
            return SetTranslation(Matrix4x4.Rotate(q), pos);
        }

        /// <summary>cMath::Wrap.</summary>
        public static float Wrap(float x, float min, float max)
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

        /// <summary>cMath::GetAngleDistanceRad: the shortest signed angle from a1 to a2.</summary>
        public static float AngleDistanceRad(float angle1, float angle2)
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

    /// <summary>cPidController (math/PidController.h): output = p·error + i·∑(error·dt) over the last N errors + d·Δerror/dt.</summary>
    public sealed class HplPid
    {
        public float p, i, d;

        readonly List<Vector3> _errors = new();
        readonly List<float> _timeSteps = new();
        int _errorNum, _lastNum = -1;

        public HplPid(float p, float i, float d, int errorNum)
        {
            this.p = p;
            this.i = i;
            this.d = d;
            SetErrorNum(errorNum);
        }

        public void SetErrorNum(int count)
        {
            while (_errors.Count < count) _errors.Add(Vector3.zero);
            while (_timeSteps.Count < count) _timeSteps.Add(0f);
            if (_errors.Count > count) _errors.RemoveRange(count, _errors.Count - count);
            if (_timeSteps.Count > count) _timeSteps.RemoveRange(count, _timeSteps.Count - count);
        }

        /// <summary>Like the original, old errors stay but their time steps are cleared.</summary>
        public void Reset()
        {
            _errorNum = 0;
            _lastNum = -1;
            for (int k = 0; k < _timeSteps.Count; k++) _timeSteps[k] = 0f;
        }

        public Vector3 Output(Vector3 error, float timeStep)
        {
            _errors[_errorNum] = error;
            _timeSteps[_errorNum] = timeStep;

            Vector3 integral = Vector3.zero;
            for (int k = 0; k < _errors.Count; k++) integral += _errors[k] * _timeSteps[k];

            Vector3 derivative = Vector3.zero;
            if (_lastNum >= 0) derivative = (_errors[_errorNum] - _errors[_lastNum]) / timeStep;

            _lastNum = _errorNum;
            _errorNum++;
            if (_errorNum >= _errors.Count) _errorNum = 0;

            return _errors[_lastNum] * p + integral * i + derivative * d;
        }
    }
}

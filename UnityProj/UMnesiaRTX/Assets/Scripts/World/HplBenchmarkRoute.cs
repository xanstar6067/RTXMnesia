using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>Authored camera and look-at keys, in HPL metres. Linear segments with eased corner tangents.</summary>
    public sealed class HplBenchmarkRoute
    {
        public readonly struct Key
        {
            public readonly float time;
            public readonly Vector3 eye, target;
            public Key(float t, Vector3 p, Vector3 look) { time = t; eye = p; target = look; }
        }
        public readonly string map, title;
        public readonly Key[] keys;
        public float Duration => keys[^1].time;
        public string ScenePath => "Assets/AmnesiaImported/Levels/maps/main/" + map + ".unity";
        HplBenchmarkRoute(string map, string title, params Key[] keys)
        { this.map = map; this.title = title; this.keys = keys; }

        // Shape-preserving Hermite interpolation: zero tangent at a reversal, no overshoot in narrow passages.
        static float Tangent(float a, float b) => a * b <= 0 ? 0 : 2 * a * b / (a + b);
        Vector3 Sample(int i, float t, bool look)
        {
            Vector3 At(int n) => look ? keys[n].target : keys[n].eye;
            float dt = keys[i + 1].time - keys[i].time;
            Vector3 a = At(i), b = At(i + 1), v = (b - a) / dt;
            Vector3 prev = i == 0 ? v : (a - At(i - 1)) / (keys[i].time - keys[i - 1].time);
            Vector3 next = i + 2 == keys.Length ? v : (At(i + 2) - b) / (keys[i + 2].time - keys[i + 1].time);
            Vector3 m0 = new(Tangent(prev.x, v.x), Tangent(prev.y, v.y), Tangent(prev.z, v.z));
            Vector3 m1 = new(Tangent(v.x, next.x), Tangent(v.y, next.y), Tangent(v.z, next.z));
            float u = Mathf.Clamp01((t - keys[i].time) / dt), u2 = u * u, u3 = u2 * u;
            return (2 * u3 - 3 * u2 + 1) * a + (u3 - 2 * u2 + u) * dt * m0
                + (-2 * u3 + 3 * u2) * b + (u3 - u2) * dt * m1;
        }
        public void Evaluate(float time, out Vector3 eye, out Vector3 target)
        {
            int i = 0;
            while (i < keys.Length - 2 && time > keys[i + 1].time) i++;
            eye = Sample(i, time, false); target = Sample(i, time, true);
        }
        static Key K(float t, float x, float y, float z, float lx, float ly, float lz) =>
            new(t, new Vector3(x, y, z), new Vector3(lx, ly, lz));

        public static readonly HplBenchmarkRoute[] All =
        {
            new("ch01/09_back_hall", "Зал с фонтаном",
                K(0, 5.75f,-2.3f,9, 5.75f,-1.5f,26.5f),
                K(7, 5.75f,-2.3f,18, 5.75f,-1.5f,26.5f),
                K(14, 2.5f,-2.1f,23, 5.75f,-1.5f,26.5f),
                K(21, 2.5f,-1.5f,28, 5.75f,-1,26.5f),
                K(26, 6,-1,29.5f, 5.75f,-1.7f,22),
                K(30, 9,-0.8f,27, 5.75f,-1.7f,18)),
            new("ch01/01_old_archives", "Старые архивы: печь",
                K(0, 33,-0.4f,15, 29.5f,-0.8f,18.5f),
                K(7, 32,-0.4f,19, 25.5f,-0.8f,23.5f),
                K(14, 30,-0.4f,23, 25.5f,-0.8f,23.5f),
                K(24, 28,-0.4f,23, 25.5f,-0.8f,23.5f),
                K(30, 30,-0.3f,24.5f, 35,1,23),
                K(35, 32,-0.3f,23, 26,-0.6f,23)),
            new("ch01/03_archives", "Архивы",
                K(0, 59.5f,1.6f,-1, 63.4f,1,-3),
                K(5, 59.5f,1.6f,4, 58.25f,1.5f,9),
                K(8, 58.25f,1.6f,6.75f, 58.5f,1.6f,14),
                K(11, 58.25f,1.6f,10, 66.5f,1.6f,10),
                K(15, 66.5f,1.6f,10, 66.5f,2,21),
                K(21, 66.5f,1.7f,21.25f, 70,2,26),
                K(25, 66.5f,1.7f,24, 75,2,26)),
            new("ch01/13_machine_room", "Машинное отделение",
                K(0, 24,-6.3f,20, 33,-5.8f,21),
                K(8, 27,-6.3f,21.5f, 33.5f,-5.8f,21),
                K(15, 28,-6.3f,18.5f, 33.5f,-6.5f,16),
                K(21, 29,-6.5f,15, 33.6f,-7,15.6f),
                K(25, 28,-6,13, 33,-6.3f,19)),
            new("ch02/18_cistern", "Цистерна",
                K(0, -5.75f,1.1f,30.25f, -16,3,35),
                K(8, -12,1.5f,32, -20.25f,4,35),
                K(17, -17.4f,4.2f,30.8f, -20.25f,6,35),
                K(24, -23,4.4f,32, -20,5,40),
                K(30, -20,5,32, -12,3,35)),
            new("ch02/24_torture_choir_east", "Хор",
                K(0, 0,1.7f,1, 0,4,15),
                K(8, -1.8f,1.7f,8, 0,2,13),
                K(13, -1.8f,1.7f,12, 0,1.7f,16),
                K(18, -1.8f,1.7f,17, 0,1.7f,18),
                K(22, -1.8f,1.7f,23, 0,1.7f,21),
                K(28, -6,2.5f,26, -11,5,35),
                K(35, -9,4,30, -20,5,45)),
        };
    }
}

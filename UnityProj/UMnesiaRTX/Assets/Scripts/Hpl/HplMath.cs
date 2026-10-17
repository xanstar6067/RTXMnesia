using System;
using System.Globalization;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// cMath / cString helpers that level scripts depend on exactly: random numbers like rand() (0..32767, as
    /// RandRectf/RandRectl in Math.cpp) and number ↔ text the way script variables are stored (String.cpp).
    /// </summary>
    public static class HplMath
    {
        const int RandMax = 32767;
        static Random _random = new();

        /// <summary>Restart the random sequence (for repeatable test runs).</summary>
        public static void Seed(int seed) => _random = new Random(seed);

        static int Rand() => _random.Next(0, RandMax + 1);

        /// <summary>cMath::RandRectf: min + rand()/RAND_MAX × (max − min), both ends included.</summary>
        public static float RandRectf(float min, float max) => min + (float)Rand() / RandMax * (max - min);

        /// <summary>cMath::RandRectl: rand() % (max − min + 1) + min.</summary>
        public static int RandRectl(int min, int max) => Rand() % (max - min + 1) + min;

        /// <summary>cMath::Modulus: |a| − |b|·floor(|a / b|).</summary>
        public static float Modulus(float dividend, float divisor)
        {
            float num = MathF.Floor(MathF.Abs(dividend / divisor));
            return MathF.Abs(dividend) - MathF.Abs(divisor) * num;
        }

        /// <summary>atoi: optional sign and leading digits, anything else stops the number ("0.5" → 0).</summary>
        public static int Atoi(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int i = 0;
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            bool negative = false;
            if (i < s.Length && (s[i] == '+' || s[i] == '-')) negative = s[i++] == '-';
            long value = 0;
            while (i < s.Length && s[i] >= '0' && s[i] <= '9' && value < int.MaxValue) value = value * 10 + (s[i++] - '0');
            return (int)Math.Clamp(negative ? -value : value, int.MinValue, int.MaxValue);
        }

        /// <summary>atof: the longest numeric prefix, invariant culture (0 if there is none).</summary>
        public static float Atof(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0f;
            s = s.TrimStart();
            for (int length = s.Length; length > 0; length--)
                if (float.TryParse(s.Substring(0, length), NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    return f;
            return 0f;
        }

        /// <summary>cString::ToString(int): "%d".</summary>
        public static string ToString(int x) => x.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// cString::ToString(float): "%.20f", fixed point (so atoi of a stored float reads its integer part) and
        /// precise enough that atof gives the same float back.
        /// </summary>
        public static string ToString(float x) => ((double)x).ToString("F20", CultureInfo.InvariantCulture);
    }
}

using UnityEngine;

namespace VRMarionette
{
    public static class Utils
    {
        public static float NormalizeTo180(float angle)
        {
            return Mod(angle + 180f, 360) - 180f;
        }

        // 正の値である剰余を求める
        private static float Mod(float a, float b)
        {
            // a % b は負の値になる場合がある
            return (a % b + b) % b;
        }

        public static Vector3 ParseVector(string value)
        {
            var values = value.Trim('(', ')').Split(",");
            return new Vector3(
                float.Parse(values[0]),
                float.Parse(values[1]),
                float.Parse(values[2])
            );
        }
    }
}
using UnityEngine;

namespace VRMarionette
{
    public static class Vector3Extensions
    {
        /// <summary>
        /// ベクトル V をベクトル U に射影したベクトルを求める。
        /// </summary>
        public static Vector3 ProjectOnto(this Vector3 v, Vector3 u)
        {
            return Vector3.Dot(v, u) / u.sqrMagnitude * u;
        }

        /// <summary>
        /// ベクトル V のベクトル U に対する直交射影したベクトルを求める。
        /// つまり、ベクトル V のうち ベクトル U に直交する成分を得る。
        /// </summary>
        public static Vector3 OrthogonalComponent(this Vector3 v, Vector3 u)
        {
            return v - ProjectOnto(v, u);
        }
    }
}
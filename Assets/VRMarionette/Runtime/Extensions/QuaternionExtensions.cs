using System;
using UnityEngine;

namespace VRMarionette
{
    public static class QuaternionExtensions
    {
        /// <summary>
        /// ToRotationWithAxis の逆変換を行い Quaternion を Vector3 に変換する
        /// 2 つの軸回りの回転が 0 であれば残りの 1 軸回りの回転は (-180, 180) の範囲で逆変換可能
        /// 全ての軸回りの回転が (-90, 90) の範囲内であれば逆変換可能
        /// いずれの範囲も開区間
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="axisDirection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static Vector3 ToEulerAnglesWithAxis(this Quaternion rotation, Direction axisDirection)
        {
            var angles = Vector3.zero;

            // axis 回りの回転量を最後に求め、他の軸周りの回転量は Y,X,Z の(Unityの実装に依存する)順番に求める
            Vector3 v;
            switch (axisDirection)
            {
                case Direction.XAxis:
                    v = rotation * Vector3.right;
                    angles.y = v.AngleYForXAxis();
                    if (Mathf.Approximately(Mathf.Abs(angles.y), 180f)) angles.y = 0;
                    v = Quaternion.Inverse(Quaternion.Euler(angles)) * rotation * Vector3.right;
                    angles.z = v.AngleZForXAxis();
                    v = Quaternion.Inverse(Quaternion.Euler(angles)) * rotation * Vector3.up;
                    angles.x = v.AngleXForXAxis();
                    break;
                case Direction.YAxis:
                    v = rotation * Vector3.up;
                    angles.x = v.AngleXForYAxis();
                    if (Mathf.Approximately(Mathf.Abs(angles.x), 180f)) angles.x = 0;
                    v = Quaternion.Inverse(Quaternion.Euler(angles)) * rotation * Vector3.up;
                    angles.z = v.AngleZForYAxis();
                    v = Quaternion.Inverse(Quaternion.Euler(angles)) * rotation * Vector3.forward;
                    angles.y = v.AngleYForYAxis();
                    break;
                case Direction.ZAxis:
                    v = rotation * Vector3.forward;
                    angles.y = v.AngleYForZAxis();
                    if (Mathf.Approximately(Mathf.Abs(angles.y), 180f)) angles.y = 0;
                    v = Quaternion.Inverse(Quaternion.Euler(angles)) * rotation * Vector3.forward;
                    angles.x = v.AngleXForZAxis();
                    v = Quaternion.Inverse(Quaternion.Euler(angles)) * rotation * Vector3.right;
                    angles.z = v.AngleZForZAxis();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axisDirection), axisDirection, null);
            }

            return angles;
        }

        public static Quaternion ExtractAxisRotation(this Quaternion q, Direction axisDirection)
        {
            var axis = axisDirection.ToAxis();
            var tiltRotation = Quaternion.FromToRotation(axis, q * axis);
            return Quaternion.Inverse(tiltRotation) * q;
        }
    }
}
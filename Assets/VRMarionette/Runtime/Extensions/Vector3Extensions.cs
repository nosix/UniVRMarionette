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

        /// <summary>
        /// ベクトル V からベクトル U に回転する場合の回転角度を求める。
        /// </summary>
        /// <param name="v">回転前のベクトル</param>
        /// <param name="u">回転後のベクトル</param>
        /// <param name="axis">回転軸を表す単位ベクトル</param>
        /// <returns>-180 から 180 の間の角度</returns>
        public static float AngleTo(this Vector3 v, Vector3 u, Vector3 axis)
        {
            var cross = Vector3.Cross(v, u);
            var direction = Vector3.Dot(cross, axis);
            return Vector3.Angle(v, v + u) * (direction >= 0 ? 1 : -1);
        }

        /// <summary>
        /// 指定した回転軸を動かした後に軸回りの回転を行う回転を得る
        /// Euler で得た回転とは異なる
        /// </summary>
        /// <param name="angle">オイラー角</param>
        /// <param name="axisDirection">回転軸の方向</param>
        /// <returns></returns>
        public static Quaternion ToRotationWithAxis(this Vector3 angle, Direction axisDirection)
        {
            var axis = axisDirection.ToAxis();
            var rotation = Quaternion.Euler(
                angle.x * (1f - axis.x),
                angle.y * (1f - axis.y),
                angle.z * (1f - axis.z)
            );
            var axisRotation = rotation * axis;
            return Quaternion.AngleAxis(Vector3.Dot(angle, axis), axisRotation) * rotation;
        }

        /// <summary>
        /// YZ 平面上における Y 軸とベクトル V の間の角度を得ることで X 軸回りの角度を得る
        /// </summary>
        public static float AngleXForXAxis(this Vector3 v)
        {
            return Mathf.Atan2(v.z, v.y) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// XZ 平面上における X 軸とベクトル V の間の角度を得ることで Y 軸回りの角度を得る
        /// </summary>
        public static float AngleYForXAxis(this Vector3 v)
        {
            return Mathf.Atan2(-v.z, v.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// XY 平面上における X 軸とベクトル V の間の角度を得ることで Z 軸回りの角度を得る
        /// </summary>
        public static float AngleZForXAxis(this Vector3 v)
        {
            return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// YZ 平面上における Y 軸とベクトル V の間の角度を得ることで X 軸回りの角度を得る
        /// </summary>
        public static float AngleXForYAxis(this Vector3 v)
        {
            return Mathf.Atan2(v.z, v.y) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// XZ 平面上における Z 軸とベクトル V の間の角度を得ることで Y 軸回りの角度を得る
        /// </summary>
        public static float AngleYForYAxis(this Vector3 v)
        {
            return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// XY 平面上における Y 軸とベクトル V の間の角度を得ることで Z 軸回りの角度を得る
        /// </summary>
        public static float AngleZForYAxis(this Vector3 v)
        {
            return Mathf.Atan2(-v.x, v.y) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// YZ 平面上における Z 軸とベクトル V の間の角度を得ることで X 軸回りの角度を得る
        /// </summary>
        public static float AngleXForZAxis(this Vector3 v)
        {
            return Mathf.Atan2(-v.y, v.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// XZ 平面上における Z 軸とベクトル V の間の角度を得ることで Y 軸回りの角度を得る
        /// </summary>
        public static float AngleYForZAxis(this Vector3 v)
        {
            return Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// XY 平面上における X 軸とベクトル V の間の角度を得ることで Z 軸回りの角度を得る
        /// </summary>
        public static float AngleZForZAxis(this Vector3 v)
        {
            return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        }

        public static float MagnitudeForXAxis(this Vector3 v)
        {
            return Mathf.Sqrt(v.y * v.y + v.z * v.z);
        }

        public static float MagnitudeForYAxis(this Vector3 v)
        {
            return Mathf.Sqrt(v.x * v.x + v.z * v.z);
        }

        public static float MagnitudeForZAxis(this Vector3 v)
        {
            return Mathf.Sqrt(v.x * v.x + v.y * v.y);
        }
    }
}
using UnityEngine;

namespace VRMarionette
{
    public static class CapsuleColliderExtensions
    {
        public static bool HasCollision(this CapsuleCollider capsule, SphereCollider sphere)
        {
            var capsuleTransform = capsule.transform;
            var sphereTransform = sphere.transform;
            var capsuleAxis = capsule.direction.ToDirection().ToAxis();
            var halfHeight = capsule.height / 2f - capsule.radius;
            Vector3 p, c;
            if (halfHeight > 0)
            {
                // CapsuleCollider の端点 e1, e2 を求める
                var center = capsule.center;
                var localE1 = center + halfHeight * capsuleAxis;
                var localE2 = center - halfHeight * capsuleAxis;
                var e1 = capsuleTransform.TransformPoint(localE1);
                var e2 = capsuleTransform.TransformPoint(localE2);
                // 線分 e2 - e1 上の点で点 c に最も近い点 p を求める
                c = sphereTransform.TransformPoint(sphere.center);
                var u = e1 - e2;
                var v = c - e2;
                var projectOnU = Vector3.Dot(v, u) / u.sqrMagnitude;
                p = Mathf.Clamp01(projectOnU) * u + e2;
            }
            else
            {
                // CapsuleCollider が球形になっているので、それぞれの球の中心点を求める
                c = sphereTransform.TransformPoint(sphere.center);
                p = capsuleTransform.TransformPoint(capsule.center);
            }

            // Capsule の軸方向の scale を 0 として、残りの軸の scale の平均を求める
            var capsuleHorizontalScale = Vector3.Scale(Vector3.one - capsuleAxis, capsuleTransform.lossyScale);
            var capsuleScale = (capsuleHorizontalScale.x + capsuleHorizontalScale.y + capsuleHorizontalScale.z) / 2f;

            // Sphere の scale は平均を求める
            var sphereLossyScale = sphereTransform.lossyScale;
            var sphereScale = (sphereLossyScale.x + sphereLossyScale.y + sphereLossyScale.z) / 3f;

            // 点 p と点 c の距離が 2 つの半径の和より近ければ衝突している
            return Vector3.Distance(p, c) <= capsule.radius * capsuleScale + sphere.radius * sphereScale;
        }
    }
}
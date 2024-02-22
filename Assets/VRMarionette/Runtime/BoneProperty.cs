using UnityEngine;

namespace VRMarionette
{
    public class BoneProperty
    {
        public Transform Transform { get; }
        public HumanBodyBones Bone { get; }
        public HumanLimit Limit { get; }
        public CapsuleCollider Collider { get; }
        public BoneGroupSpec GroupSpec { get; }

        public bool HasCollider => Collider is not null;

        public BoneProperty(
            Transform transform,
            HumanBodyBones bone,
            HumanLimit limit,
            CapsuleCollider collider,
            BoneGroupSpec groupSpec
        )
        {
            Transform = transform;
            Bone = bone;
            Limit = limit;
            Collider = collider;
            GroupSpec = groupSpec;
        }

        public Vector3 GetBottomPosition()
        {
            if (!HasCollider) return Transform.position;
            var length = Collider.height - 2f * Collider.radius;
            var halfLength = length > 0 ? length / 2f : 0f;
            var axisDirection = Collider.direction.ToDirection().ToAxis();
            var centerOffset = Collider.center;
            var localP1 = centerOffset + halfLength * axisDirection;
            var localP2 = centerOffset - halfLength * axisDirection;
            var p1 = Transform.TransformPoint(localP1);
            var p2 = Transform.TransformPoint(localP2);
            var p = p1.y < p2.y ? p1 : p2;
            p.y -= Collider.radius;
            return p;
        }

        public ForceEvent CreateForceEvent(bool hold, Vector3 forcePoint)
        {
            return new ForceEvent
            {
                Bone = Bone,
                Hold = hold,
                Distance = GetPositionInCollider(forcePoint),
                AxisDirection = HasCollider ? Collider.direction.ToDirection() : Direction.YAxis
            };
        }

        /// <summary>
        /// Collider 内における位置を得る
        /// </summary>
        /// <param name="worldPosition">この点が Collider 内でどの位置にあるかを調べる</param>
        /// <returns>
        /// 関節に近い Collider の端を 0f とする。
        /// 軸方向以外は Collider の境界を 1f もしくは -1f とする。
        /// 軸方向は関節から遠い Collider の端を 1f とする。
        /// Collider の境界外の場合は 1f より大きく、-1f より小さい場合もありえる。
        /// </returns>
        private Vector3 GetPositionInCollider(Vector3 worldPosition)
        {
            if (!HasCollider) return Vector3.zero;

            var localPosition = Transform.InverseTransformPoint(worldPosition);
            var axis = Collider.direction.ToDirection().ToAxis();
            var radius = Collider.radius;
            var center = Collider.center;
            var halfLength = Mathf.Max(Collider.height / 2f, radius);
            var sign = Vector3.Dot(axis, center) > 0 ? 1 : -1;
            localPosition -= center - sign * halfLength * axis;
            var colliderSize = sign * 2f * halfLength * axis + radius * (Vector3.one - axis);
            return new Vector3(
                localPosition.x / colliderSize.x,
                localPosition.y / colliderSize.y,
                localPosition.z / colliderSize.z
            );
        }

        /// <summary>
        /// BoneProperty.FindOrigin で得た sourceLocalPosition の位置をワールド座標に変換する。
        /// sourceLocalPosition の位置は骨が連結している場合に骨を一直線に伸ばした時の相対位置になっている。
        /// </summary>
        /// <param name="targetTransform">連結した骨の末端側の関節</param>
        /// <param name="originTransform">連結した骨の根元の関節</param>
        /// <param name="sourceLocalPosition">力の発生源の位置(根元の関節基準の位置)</param>
        /// <returns>力の発生源の位置(ワールド座標)</returns>
        public static Vector3 ToWorldPosition(
            Transform targetTransform,
            Transform originTransform,
            Vector3 sourceLocalPosition
        )
        {
            var t = targetTransform;
            while (t != originTransform)
            {
                var parentTransform = t.parent;
                sourceLocalPosition -= parentTransform.InverseTransformPoint(t.position);
                t = parentTransform;
            }

            return targetTransform.TransformPoint(sourceLocalPosition);
        }
    }
}
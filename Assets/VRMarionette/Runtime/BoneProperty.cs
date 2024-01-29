using UnityEngine;

namespace VRMarionette
{
    public class BoneProperty
    {
        public Transform Transform { get; }
        public HumanBodyBones Bone { get; }
        public HumanLimit Limit { get; }
        public CapsuleCollider Collider { get; }

        public bool HasCollider => Collider is not null;

        private readonly BoneProperties _properties;

        public BoneProperty(
            Transform transform,
            HumanBodyBones bone,
            HumanLimit limit,
            CapsuleCollider collider,
            BoneProperties properties
        )
        {
            Transform = transform;
            Bone = bone;
            Limit = limit;
            Collider = collider;
            _properties = properties;
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

        /// <summary>
        /// 骨が連結して回転する場合の根元の骨の関節を探す。
        /// 根元の骨の関節が OriginTransform になり、
        /// 関節の位置を原点としたときの力の発生源の位置を SourceLocalPosition として保持する。
        /// </summary>
        /// <param name="targetTransform">連結した骨の末端側の関節</param>
        /// <param name="sourceLocalPosition">力の発生源の位置(末端側の関節基準の位置を受け取り、根元の関節基準の位置を返す)</param>
        /// <returns>根元の関節</returns>
        public Transform FindOrigin(Transform targetTransform, ref Vector3 sourceLocalPosition)
        {
            var originTransform = targetTransform;
            while (true)
            {
                var originProperty = _properties.Get(originTransform);
                if (!originProperty.HasCollider || !IsLinked(originProperty.Bone)) break;
                var parentTransform = originTransform.parent;
                sourceLocalPosition += parentTransform.InverseTransformPoint(originTransform.position);
                originTransform = parentTransform;
            }

            return originTransform;
        }

        /// <summary>
        /// 対象としている骨と指定した骨が連結していることを調べる
        /// </summary>
        /// <param name="bone">連結していることを調べる骨</param>
        /// <returns>対象の骨と指定した骨が連結しているなら true を返す</returns>
        public bool IsLinked(HumanBodyBones bone)
        {
            // TODO BoneGroups の情報で代用できないか？
            switch (Bone)
            {
                case HumanBodyBones.Spine:
                case HumanBodyBones.Chest:
                    return bone is HumanBodyBones.Spine or HumanBodyBones.Chest;
                case HumanBodyBones.Neck:
                case HumanBodyBones.Head:
                    return bone is HumanBodyBones.Neck or HumanBodyBones.Head;
                case HumanBodyBones.LeftShoulder:
                case HumanBodyBones.LeftUpperArm:
                    return bone is HumanBodyBones.LeftShoulder or HumanBodyBones.LeftUpperArm;
                case HumanBodyBones.RightShoulder:
                case HumanBodyBones.RightUpperArm:
                    return bone is HumanBodyBones.RightShoulder or HumanBodyBones.RightUpperArm;
                default:
                    return false;
            }
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
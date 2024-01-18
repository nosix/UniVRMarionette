using UnityEngine;

namespace VRMarionette
{
    public class BoneProperty
    {
        public Transform Transform { get; }
        public HumanBodyBones Bone { get; }
        public CapsuleCollider Collider { get; }

        public bool HasCollider => Collider is not null;

        private readonly BoneProperties _properties;

        public BoneProperty(
            Transform transform,
            HumanBodyBones bone,
            CapsuleCollider collider,
            BoneProperties properties
        )
        {
            Transform = transform;
            Bone = bone;
            Collider = collider;
            _properties = properties;
        }

        public Vector3 ToBottomPosition(Vector3 position)
        {
            position.y -= Collider.radius;
            return position;
        }

        public Vector3 ToPosition(Vector3 bottomPosition)
        {
            bottomPosition.y += Collider.radius;
            return bottomPosition;
        }

        /// <summary>
        /// 骨が連結して回転する場合の根元の骨の関節を探す。
        /// 根元の骨の関節が OriginTransform になり、
        /// 関節の位置を原点としたときの力の発生源の位置を SourceLocalPosition として保持する。
        /// </summary>
        /// <param name="originTransform">連結した骨の末端側の関節を受け取り、根元の関節を返す</param>
        /// <param name="sourceLocalPosition">力の発生源の位置(末端側の関節基準の位置を受け取り、根元の関節基準の位置を返す)</param>
        public void FindOrigin(ref Transform originTransform, ref Vector3 sourceLocalPosition)
        {
            while (true)
            {
                var originProperty = _properties.Get(originTransform);
                if (!originProperty.HasCollider || !IsLinked(originProperty.Bone)) break;
                var parentTransform = originTransform.parent;
                sourceLocalPosition += parentTransform.InverseTransformPoint(originTransform.position);
                originTransform = parentTransform;
            }
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
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    public class HumanoidBones
    {
        public Transform RootTransform { get; }
        public BoneProperties BoneProperties { get; }

        private readonly IReadOnlyDictionary<HumanBodyBones, BoneGroupProperty> _boneGroupProperties;

        public HumanoidBones(
            Animator animator,
            HumanoidManipulator manipulator,
            ForceFieldContainer forceFields,
            Transform rootTransform
        )
        {
            RootTransform = rootTransform;

            var bonePropertiesBuilder = new BoneProperties.Builder(manipulator.BoneGroups);

            CreateColliderForEachBones(
                animator,
                manipulator.HumanLimits,
                forceFields.forceFields.ToDictionary(e => e.bone, e => e),
                bonePropertiesBuilder
            );

            BoneProperties = bonePropertiesBuilder.Build();

            // Collider が存在する Bone はその Bone を対象とする BoneGroup を構成する
            var boneGroupProperties = BoneProperties
                .Where(e => e.HasCollider)
                .ToDictionary(
                    boneProperty => boneProperty.Bone,
                    boneProperty => new BoneGroupProperty(boneProperty, BoneProperties)
                );

            // Collider が存在しない Bone はその Bone の親の BoneGroup を適用する
            foreach (var boneProperty in BoneProperties.Where(e => !e.HasCollider))
            {
                var targetProperty = FindTarget(boneProperty, BoneProperties);
                boneGroupProperties.Add(boneProperty.Bone, boneGroupProperties[targetProperty.Bone]);
            }

            _boneGroupProperties = boneGroupProperties;
        }

        private static void CreateColliderForEachBones(
            Animator animator,
            HumanLimits humanLimits,
            IReadOnlyDictionary<HumanBodyBones, ForceField> forceFields,
            BoneProperties.Builder bonePropertiesBuilder
        )
        {
            for (HumanBodyBones bone = 0; bone < HumanBodyBones.LastBone; bone++)
            {
                var boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform is null) continue;

                var humanLimit = humanLimits.Get(bone);
                var forceField = forceFields.GetValueOrDefault(bone);

                float boneLength;
                CapsuleCollider capsule;

                if (forceField is null || humanLimit is null)
                {
                    // Property だけ登録して Collider は作らない
                    CalculateBoneLength(
                        bone,
                        boneTransform,
                        animator,
                        out boneLength
                    );
                    bonePropertiesBuilder.Add(
                        boneTransform,
                        bone,
                        boneLength,
                        humanLimit,
                        collider: null,
                        isAxisAligned: null
                    );
                    continue;
                }

                if (!CalculateBoneLength(bone, boneTransform, animator, out boneLength))
                {
                    var colliderCenterOffset =
                        Vector3.Scale(forceField.centerOffset, humanLimit.axis.ToAxis())
                            .magnitude;
                    boneLength = colliderCenterOffset * 2f;
                }

                // 頭は末端になるので特別に処理する
                if (bone == HumanBodyBones.Head)
                {
                    capsule = CreateHeadCapsule(
                        boneTransform,
                        humanLimit,
                        forceField,
                        boneLength
                    );
                    bonePropertiesBuilder.Add(
                        boneTransform,
                        bone,
                        boneLength,
                        humanLimit,
                        capsule,
                        forceField.isAxisAligned
                    );
                    continue;
                }

                capsule = CreateCapsule(
                    boneTransform,
                    humanLimit,
                    forceField,
                    boneLength
                );
                bonePropertiesBuilder.Add(
                    boneTransform,
                    bone,
                    boneLength,
                    humanLimit,
                    capsule,
                    forceField.isAxisAligned
                );
            }
        }

        private static CapsuleCollider CreateCapsule(
            Transform headBoneTransform,
            HumanLimit humanLimit,
            ForceField forceField,
            float height
        )
        {
            var center =
                (forceField.isAxisAligned ? 1 : -1) * height / 2f * humanLimit.axis.ToAxis()
                + forceField.centerOffset;
            return AddCapsuleCollider(
                headBoneTransform.gameObject,
                center,
                height,
                forceField.radius,
                humanLimit.axis
            );
        }

        private static CapsuleCollider CreateHeadCapsule(
            Transform boneTransform,
            HumanLimit humanLimit,
            ForceField forceField,
            float height
        )
        {
            return AddCapsuleCollider(
                boneTransform.gameObject,
                forceField.centerOffset,
                height,
                forceField.radius,
                humanLimit.axis
            );
        }

        private static CapsuleCollider AddCapsuleCollider(
            GameObject target,
            Vector3 center,
            float height,
            float radius,
            Direction direction
        )
        {
            var capsule = target.AddComponent<CapsuleCollider>();
            capsule.center = center;
            capsule.height = height;
            capsule.radius = radius;
            capsule.direction = (int)direction;
            return capsule;
        }

        /// <summary>
        /// 骨の長さを計算する
        /// </summary>
        /// <param name="bone">計算対象の骨</param>
        /// <param name="headTransform">計算対象の骨の根元</param>
        /// <param name="animator">関係する骨を得るために使う</param>
        /// <param name="boneLength">計算結果の骨の長さ(計算できなかった場合は 0)</param>
        /// <returns>骨の長さを計算できた場合は true</returns>
        /// <exception cref="InvalidOperationException">想定外の状態になっている場合に発生する</exception>
        private static bool CalculateBoneLength(
            HumanBodyBones bone,
            Transform headTransform,
            Animator animator,
            out float boneLength
        )
        {
            boneLength = 0f;

            Transform tailTransform = null;
            switch (bone)
            {
                // 必ず１つの子が存在する
                case HumanBodyBones.LeftUpperLeg:
                case HumanBodyBones.RightUpperLeg:
                case HumanBodyBones.LeftLowerLeg:
                case HumanBodyBones.RightLowerLeg:
                case HumanBodyBones.Neck:
                case HumanBodyBones.LeftShoulder:
                case HumanBodyBones.RightShoulder:
                case HumanBodyBones.LeftUpperArm:
                case HumanBodyBones.RightUpperArm:
                case HumanBodyBones.LeftLowerArm:
                case HumanBodyBones.RightLowerArm:
                {
                    if (headTransform.childCount != 1) throw new InvalidOperationException();
                    tailTransform = headTransform.GetChild(0);
                    break;
                }
                // 子(Toes)が存在しない場合がある
                case HumanBodyBones.LeftFoot:
                case HumanBodyBones.RightFoot:
                {
                    if (headTransform.childCount > 1) throw new InvalidOperationException();
                    tailTransform = headTransform.GetChild(0);
                    break;
                }
                // Chest, UpperChest, Neck が存在しない場合がある
                case HumanBodyBones.Spine:
                    tailTransform = animator.GetBoneTransform(HumanBodyBones.Chest) ??
                                    animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                                    animator.GetBoneTransform(HumanBodyBones.Neck) ??
                                    animator.GetBoneTransform(HumanBodyBones.Head);
                    break;
                // UpperChest, Neck が存在しない場合がある
                case HumanBodyBones.Chest:
                    tailTransform = animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                                    animator.GetBoneTransform(HumanBodyBones.Neck) ??
                                    animator.GetBoneTransform(HumanBodyBones.Head);
                    break;
                // Neck が存在しない場合がある
                case HumanBodyBones.UpperChest:
                    tailTransform = animator.GetBoneTransform(HumanBodyBones.Neck) ??
                                    animator.GetBoneTransform(HumanBodyBones.Head);
                    break;
                // 子が存在しない
                case HumanBodyBones.Head:
                    break;
                // 子(指)が複数存在する
                case HumanBodyBones.LeftHand:
                    tailTransform = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal) ??
                                    throw new InvalidOperationException();
                    break;
                case HumanBodyBones.RightHand:
                    tailTransform = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal) ??
                                    throw new InvalidOperationException();
                    break;
                default:
                    // 対象外の骨は長さ 0 とする
                    return true;
            }

            // 計算対象だが末端になる骨が無い場合は計算不可
            if (tailTransform is null) return false;

            // 末端となる骨との距離を返す
            boneLength = Vector3.Distance(headTransform.position, tailTransform.position);
            return true;
        }

        /// <summary>
        /// Collider を持たない場合は Target になれないので、親の Target を探す
        /// </summary>
        /// <param name="targetProperty">検査対象の始点 Target</param>
        /// <param name="properties">BoneProperty のコンテナ</param>
        /// <returns></returns>
        private static BoneProperty FindTarget(BoneProperty targetProperty, BoneProperties properties)
        {
            while (targetProperty.Bone != HumanBodyBones.Hips && !targetProperty.HasCollider)
            {
                targetProperty = properties.Get(targetProperty.Transform.parent);
            }

            return targetProperty;
        }

        public BoneGroupProperty GetBoneGroupProperty(HumanBodyBones bone)
        {
            return _boneGroupProperties.GetValueOrDefault(bone);
        }

        public BoneProperty Next(BoneGroupProperty targetGroup)
        {
            var nextTargetTransform = targetGroup.OriginTransform.parent;
            return nextTargetTransform != RootTransform
                ? BoneProperties.Get(nextTargetTransform)
                : null;
        }
    }
}
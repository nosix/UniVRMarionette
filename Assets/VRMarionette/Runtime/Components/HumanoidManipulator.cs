using System;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    /// <summary>
    /// Humanoid の骨の回転を操作するクラス。
    /// 回転では正方向(角度が正の値)と負方向(角度が負の値)と区別している。
    /// それぞれの方向で回転角度の限界が設けられている。
    /// 正方向の限界は max で設定し、負方向の限界は min で設定している。
    /// 限界の設定値は HumanLimitContainer が保持する。
    /// </summary>
    public class HumanoidManipulator : MonoBehaviour
    {
        public HumanLimits HumanLimits { private set; get; }

        private Animator _animator;
        private BoneGroups _boneGroups;

        public void Initialize(HumanLimitContainer humanLimits)
        {
            if (humanLimits is null)
            {
                throw new InvalidOperationException(
                    "The HumanoidManipulator component requires the HumanLimitContainer object.");
            }

            _animator = GetComponent<Animator>() ?? throw new InvalidOperationException(
                "The HumanoidManipulator component requires the Animator component.");

            HumanLimits = new HumanLimits(humanLimits);
            _boneGroups = new BoneGroups(HumanLimits);
        }

        /// <summary>
        /// 骨を回転させる。
        /// 現在の骨の回転角度に引数で指定した角度が加えられる。
        /// 角度は-180度から180度の範囲に変換されて SetBoneRotation に渡される。
        /// </summary>
        /// <param name="bone">回転対象の骨</param>
        /// <param name="angle">角度</param>
        /// <returns>回転した角度</returns>
        public Vector3 Rotate(HumanBodyBones bone, Vector3 angle)
        {
            var currentAngles = GetBoneRotation(bone);
            var nextAngles = currentAngles + NormalizeAngles(angle);
            return SetBoneRotation(bone, nextAngles);
        }

        /// <summary>
        /// 骨の回転角度を指定した角度に設定する。
        /// 回転の限界角度が正方向回転と負方向回転にそれぞれ設けられている。
        /// そのため、-180度と+180度は異なる回転になる。
        /// </summary>
        /// <param name="bone">回転対象の骨</param>
        /// <param name="angle">角度</param>
        /// <exception cref="ArgumentOutOfRangeException">存在しない骨を指定した</exception>
        /// <returns>回転した角度</returns>
        public Vector3 SetBoneRotation(HumanBodyBones bone, Vector3 angle)
        {
            return GetBoneCategory(bone, out var groupId) switch
            {
                BoneCategory.Independent => SetLocalEulerAngle(bone, angle),
                BoneCategory.Grouped => SetBoneGroupRotation(groupId, angle),
                BoneCategory.NotSupported => Vector3.zero,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// 骨の回転角度を取得する。
        /// </summary>
        /// <param name="bone">取得対象の骨</param>
        /// <returns>-180度から180度の間の角度</returns>
        /// <exception cref="ArgumentOutOfRangeException">存在しない骨を指定した</exception>
        public Vector3 GetBoneRotation(HumanBodyBones bone)
        {
            return GetBoneCategory(bone, out var groupId) switch
            {
                BoneCategory.Independent => GetLocalEulerAngle(bone),
                BoneCategory.Grouped => GetBoneGroupRotation(groupId),
                BoneCategory.NotSupported => Vector3.zero,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// 骨の分類(独立している/グループ化されている/操作対象外)を返す。
        /// </summary>
        /// <param name="bone">分類を調べる対象</param>
        /// <param name="groupId">グループ化されている場合はそのグループのID</param>
        /// <exception cref="ArgumentOutOfRangeException">boneが不正な場合</exception>
        private static BoneCategory GetBoneCategory(HumanBodyBones bone, out BoneGroups.Id groupId)
        {
            groupId = BoneGroups.Id.Nothing;

            switch (bone)
            {
                case HumanBodyBones.Hips:
                    break;
                case HumanBodyBones.Spine:
                case HumanBodyBones.Chest:
                    // 2つの部位は x,y,z のいずれも連動して動く
                    groupId = BoneGroups.Id.Body;
                    break;
                case HumanBodyBones.UpperChest:
                    break;
                case HumanBodyBones.Neck:
                case HumanBodyBones.Head:
                    // 2つの部位は x,y,z のいずれも連動して動く
                    groupId = BoneGroups.Id.Neck;
                    break;
                case HumanBodyBones.LeftShoulder:
                case HumanBodyBones.LeftUpperArm:
                    // 2つの部位は x,y,z のいずれも連動して動く
                    groupId = BoneGroups.Id.LeftShoulder;
                    break;
                case HumanBodyBones.RightShoulder:
                case HumanBodyBones.RightUpperArm:
                    // 2つの部位は x,y,z のいずれも連動して動く
                    groupId = BoneGroups.Id.RightShoulder;
                    break;
                case HumanBodyBones.LeftLowerArm:
                case HumanBodyBones.LeftHand:
                case HumanBodyBones.RightLowerArm:
                case HumanBodyBones.RightHand:
                    break;
                case HumanBodyBones.LeftThumbProximal:
                case HumanBodyBones.LeftThumbIntermediate:
                    // 2つの部位は x,y,z のいずれも連動して動く
                    groupId = BoneGroups.Id.LeftThumb;
                    break;
                case HumanBodyBones.RightThumbProximal:
                case HumanBodyBones.RightThumbIntermediate:
                    // 2つの部位は x,y,z のいずれも連動して動く
                    groupId = BoneGroups.Id.RightThumb;
                    break;
                case HumanBodyBones.LeftThumbDistal:
                case HumanBodyBones.RightThumbDistal:
                    break;
                case HumanBodyBones.LeftIndexProximal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.LeftIndexProximal;
                    break;
                case HumanBodyBones.LeftMiddleProximal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.LeftMiddleProximal;
                    break;
                case HumanBodyBones.LeftRingProximal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.LeftRingProximal;
                    break;
                case HumanBodyBones.LeftLittleProximal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.LeftLittleProximal;
                    break;
                case HumanBodyBones.RightIndexProximal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.RightIndexProximal;
                    break;
                case HumanBodyBones.RightMiddleProximal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.RightMiddleProximal;
                    break;
                case HumanBodyBones.RightRingProximal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.RightRingProximal;
                    break;
                case HumanBodyBones.RightLittleProximal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.RightLittleProximal;
                    break;
                case HumanBodyBones.LeftIndexIntermediate:
                case HumanBodyBones.LeftMiddleIntermediate:
                case HumanBodyBones.LeftRingIntermediate:
                case HumanBodyBones.LeftLittleIntermediate:
                case HumanBodyBones.RightIndexIntermediate:
                case HumanBodyBones.RightMiddleIntermediate:
                case HumanBodyBones.RightRingIntermediate:
                case HumanBodyBones.RightLittleIntermediate:
                    // z のみ動き、単独で動く
                    break;
                case HumanBodyBones.LeftIndexDistal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.LeftIndexDistal;
                    break;
                case HumanBodyBones.LeftMiddleDistal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.LeftMiddleDistal;
                    break;
                case HumanBodyBones.LeftRingDistal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.LeftRingDistal;
                    break;
                case HumanBodyBones.LeftLittleDistal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.LeftLittleDistal;
                    break;
                case HumanBodyBones.RightIndexDistal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.RightIndexDistal;
                    break;
                case HumanBodyBones.RightMiddleDistal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.RightMiddleDistal;
                    break;
                case HumanBodyBones.RightRingDistal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.RightRingDistal;
                    break;
                case HumanBodyBones.RightLittleDistal:
                    // z のみ動き、Intermediate が連動する
                    groupId = BoneGroups.Id.RightLittleDistal;
                    break;
                case HumanBodyBones.LeftUpperLeg:
                case HumanBodyBones.LeftLowerLeg:
                case HumanBodyBones.LeftFoot:
                case HumanBodyBones.RightUpperLeg:
                case HumanBodyBones.RightLowerLeg:
                case HumanBodyBones.RightFoot:
                case HumanBodyBones.LeftToes:
                case HumanBodyBones.RightToes:
                    break;
                case HumanBodyBones.LeftEye:
                case HumanBodyBones.RightEye:
                case HumanBodyBones.Jaw:
                case HumanBodyBones.LastBone:
                    return BoneCategory.NotSupported;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bone), bone, null);
            }

            return groupId == BoneGroups.Id.Nothing ? BoneCategory.Independent : BoneCategory.Grouped;
        }

        /// <summary>
        /// BoneGroup に含まれる全ての骨の回転角度を設定する。
        /// </summary>
        /// <param name="group">複数の骨をまとめたグループ</param>
        /// <param name="angle">設定する回転角度</param>
        /// <returns>回転した角度</returns>
        private Vector3 SetBoneGroupRotation(BoneGroups.Id group, Vector3 angle)
        {
            var groupSpec = _boneGroups.GetSpec(group);

            var rotatedAngle = Vector3.zero;

            foreach (var (bone, ratio) in groupSpec.Ratios)
            {
                rotatedAngle += SetLocalEulerAngle(bone, ratio.Apply(angle));
            }

            return rotatedAngle;
        }

        /// <summary>
        /// 骨の localEulerAngles を変更する。
        /// </summary>
        /// <param name="bone">変更対象の骨</param>
        /// <param name="angle">localEulerAngles に設定する回転角度</param>
        /// <returns>回転した角度</returns>
        private Vector3 SetLocalEulerAngle(HumanBodyBones bone, Vector3 angle)
        {
            var boneTransform = _animator.GetBoneTransform(bone);
            if (boneTransform is null) return Vector3.zero;
            var clampedAngle = HumanLimits.ClampAngle(bone, angle);
            var rotation = HumanLimits.ToRotation(bone, clampedAngle);
            var localRotation = GetLocalEulerAngle(bone);
            boneTransform.localRotation = rotation;
            return GetLocalEulerAngle(bone) - localRotation;
        }

        private Vector3 GetBoneGroupRotation(BoneGroups.Id group)
        {
            var groupSpec = _boneGroups.GetSpec(group);

            return groupSpec.Ratios.Keys
                .Aggregate(Vector3.zero, (current, bone) => current + GetLocalEulerAngle(bone));
        }

        private Vector3 GetLocalEulerAngle(HumanBodyBones bone)
        {
            var boneTransform = _animator.GetBoneTransform(bone);
            if (boneTransform is null) return Vector3.zero;
            var angle = HumanLimits.ToEulerAngles(bone, boneTransform.localRotation);
            return angle;
        }

        private static Vector3 NormalizeAngles(Vector3 angles)
        {
            return new Vector3(
                NormalizeAngle(angles.x),
                NormalizeAngle(angles.y),
                NormalizeAngle(angles.z)
            );
        }

        private static float NormalizeAngle(float angle)
        {
            return Mod(angle + 180f, 360) - 180f;
        }

        // 正の値である剰余を求める
        private static float Mod(float a, float b)
        {
            // a % b は負の値になる場合がある
            return (a % b + b) % b;
        }

        private enum BoneCategory
        {
            Independent,
            Grouped,
            NotSupported
        }
    }
}
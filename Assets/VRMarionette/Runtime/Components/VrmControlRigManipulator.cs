using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace VRMarionette
{
    /// <summary>
    /// Control Rig に含まれる骨の回転を操作するクラス。
    /// 回転では正方向(角度が正の値)と負方向(角度が負の値)と区別している。
    /// それぞれの方向で回転角度の限界が設けられている。
    /// 正方向の限界は max で設定し、負方向の限界は min で設定している。
    /// 限界の設定値は HumanLimitContainer が保持する。
    /// </summary>
    public class VrmControlRigManipulator : MonoBehaviour
    {
        public bool withEulerAnglesCache = true;

        private Animator _animator;
        private HumanLimits _humanLimits;
        private BoneGroups _boneGroups;
        private EulerAngleAccessor _eulerAngleAccessor;
        private EulerAngleAccessorWithCache _eulerAngleAccessorWithCache;

        public void Initialize(Vrm10Instance instance, HumanLimitContainer humanLimits)
        {
            _animator = instance.GetComponent<Animator>();
            if (!_animator)
            {
                throw new InvalidOperationException("The VrmInstance has not Animator component.");
            }

            _humanLimits = new HumanLimits(humanLimits);
            _boneGroups = new BoneGroups(_humanLimits);
            _eulerAngleAccessor = new EulerAngleAccessor(_animator, _humanLimits);
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
            return GetEulerAngleAccessor().SetLocalEulerAngle(bone, angle);
        }

        private Vector3 GetBoneGroupRotation(BoneGroups.Id group)
        {
            var groupSpec = _boneGroups.GetSpec(group);

            return groupSpec.Ratios.Keys
                .Aggregate(Vector3.zero, (current, bone) => current + GetLocalEulerAngle(bone));
        }

        private Vector3 GetLocalEulerAngle(HumanBodyBones bone)
        {
            return GetEulerAngleAccessor().GetLocalEulerAngle(bone);
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

        private IEulerAngleAccessor GetEulerAngleAccessor()
        {
            _eulerAngleAccessorWithCache = withEulerAnglesCache switch
            {
                true when _eulerAngleAccessorWithCache is null =>
                    new EulerAngleAccessorWithCache(_animator, _humanLimits, _eulerAngleAccessor),
                false when _eulerAngleAccessorWithCache is not null =>
                    null,
                _ => _eulerAngleAccessorWithCache
            };

            return withEulerAnglesCache ? _eulerAngleAccessorWithCache : _eulerAngleAccessor;
        }

        private interface IEulerAngleAccessor
        {
            public Vector3 SetLocalEulerAngle(HumanBodyBones bone, Vector3 angle);
            public Vector3 GetLocalEulerAngle(HumanBodyBones bone);
        }

        /// <summary>
        /// HumanLimits の制限範囲が広い Bone で不正確な値になることはあるが
        /// Bone の rotation をそのまま使用するので
        /// VrmControlRigManipulator を通さずに rotation を変更した場合でも
        /// ある程度は動作する
        /// (Hips, UpperLeg, Hand で不正確な値になる)
        /// </summary>
        private class EulerAngleAccessor : IEulerAngleAccessor
        {
            private readonly Animator _animator;
            private readonly HumanLimits _humanLimits;

            public EulerAngleAccessor(Animator animator, HumanLimits humanLimits)
            {
                _animator = animator;
                _humanLimits = humanLimits;
            }

            public Vector3 SetLocalEulerAngle(HumanBodyBones bone, Vector3 angle)
            {
                var boneTransform = _animator.GetBoneTransform(bone);
                if (!boneTransform) return Vector3.zero;
                var nextAngle = _humanLimits.ClampAngle(bone, angle);
                var rotatedAngle = nextAngle - GetLocalEulerAngle(bone);
                boneTransform.localEulerAngles = nextAngle;
                return rotatedAngle;
            }

            public Vector3 GetLocalEulerAngle(HumanBodyBones bone)
            {
                var boneTransform = _animator.GetBoneTransform(bone);
                if (!boneTransform) return Vector3.zero;
                // 一つのクォータニオンに対して複数のオイラー角があり得るので候補の中から最適なオイラー角を選ぶ
                // 正規化した角度を制限範囲内に補正した上で差分が最も小さかったオイラー角を最適と判断する
                var candidate1 = boneTransform.localEulerAngles;
                var candidate2 = new Vector3(180f - candidate1.x, candidate1.y + 180f, candidate1.z + 180f);
                var normalized1 = NormalizeAngles(candidate1);
                var normalized2 = NormalizeAngles(candidate2);
                var clamped1 = _humanLimits.ClampAngle(bone, normalized1);
                var clamped2 = _humanLimits.ClampAngle(bone, normalized2);
                return (normalized1 - clamped1).magnitude > (normalized2 - clamped2).magnitude
                    ? normalized2
                    : normalized1;
            }
        }

        /// <summary>
        /// Bone の localEulerAngles を別途キャッシュすることで正確さを維持する
        /// 但し、VrmControlRigManipulator を通さずに rotation を変更すると
        /// キャッシュを破棄して不正確な値になることがある
        /// </summary>
        private class EulerAngleAccessorWithCache : IEulerAngleAccessor
        {
            private readonly Animator _animator;
            private readonly HumanLimits _humanLimits;
            private readonly EulerAngleAccessor _accessor;

            private readonly IDictionary<HumanBodyBones, Vector3> _localEulerAngles
                = new Dictionary<HumanBodyBones, Vector3>();

            public EulerAngleAccessorWithCache(Animator animator, HumanLimits humanLimits, EulerAngleAccessor accessor)
            {
                _animator = animator;
                _humanLimits = humanLimits;
                _accessor = accessor;
            }

            public Vector3 SetLocalEulerAngle(HumanBodyBones bone, Vector3 angle)
            {
                var boneTransform = _animator.GetBoneTransform(bone);
                if (!boneTransform) return Vector3.zero;
                var nextAngle = _humanLimits.ClampAngle(bone, angle);
                var rotatedAngle = nextAngle - GetLocalEulerAngle(bone);
                _localEulerAngles[bone] = nextAngle;
                boneTransform.localEulerAngles = nextAngle;
                return rotatedAngle;
            }

            public Vector3 GetLocalEulerAngle(HumanBodyBones bone)
            {
                var boneTransform = _animator.GetBoneTransform(bone);
                if (!boneTransform) return Vector3.zero;

                // キャッシュがない場合はキャッシュなしアクセサを使う
                if (!_localEulerAngles.TryGetValue(bone, out var angle))
                {
                    return _accessor.GetLocalEulerAngle(bone);
                }

                // Bone の rotation とキャッシュが異なる場合はキャッシュを無効にしてキャッシュなしアクセサを使う
                if (Quaternion.Angle(boneTransform.localRotation, Quaternion.Euler(angle)) > Mathf.Epsilon)
                {
                    _localEulerAngles.Remove(bone);
                    return _accessor.GetLocalEulerAngle(bone);
                }

                // キャッシュ済みの角度は ClampAngle 済みなのでそのまま返す
                return angle;
            }
        }
    }
}
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    public class GravityApplier : MonoBehaviour
    {
        public bool isKinematic;

        [Tooltip(
            "This value range is considered to be almost the same height " +
            "when comparing the height of body parts.")]
        public float nearDistance = 0.03f;

        [Tooltip(
            "If the distance between the center of gravity and the ground contact point is within this value range, " +
            "the body will stand upright.")]
        public Vector3 uprightThresholdDistance = new(0.2f, 1f, 0.2f);

        public Transform centroid;
        public Transform bottom;

        public IPostureControl PostureControl { get; set; }
        public Vector3 CentroidPosition { get; private set; }
        public Vector3 BottomPosition { get; private set; }

        private bool _initialized;
        private bool _postureFixed;
        private Rigidbody _rigidbody;
        private Transform _hipsTransform;
        private IReadOnlyList<WeightUnit> _weightUnits;

        private IReadOnlyList<BoneProperty> _spineBones;
        private IReadOnlyList<BoneProperty> _leftArmBones;
        private IReadOnlyList<BoneProperty> _rightArmBones;
        private IReadOnlyList<BoneProperty> _leftLegBones;
        private IReadOnlyList<BoneProperty> _rightLegBones;

        public void Initialize(BodyWeightContainer bodyWeights)
        {
            if (bodyWeights is null)
            {
                throw new InvalidOperationException(
                    "The GravityApplier requires the BodyWeightContainer object.");
            }

            _rigidbody = GetComponent<Rigidbody>() ?? throw new InvalidOperationException(
                "The GravityApplier requires the Rigidbody component.");

            _rigidbody.useGravity = true;

            // 身体の動きが不安定になるので Rigidbody による回転を禁止する
            _rigidbody.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationY |
                RigidbodyConstraints.FreezeRotationZ;

            // Discrete では足が地面の Collider を突き抜けることがある
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var animator = GetComponent<Animator>() ?? throw new InvalidOperationException(
                "The GravityApplier requires the Animator component.");

            _hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips) ?? throw new InvalidOperationException(
                "The Animator has not hips bone.");

            _weightUnits = ToWeightUnitList(animator, new BodyWeights(bodyWeights));

            var forceGenerator = GetComponent<ForceResponder>() ?? throw new InvalidOperationException(
                "The GravityApplier requires the ForceResponder component.");

            var boneProperties = forceGenerator.BoneProperties;
            _spineBones = CreateBonePropertyList(animator, boneProperties, new[]
            {
                HumanBodyBones.Hips,
                HumanBodyBones.Spine,
                HumanBodyBones.Chest,
                HumanBodyBones.UpperChest,
                HumanBodyBones.Head
            });
            _leftArmBones = CreateBonePropertyList(animator, boneProperties, new[]
            {
                HumanBodyBones.LeftUpperArm,
                HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand
            });
            _rightArmBones = CreateBonePropertyList(animator, boneProperties, new[]
            {
                HumanBodyBones.RightUpperArm,
                HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand
            });
            _leftLegBones = CreateBonePropertyList(animator, boneProperties, new[]
            {
                HumanBodyBones.LeftUpperLeg,
                HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot
            });
            _rightLegBones = CreateBonePropertyList(animator, boneProperties, new[]
            {
                HumanBodyBones.RightUpperLeg,
                HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot
            });

            _initialized = true;

            SetPostureState(true);
        }

        private static IReadOnlyList<WeightUnit> ToWeightUnitList(Animator animator, BodyWeights bodyWeights)
        {
            return Enumerable.Range(0, (int)HumanBodyBones.LastBone - 1)
                .Cast<HumanBodyBones>()
                .Select(bone => new WeightUnit
                    {
                        BoneTransform = animator.GetBoneTransform(bone),
                        Weight = bodyWeights.Get(bone)
                    }
                )
                .Where(e => e.Weight != 0f && e.BoneTransform is not null)
                .ToList();
        }

        private static IReadOnlyList<BoneProperty> CreateBonePropertyList(
            Animator animator,
            BoneProperties properties,
            IEnumerable<HumanBodyBones> bones
        )
        {
            var list = new List<BoneProperty>();

            foreach (var bone in bones)
            {
                var boneTransform = animator.GetBoneTransform(bone);
                if (!properties.TryGetValue(boneTransform, out var property)) continue;
                if (!property.HasCollider) continue;
                list.Add(property);
            }

            return list;
        }

        private void SetPostureState(bool isFixed)
        {
            if (_postureFixed == isFixed) return;

            _postureFixed = isFixed;

            // 設定を受け付けなかった場合は状態を戻す
            if (PostureControl?.SetPostureControlState(!isFixed) == false)
            {
                _postureFixed = !isFixed;
            }
        }

        private Vector3 GetBottomPosition()
        {
            var bottomPositions = new[]
            {
                GetBottomPosition(_spineBones),
                GetBottomPosition(_leftArmBones),
                GetBottomPosition(_rightArmBones),
                GetBottomPosition(_leftLegBones),
                GetBottomPosition(_rightLegBones),
            };

            // 地面に最も近い位置を求める(複数の位置が同じ位の高さにある場合がある)
            var minY = bottomPositions.Min(p => p.y);
            var isLowest = bottomPositions.Select(p => p.y - minY < nearDistance).ToArray();

            var accumulator = new Vector3Accumulator();

            if (isLowest[0]) accumulator.Add(bottomPositions[0]);

            if (isLowest[1] && isLowest[2])
            {
                accumulator.Add((bottomPositions[1] + bottomPositions[2]) / 2f);
            }
            else
            {
                if (isLowest[1]) accumulator.Add(bottomPositions[1]);
                if (isLowest[2]) accumulator.Add(bottomPositions[2]);
            }

            if (isLowest[3] && isLowest[4])
            {
                accumulator.Add((bottomPositions[3] + bottomPositions[4]) / 2f);
            }
            else
            {
                if (isLowest[3]) accumulator.Add(bottomPositions[3]);
                if (isLowest[4]) accumulator.Add(bottomPositions[4]);
            }

            // 地面に最も近い位置の平均を求める
            return accumulator.Average;
        }

        private static Vector3 GetBottomPosition(IReadOnlyList<BoneProperty> bonePropertiesOrder)
        {
            var bottomPosition = bonePropertiesOrder.First().GetBottomPosition();

            foreach (var boneProperty in bonePropertiesOrder)
            {
                var p = boneProperty.GetBottomPosition();
                if (p.y + Mathf.Epsilon > bottomPosition.y) continue;
                bottomPosition = p;
            }

            return bottomPosition;
        }

        private Vector3 GetCentroidPosition()
        {
            var centroidPosition = Vector3.zero;
            var totalWeight = 0f;

            foreach (var unit in _weightUnits)
            {
                centroidPosition += unit.BoneTransform.position * unit.Weight;
                totalWeight += unit.Weight;
            }

            return centroidPosition / totalWeight;
        }

        private void Update()
        {
            if (!_initialized) return;

            _rigidbody.isKinematic = isKinematic;

            CentroidPosition = GetCentroidPosition();
            BottomPosition = GetBottomPosition();

            if (centroid) centroid.position = CentroidPosition;
            if (bottom) bottom.position = BottomPosition;

            var deltaPosition =
                (CentroidPosition - BottomPosition).AlignWithForwardDirection(_hipsTransform.rotation);

            SetPostureState(
                Mathf.Abs(deltaPosition.x) < uprightThresholdDistance.x &&
                Mathf.Abs(deltaPosition.y) < uprightThresholdDistance.y &&
                Mathf.Abs(deltaPosition.z) < uprightThresholdDistance.z
            );
        }

        private struct WeightUnit
        {
            public Transform BoneTransform;
            public float Weight;
        }

        private struct Vector3Accumulator
        {
            private Vector3 _total;
            private int _count;

            public Vector3 Average => _total / _count;

            public void Add(Vector3 v)
            {
                _total += v;
                _count++;
            }
        }
    }
}
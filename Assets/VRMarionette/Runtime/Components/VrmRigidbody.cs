// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace VRMarionette
{
    public class VrmRigidbody : MonoBehaviour
    {
        public bool isKinematic;

        [Tooltip(
            "This value range is considered to be almost the same height " +
            "when comparing the height of body parts.")]
        public float nearDistance = 0.03f;

        [Tooltip(
            "If the distance between the center of gravity and the ground contact point is within this value range, " +
            "the body will stand upright.")]
        public Vector3 uprightThresholdDistance = new(0.2f, 1f, 0.3f);

        public Transform centroid;
        public Transform ground;

        private bool _initialized;
        private Rigidbody _rigidbody;
        private Transform _hipsTransform;
        private IReadOnlyList<WeightUnit> _weightUnits;

        public void Initialize(Vrm10Instance instance, BodyWeightContainer bodyWeights)
        {
            if (!bodyWeights)
            {
                throw new InvalidOperationException("The VrmRigidbody has not body weights.");
            }

            _rigidbody = instance.GetComponent<Rigidbody>();

            if (!_rigidbody)
            {
                throw new InvalidOperationException("The VrmInstance has not Rigidbody component.");
            }

            _rigidbody.useGravity = true;

            var animator = instance.GetComponent<Animator>();

            if (!animator)
            {
                throw new InvalidOperationException("The VrmInstance has not Animator component.");
            }

            _hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips)
                             ?? throw new InvalidOperationException();

            _weightUnits = ToWeightUnitList(animator, new BodyWeights(bodyWeights));

            _initialized = true;

            LockRotation();
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

        private void LockRotation()
        {
            _rigidbody.constraints =
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationY |
                RigidbodyConstraints.FreezeRotationZ;
        }

        private void UnlockRotation()
        {
            _rigidbody.constraints = RigidbodyConstraints.None;
        }

        private Vector3 GetCentroidPosition(out Vector3 basePosition)
        {
            var centroidPosition = Vector3.zero;

            basePosition = _hipsTransform.position;

            // 地面に最も近い位置を求める(複数の位置が同じ位の高さにある場合がある)
            var minY = basePosition.y;
            var lowestPositions = new List<Vector3> { basePosition };
            foreach (var unit in _weightUnits)
            {
                var bonePosition = unit.BoneTransform.position;
                if (bonePosition.y < minY)
                {
                    minY = bonePosition.y;
                    lowestPositions = lowestPositions.Where(e => e.y - minY < nearDistance).ToList();
                }

                if (bonePosition.y - minY < nearDistance)
                {
                    lowestPositions.Add(bonePosition);
                }
            }

            // 地面に最も近い位置の平均を基準の位置とする
            basePosition = lowestPositions
                .Aggregate(Vector3.zero, (total, e) => total + e) / lowestPositions.Count;

            foreach (var unit in _weightUnits)
            {
                var delta = unit.BoneTransform.position - basePosition;
                centroidPosition += delta * unit.Weight;
            }

            centroidPosition.y /= _weightUnits.Count;
            return centroidPosition;
        }

        private void Update()
        {
            if (!_initialized) return;

            _rigidbody.isKinematic = isKinematic;

            var centroidPosition = GetCentroidPosition(out var basePosition);

            if (centroid is not null) centroid.position = basePosition + centroidPosition;
            if (ground is not null) ground.position = basePosition;

            var yAngle = _hipsTransform.eulerAngles.y;
            centroidPosition = Quaternion.Euler(0f, -yAngle, 0f) * centroidPosition;

            if (Mathf.Abs(centroidPosition.x) < uprightThresholdDistance.x &&
                Mathf.Abs(centroidPosition.y) < uprightThresholdDistance.y &&
                Mathf.Abs(centroidPosition.z) < uprightThresholdDistance.z)
            {
                LockRotation();
            }
            else
            {
                UnlockRotation();
            }
        }

        private struct WeightUnit
        {
            public Transform BoneTransform;
            public float Weight;
        }
    }
}
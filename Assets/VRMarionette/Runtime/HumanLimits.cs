using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    /// <summary>
    /// 各 Bone の回転角度の限界値
    /// 存在しない Bone の min, max は Vector.zero に設定される
    /// </summary>
    public class HumanLimits
    {
        private readonly IReadOnlyDictionary<HumanBodyBones, HumanLimit> _entries;

        private static readonly Vector3 Min = new(-180f, -180f, -180f);
        private static readonly Vector3 Max = new(180f, 180f, 180f);

        public HumanLimits(HumanLimitContainer container, Animator animator)
        {
            _entries = container.humanLimits
                .ToDictionary(
                    e => e.bone,
                    e => Filter(Sanitize(e), animator)
                );
        }

        private static HumanLimit Sanitize(HumanLimit humanLimit)
        {
            if (humanLimit.min.x > -Mathf.Epsilon) humanLimit.min.x = 0f;
            if (humanLimit.min.y > -Mathf.Epsilon) humanLimit.min.y = 0f;
            if (humanLimit.min.z > -Mathf.Epsilon) humanLimit.min.z = 0f;
            if (humanLimit.max.x < Mathf.Epsilon) humanLimit.max.x = 0f;
            if (humanLimit.max.y < Mathf.Epsilon) humanLimit.max.y = 0f;
            if (humanLimit.max.z < Mathf.Epsilon) humanLimit.max.z = 0f;

            if (humanLimit.min.x < Min.x) humanLimit.min.x = Min.x;
            if (humanLimit.min.y < Min.y) humanLimit.min.y = Min.y;
            if (humanLimit.min.z < Min.z) humanLimit.min.z = Min.z;
            if (humanLimit.max.x > Max.x) humanLimit.max.x = Max.x;
            if (humanLimit.max.y > Max.y) humanLimit.max.y = Max.y;
            if (humanLimit.max.z > Max.z) humanLimit.max.z = Max.z;

            return humanLimit;
        }

        private static HumanLimit Filter(HumanLimit humanLimit, Animator animator)
        {
            if (animator.GetBoneTransform(humanLimit.bone) is not null) return humanLimit;
            humanLimit.min = Vector3.zero;
            humanLimit.max = Vector3.zero;
            Debug.LogWarning(
                $"Since {humanLimit.bone} does not exist, " +
                $"the humanLimit of {humanLimit.bone} is set to {Vector3.zero}.");
            return humanLimit;
        }

        public bool TryGetValue(HumanBodyBones bone, out HumanLimit humanLimit)
        {
            return _entries.TryGetValue(bone, out humanLimit);
        }

        public HumanLimit Get(HumanBodyBones bone)
        {
            return _entries.GetValueOrDefault(bone);
        }

        public Vector3 ClampAngle(HumanBodyBones bone, Vector3 angle)
        {
            Vector3 min, max;

            if (_entries.TryGetValue(bone, out var humanLimit))
            {
                min = humanLimit.min;
                max = humanLimit.max;
            }
            else
            {
                min = Min;
                max = Max;
            }

            return new Vector3(
                Mathf.Clamp(angle.x, min.x, max.x),
                Mathf.Clamp(angle.y, min.y, max.y),
                Mathf.Clamp(angle.z, min.z, max.z)
            );
        }

        public Quaternion ToRotation(HumanBodyBones bone, Vector3 angle)
        {
            return _entries.TryGetValue(bone, out var humanLimit)
                ? angle.ToRotationWithAxis(humanLimit.axis)
                : Quaternion.Euler(angle);
        }

        public Vector3 ToEulerAngles(HumanBodyBones bone, Quaternion rotation)
        {
            return _entries.TryGetValue(bone, out var humanLimit)
                ? rotation.ToEulerAnglesWithAxis(humanLimit.axis)
                : rotation.eulerAngles;
        }
    }
}
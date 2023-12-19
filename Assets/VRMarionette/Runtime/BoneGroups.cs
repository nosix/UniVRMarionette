using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRMarionette
{
    public class BoneGroups
    {
        public enum Id
        {
            Body,
            Neck,
            LeftShoulder,
            RightShoulder,
            LeftThumb,
            RightThumb,
            LeftIndexProximal,
            LeftMiddleProximal,
            LeftRingProximal,
            LeftLittleProximal,
            RightIndexProximal,
            RightMiddleProximal,
            RightRingProximal,
            RightLittleProximal,
            LeftIndexDistal,
            LeftMiddleDistal,
            LeftRingDistal,
            LeftLittleDistal,
            RightIndexDistal,
            RightMiddleDistal,
            RightRingDistal,
            RightLittleDistal,
            Nothing
        }

        private readonly IReadOnlyDictionary<Id, BoneGroupSpec> _specs;

        public BoneGroups(HumanLimits humanLimits)
        {
            _specs = new Dictionary<Id, BoneGroupSpec>
            {
                {
                    Id.Body,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.Spine, HumanBodyBones.Chest,
                        HumanBodyBones.UpperChest)
                },
                {
                    Id.Neck,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.Head, HumanBodyBones.Neck)
                },
                {
                    Id.LeftShoulder,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm)
                },
                {
                    Id.RightShoulder,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm)
                },
                {
                    Id.LeftThumb,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftThumbProximal,
                        HumanBodyBones.LeftThumbIntermediate)
                },
                {
                    Id.RightThumb,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.RightThumbProximal,
                        HumanBodyBones.RightThumbIntermediate)
                },
                {
                    Id.LeftIndexProximal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftIndexProximal,
                        HumanBodyBones.LeftIndexIntermediate)
                },
                {
                    Id.LeftMiddleProximal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftMiddleProximal,
                        HumanBodyBones.LeftMiddleIntermediate)
                },
                {
                    Id.LeftRingProximal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate)
                },
                {
                    Id.LeftLittleProximal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftLittleProximal,
                        HumanBodyBones.LeftLittleIntermediate)
                },
                {
                    Id.RightIndexProximal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.RightIndexProximal,
                        HumanBodyBones.RightIndexIntermediate)
                },
                {
                    Id.RightMiddleProximal,
                    new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate)
                },
                {
                    Id.RightRingProximal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.RightRingProximal,
                        HumanBodyBones.RightRingIntermediate)
                },
                {
                    Id.RightLittleProximal,
                    new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate)
                },
                {
                    Id.LeftIndexDistal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate)
                },
                {
                    Id.LeftMiddleDistal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftMiddleDistal,
                        HumanBodyBones.LeftMiddleIntermediate)
                },
                {
                    Id.LeftRingDistal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate)
                },
                {
                    Id.LeftLittleDistal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.LeftLittleDistal,
                        HumanBodyBones.LeftLittleIntermediate)
                },
                {
                    Id.RightIndexDistal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.RightIndexDistal,
                        HumanBodyBones.RightIndexIntermediate)
                },
                {
                    Id.RightMiddleDistal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.RightMiddleDistal,
                        HumanBodyBones.RightMiddleIntermediate)
                },
                {
                    Id.RightRingDistal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate)
                },
                {
                    Id.RightLittleDistal,
                    new BoneGroupSpec(humanLimits, HumanBodyBones.RightLittleDistal,
                        HumanBodyBones.RightLittleIntermediate)
                },
            };
        }

        public BoneGroupSpec GetSpec(Id id)
        {
            if (_specs.TryGetValue(id, out var spec)) return spec;
            throw new InvalidOperationException($"The bone group {id} is missing from the BoneGroups configuration.");
        }

        /// <summary>
        /// 連動して動作する骨グループの仕様を表す。
        /// 各骨に可動範囲の角度(minは負方向の角度,maxは正方向の角度)が設定されている。
        /// グループとしての可動範囲は各骨の可動範囲の合計になる。
        /// 各骨の可動範囲÷グループの可動範囲が Ratio (分配比率)として保持されている。
        /// </summary>
        public class BoneGroupSpec
        {
            public IReadOnlyDictionary<HumanBodyBones, Ratio> Ratios { get; }

            public BoneGroupSpec(HumanLimits humanLimits, params HumanBodyBones[] bones)
            {
                // Group に含まれる全骨の合計角度(可動範囲)を求める
                var totalMinAngle = new Vector3();
                var totalMaxAngle = new Vector3();
                foreach (var bone in bones)
                {
                    var humanLimit = humanLimits.Get(bone);
                    totalMinAngle += humanLimit.min; // min は 0 以下
                    totalMaxAngle += humanLimit.max; // max は 0 以上
                }

                // Group 内での各部位の角度の分配比率を求める
                var tmpRatios = new Dictionary<HumanBodyBones, Ratio>();
                foreach (var bone in bones)
                {
                    var humanLimit = humanLimits.Get(bone);
                    tmpRatios.Add(bone, new Ratio(
                        new Vector3(
                            ToRatio(humanLimit.min.x, totalMinAngle.x),
                            ToRatio(humanLimit.min.y, totalMinAngle.y),
                            ToRatio(humanLimit.min.z, totalMinAngle.z)
                        ),
                        new Vector3(
                            ToRatio(humanLimit.max.x, totalMaxAngle.x),
                            ToRatio(humanLimit.max.y, totalMaxAngle.y),
                            ToRatio(humanLimit.max.z, totalMaxAngle.z)
                        )
                    ));
                }

                Ratios = tmpRatios;
            }

            private static float ToRatio(float value, float total)
            {
                return Mathf.Approximately(total, 0f) ? 0f : value / total;
            }
        }

        public readonly struct Ratio
        {
            private Vector3 Min { get; }
            private Vector3 Max { get; }

            public Ratio(Vector3 min, Vector3 max)
            {
                Min = min;
                Max = max;
            }

            public Vector3 Apply(Vector3 angle)
            {
                var x = 0f;
                var y = 0f;
                var z = 0f;

                if (angle.x < -Mathf.Epsilon) x = angle.x * Min.x;
                if (angle.x > Mathf.Epsilon) x = angle.x * Max.x;
                if (angle.y < -Mathf.Epsilon) y = angle.y * Min.y;
                if (angle.y > Mathf.Epsilon) y = angle.y * Max.y;
                if (angle.z < -Mathf.Epsilon) z = angle.z * Min.z;
                if (angle.z > Mathf.Epsilon) z = angle.z * Max.z;

                return new Vector3(x, y, z);
            }
        }
    }
}
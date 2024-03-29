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

        private readonly IReadOnlyDictionary<Id, BoneGroupSpec> _specsByGroup;
        private readonly IReadOnlyDictionary<HumanBodyBones, BoneGroupSpec> _specsByBone;

        public BoneGroups(HumanLimits humanLimits)
        {
            _specsByGroup = new Dictionary<Id, BoneGroupSpec>
            {
                {
                    Id.Body, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.UpperChest, HumanBodyBones.Chest, HumanBodyBones.Spine)
                },
                {
                    Id.Neck, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.Head, HumanBodyBones.Neck)
                },
                {
                    Id.LeftShoulder, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm)
                },
                {
                    Id.RightShoulder, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm)
                },
                {
                    Id.LeftThumb, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftThumbIntermediate)
                },
                {
                    Id.RightThumb, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate)
                },
                {
                    Id.LeftIndexDistal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate)
                },
                {
                    Id.LeftMiddleDistal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate)
                },
                {
                    Id.LeftRingDistal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate)
                },
                {
                    Id.LeftLittleDistal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate)
                },
                {
                    Id.RightIndexDistal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate)
                },
                {
                    Id.RightMiddleDistal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate)
                },
                {
                    Id.RightRingDistal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate)
                },
                {
                    Id.RightLittleDistal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate)
                },
                {
                    Id.LeftIndexProximal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate)
                },
                {
                    Id.LeftMiddleProximal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate)
                },
                {
                    Id.LeftRingProximal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate)
                },
                {
                    Id.LeftLittleProximal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate)
                },
                {
                    Id.RightIndexProximal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate)
                },
                {
                    Id.RightMiddleProximal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate)
                },
                {
                    Id.RightRingProximal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate)
                },
                {
                    Id.RightLittleProximal, new BoneGroupSpec(humanLimits,
                        HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate)
                }
            };

            var specsByBone = new Dictionary<HumanBodyBones, BoneGroupSpec>();

            foreach (var spec in _specsByGroup.Values)
            {
                foreach (var bone in spec.Bones)
                {
                    // Intermediate は複数の BoneGroup に属するが Proximal(根元) を優先するため上書きする
                    specsByBone[bone] = spec;
                }
            }

            _specsByBone = specsByBone;
        }

        public BoneGroupSpec GetSpec(Id id)
        {
            if (_specsByGroup.TryGetValue(id, out var spec)) return spec;
            throw new InvalidOperationException($"The bone group {id} is missing from the BoneGroups configuration.");
        }

        public BoneGroupSpec GetSpec(HumanBodyBones bone)
        {
            return _specsByBone.GetValueOrDefault(bone);
        }
    }
}
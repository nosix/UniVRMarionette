using System;
using UnityEngine;

namespace VRMarionette
{
    [Serializable]
    public class BoneSnapshot
    {
        public Quaternion[] rotations = new Quaternion[(int)HumanBodyBones.LastBone];

        public void Capture(Animator animator)
        {
            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                var t = animator.GetBoneTransform(bone);
                rotations[i] = t != null ? t.localRotation : Quaternion.identity;
            }
        }

        public void Restore(Animator animator)
        {
            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                var bone = (HumanBodyBones)i;
                var t = animator.GetBoneTransform(bone);
                if (t is not null) t.localRotation = rotations[i];
            }
        }
    }
}
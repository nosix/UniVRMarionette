using System;
using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace VRMarionette_Sample.GravityApplierDemo.Runtime
{
    public class RotationSamples : MonoBehaviour
    {
        public HumanoidMixer mixer;
        public RotationSample[] samples;

        public void SetInstance(Vrm10Instance instance)
        {
            mixer = instance.GetComponent<HumanoidMixer>() ?? throw new InvalidOperationException(
                "HumanoidMixer is required.");
        }

        public void Reset()
        {
            mixer.Reset();

            var t = mixer.transform;
            t.rotation = Quaternion.identity;
            t.position = new Vector3(0f, 1f, 0f);
        }

        public void Apply(int index)
        {
            var sample = samples[index];
            mixer.SetBoneRotation(HumanBodyBones.Hips, sample.hips);
            mixer.SetBoneRotation(HumanBodyBones.LeftUpperLeg, sample.leftUpperLeg);
            mixer.SetBoneRotation(HumanBodyBones.LeftLowerLeg, sample.leftLowerLeg);
            mixer.SetBoneRotation(HumanBodyBones.LeftFoot, sample.leftFoot);
            mixer.SetBoneRotation(HumanBodyBones.RightUpperLeg, sample.rightUpperLeg);
            mixer.SetBoneRotation(HumanBodyBones.RightLowerLeg, sample.rightLowerLeg);
            mixer.SetBoneRotation(HumanBodyBones.RightFoot, sample.rightFoot);
        }

        [Serializable]
        public struct RotationSample
        {
            public Vector3 hips;
            public Vector3 leftUpperLeg;
            public Vector3 leftLowerLeg;
            public Vector3 leftFoot;
            public Vector3 rightUpperLeg;
            public Vector3 rightLowerLeg;
            public Vector3 rightFoot;
        }
    }
}
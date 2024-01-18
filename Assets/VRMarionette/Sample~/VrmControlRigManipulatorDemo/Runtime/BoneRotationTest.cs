using System;
using System.Collections;
using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace VRMarionette_Sample.VrmControlRigManipulatorDemo.Runtime
{
    public class BoneRotationTest : MonoBehaviour
    {
        public bool runTest;

        public void Run(Vrm10Instance instance)
        {
            var manipulator = instance.GetComponent<VrmControlRigManipulator>()
                              ?? throw new InvalidOperationException("VrmControlRigManipulator does not found.");
            if (runTest) StartCoroutine(RunTest(manipulator));
        }

        private static IEnumerator RunTest(VrmControlRigManipulator manipulator)
        {
            for (HumanBodyBones bone = 0; bone < HumanBodyBones.LastBone; bone++)
            {
                if (bone is
                    HumanBodyBones.Jaw or
                    HumanBodyBones.LeftEye or
                    HumanBodyBones.RightEye
                   ) continue;

                Debug.Log($"Test {bone}");

                for (var x = -180; x <= 180; x += 45)
                {
                    for (var y = -180; y <= 180; y += 45)
                    {
                        for (var z = -180; z <= 180; z += 45)
                        {
                            manipulator.SetBoneRotation(bone, Vector3.zero);
                            var angles = new Vector3(x, y, z);
                            var deltaAngles = manipulator.SetBoneRotation(bone, angles);
                            var actualAngles = manipulator.GetBoneRotation(bone);

                            var diff = (actualAngles - deltaAngles).magnitude;
                            if (diff > 0.01)
                            {
                                Debug.LogError($"Fail: {bone} {angles}\n{deltaAngles} != {actualAngles}\n{diff}");
                            }

                            yield return null;
                        }
                    }
                }
            }

            Debug.Log("Finished");
        }
    }
}
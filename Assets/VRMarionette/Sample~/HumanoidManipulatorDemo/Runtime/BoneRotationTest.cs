using System;
using System.Collections;
using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace VRMarionette_Sample.HumanoidManipulatorDemo.Runtime
{
    public class BoneRotationTest : MonoBehaviour
    {
        public bool runTest;

        public Vector3 min;
        public Vector3 max;
        public Vector3 step;

        public new Transform camera;

        public void Run(Vrm10Instance instance)
        {
            var manipulator = instance.GetComponent<HumanoidManipulator>() ?? throw new InvalidOperationException(
                "HumanoidManipulator does not found.");

            if (runTest) StartCoroutine(RunTest(manipulator));
        }

        private IEnumerator RunTest(HumanoidManipulator manipulator)
        {
            var animator = manipulator.GetComponent<Animator>();

            for (HumanBodyBones bone = 0; bone < HumanBodyBones.LastBone; bone++)
            {
                if (bone is
                    HumanBodyBones.Jaw or
                    HumanBodyBones.LeftEye or
                    HumanBodyBones.RightEye
                   ) continue;

                Debug.Log($"Test {bone}");

                var cameraPosition = animator.GetBoneTransform(bone).position;
                cameraPosition.z += 1f;
                camera.position = cameraPosition;

                for (var x = min.x; x <= max.x; x += step.x)
                {
                    for (var y = min.y; y <= max.y; y += step.y)
                    {
                        for (var z = min.z; z <= max.z; z += step.z)
                        {
                            manipulator.SetBoneRotation(bone, Vector3.zero);
                            var angles = new Vector3(x, y, z);
                            var deltaAngles = manipulator.SetBoneRotation(bone, angles);
                            var actualAngles = manipulator.GetBoneRotation(bone);

                            var isValidX = Mathf.Abs(deltaAngles.x) < 0.1f || angles.x / deltaAngles.x > 0.9f;
                            var isValidY = Mathf.Abs(deltaAngles.y) < 0.1f || angles.y / deltaAngles.y > 0.9f;
                            var isValidZ = Mathf.Abs(deltaAngles.z) < 0.1f || angles.z / deltaAngles.z > 0.9f;

                            if (!isValidX || !isValidY || !isValidZ)
                            {
                                var angle = Quaternion.Angle(
                                    manipulator.HumanLimits.ToRotation(bone, actualAngles),
                                    manipulator.HumanLimits.ToRotation(bone, angles)
                                );

                                if (angle > 0.01)
                                {
                                    Debug.LogError(
                                        $"Fail: {bone}\n" +
                                        $"angles={angles}\n" +
                                        $"deltaAngles={deltaAngles}\n" +
                                        $"actualAngles={actualAngles}\n" +
                                        $"angle={angle}\n"
                                    );
                                }
                                else
                                {
                                    Debug.LogWarning(
                                        $"Warning: {bone}\n" +
                                        "The values are different, but the posture is the same.\n" +
                                        $"angles={angles}\n" +
                                        $"deltaAngles={deltaAngles}\n" +
                                        $"actualAngles={actualAngles}\n"
                                    );
                                }
                            }

                            yield return null;
                        }
                    }
                }

                manipulator.SetBoneRotation(bone, Vector3.zero);
            }

            Debug.Log("Finished");
        }
    }
}
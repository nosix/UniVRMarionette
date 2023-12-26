using System;
using UnityEngine;

namespace VRMarionette
{
    /// <summary>
    /// Unity Editor 上で VRM モデルの Control RIg を操作するためのコンポーネント。
    /// VrmControlRigMixerEditor と連携して使われる。
    /// VrmControlRigManipulator コンポーネントを持つ GameObject に追加して使用する。
    /// </summary>
    public class VrmControlRigMixer : MonoBehaviour
    {
        public VrmControlRigManipulator vrmManipulator;

        public BoneRotation hips;
        public BoneRotation spine;
        public BoneRotation chest;
        public BoneRotation upperChest;
        public BoneRotation neck;
        public BoneRotation head;

        [Header("Left Arm")]
        public BoneRotation leftShoulder;

        public BoneRotation leftUpperArm;
        public BoneRotation leftLowerArm;
        public BoneRotation leftHand;
        public BoneRotation leftThumbProximal;
        public BoneRotation leftThumbIntermediate;
        public BoneRotation leftThumbDistal;
        public BoneRotation leftIndexProximal;
        public BoneRotation leftIndexIntermediate;
        public BoneRotation leftIndexDistal;
        public BoneRotation leftMiddleProximal;
        public BoneRotation leftMiddleIntermediate;
        public BoneRotation leftMiddleDistal;
        public BoneRotation leftRingProximal;
        public BoneRotation leftRingIntermediate;
        public BoneRotation leftRingDistal;
        public BoneRotation leftLittleProximal;
        public BoneRotation leftLittleIntermediate;
        public BoneRotation leftLittleDistal;

        [Header("Right Arm")]
        public BoneRotation rightShoulder;

        public BoneRotation rightUpperArm;
        public BoneRotation rightLowerArm;
        public BoneRotation rightHand;
        public BoneRotation rightThumbProximal;
        public BoneRotation rightThumbIntermediate;
        public BoneRotation rightThumbDistal;
        public BoneRotation rightIndexProximal;
        public BoneRotation rightIndexIntermediate;
        public BoneRotation rightIndexDistal;
        public BoneRotation rightMiddleProximal;
        public BoneRotation rightMiddleIntermediate;
        public BoneRotation rightMiddleDistal;
        public BoneRotation rightRingProximal;
        public BoneRotation rightRingIntermediate;
        public BoneRotation rightRingDistal;
        public BoneRotation rightLittleProximal;
        public BoneRotation rightLittleIntermediate;
        public BoneRotation rightLittleDistal;

        [Header("Left Leg")]
        public BoneRotation leftUpperLeg;

        public BoneRotation leftLowerLeg;
        public BoneRotation leftFoot;
        public BoneRotation leftToes;

        [Header("Right Leg")]
        public BoneRotation rightUpperLeg;

        public BoneRotation rightLowerLeg;
        public BoneRotation rightFoot;
        public BoneRotation rightToes;

        private void Start()
        {
            vrmManipulator = GetComponent<VrmControlRigManipulator>();
            if (!vrmManipulator)
            {
                Debug.LogError("Please set to the GameObject with the VrmControlRigManipulator Component.");
            }

            hips.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.Hips, angle));
            spine.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.Spine, angle));
            chest.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.Chest, angle));
            upperChest.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.UpperChest, angle));
            neck.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.Neck, angle));
            head.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.Head, angle));

            leftShoulder.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftShoulder, angle));
            leftUpperArm.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftUpperArm, angle));
            leftLowerArm.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftLowerArm, angle));
            leftHand.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftHand, angle));

            leftThumbProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftThumbProximal, angle));
            leftThumbIntermediate.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftThumbIntermediate, angle));
            leftThumbDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftThumbDistal, angle));
            leftIndexProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftIndexProximal, angle));
            leftIndexIntermediate.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftIndexIntermediate, angle));
            leftIndexDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftIndexDistal, angle));
            leftMiddleProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftMiddleProximal, angle));
            leftMiddleIntermediate.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftMiddleIntermediate, angle));
            leftMiddleDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftMiddleDistal, angle));
            leftRingProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftRingProximal, angle));
            leftRingIntermediate.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftRingIntermediate, angle));
            leftRingDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftRingDistal, angle));
            leftLittleProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftLittleProximal, angle));
            leftLittleIntermediate.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftLittleIntermediate, angle));
            leftLittleDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftLittleDistal, angle));

            rightShoulder.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightShoulder, angle));
            rightUpperArm.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightUpperArm, angle));
            rightLowerArm.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightLowerArm, angle));
            rightHand.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightHand, angle));

            rightThumbProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightThumbProximal, angle));
            rightThumbIntermediate.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightThumbIntermediate, angle));
            rightThumbDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightThumbDistal, angle));
            rightIndexProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightIndexProximal, angle));
            rightIndexIntermediate.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightIndexIntermediate, angle));
            rightIndexDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightIndexDistal, angle));
            rightMiddleProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightMiddleProximal, angle));
            rightMiddleIntermediate.SetOnChanged(
                angle => SetBoneRotation(HumanBodyBones.RightMiddleIntermediate, angle));
            rightMiddleDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightMiddleDistal, angle));
            rightRingProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightRingProximal, angle));
            rightRingIntermediate.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightRingIntermediate, angle));
            rightRingDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightRingDistal, angle));
            rightLittleProximal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightLittleProximal, angle));
            rightLittleIntermediate.SetOnChanged(
                angle => SetBoneRotation(HumanBodyBones.RightLittleIntermediate, angle));
            rightLittleDistal.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightLittleDistal, angle));

            leftUpperLeg.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftUpperLeg, angle));
            leftLowerLeg.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftLowerLeg, angle));
            leftFoot.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftFoot, angle));
            leftToes.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.LeftToes, angle));

            rightUpperLeg.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightUpperLeg, angle));
            rightLowerLeg.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightLowerLeg, angle));
            rightFoot.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightFoot, angle));
            rightToes.SetOnChanged(angle => SetBoneRotation(HumanBodyBones.RightToes, angle));
        }

        public void Reset()
        {
            hips.Reset();
            spine.Reset();
            chest.Reset();
            upperChest.Reset();
            neck.Reset();
            head.Reset();

            leftShoulder.Reset();
            leftUpperArm.Reset();
            leftLowerArm.Reset();
            leftHand.Reset();

            leftThumbProximal.Reset();
            leftThumbIntermediate.Reset();
            leftThumbDistal.Reset();
            leftIndexProximal.Reset();
            leftIndexIntermediate.Reset();
            leftIndexDistal.Reset();
            leftMiddleProximal.Reset();
            leftMiddleIntermediate.Reset();
            leftMiddleDistal.Reset();
            leftRingProximal.Reset();
            leftRingIntermediate.Reset();
            leftRingDistal.Reset();
            leftLittleProximal.Reset();
            leftLittleIntermediate.Reset();
            leftLittleDistal.Reset();

            rightShoulder.Reset();
            rightUpperArm.Reset();
            rightLowerArm.Reset();
            rightHand.Reset();

            rightThumbProximal.Reset();
            rightThumbIntermediate.Reset();
            rightThumbDistal.Reset();
            rightIndexProximal.Reset();
            rightIndexIntermediate.Reset();
            rightIndexDistal.Reset();
            rightMiddleProximal.Reset();
            rightMiddleIntermediate.Reset();
            rightMiddleDistal.Reset();
            rightRingProximal.Reset();
            rightRingIntermediate.Reset();
            rightRingDistal.Reset();
            rightLittleProximal.Reset();
            rightLittleIntermediate.Reset();
            rightLittleDistal.Reset();

            leftUpperLeg.Reset();
            leftLowerLeg.Reset();
            leftFoot.Reset();
            leftToes.Reset();

            rightUpperLeg.Reset();
            rightLowerLeg.Reset();
            rightFoot.Reset();
            rightToes.Reset();
        }

        public void SetBoneRotation(HumanBodyBones bone, Vector3 angle)
        {
            if (vrmManipulator) vrmManipulator.SetBoneRotation(bone, angle);
        }

        private void OnValidate()
        {
            hips.OnValidate();
            spine.OnValidate();
            chest.OnValidate();
            upperChest.OnValidate();
            neck.OnValidate();
            head.OnValidate();

            leftShoulder.OnValidate();
            leftUpperArm.OnValidate();
            leftLowerArm.OnValidate();
            leftHand.OnValidate();

            leftThumbProximal.OnValidate();
            leftThumbIntermediate.OnValidate();
            leftThumbDistal.OnValidate();
            leftIndexProximal.OnValidate();
            leftIndexIntermediate.OnValidate();
            leftIndexDistal.OnValidate();
            leftMiddleProximal.OnValidate();
            leftMiddleIntermediate.OnValidate();
            leftMiddleDistal.OnValidate();
            leftRingProximal.OnValidate();
            leftRingIntermediate.OnValidate();
            leftRingDistal.OnValidate();
            leftLittleProximal.OnValidate();
            leftLittleIntermediate.OnValidate();
            leftLittleDistal.OnValidate();

            rightShoulder.OnValidate();
            rightUpperArm.OnValidate();
            rightLowerArm.OnValidate();
            rightHand.OnValidate();

            rightThumbProximal.OnValidate();
            rightThumbIntermediate.OnValidate();
            rightThumbDistal.OnValidate();
            rightIndexProximal.OnValidate();
            rightIndexIntermediate.OnValidate();
            rightIndexDistal.OnValidate();
            rightMiddleProximal.OnValidate();
            rightMiddleIntermediate.OnValidate();
            rightMiddleDistal.OnValidate();
            rightRingProximal.OnValidate();
            rightRingIntermediate.OnValidate();
            rightRingDistal.OnValidate();
            rightLittleProximal.OnValidate();
            rightLittleIntermediate.OnValidate();
            rightLittleDistal.OnValidate();

            leftUpperLeg.OnValidate();
            leftLowerLeg.OnValidate();
            leftFoot.OnValidate();
            leftToes.OnValidate();

            rightUpperLeg.OnValidate();
            rightLowerLeg.OnValidate();
            rightFoot.OnValidate();
            rightToes.OnValidate();
        }

        [Serializable]
        public struct BoneRotation
        {
            [Range(-180, 180)] public float x;
            [Range(-180, 180)] public float y;
            [Range(-180, 180)] public float z;

            private float _prevX;
            private float _prevY;
            private float _prevZ;

            private Action<Vector3> _onChanged;

            public void SetOnChanged(Action<Vector3> onChanged)
            {
                _prevX = x;
                _prevY = y;
                _prevZ = z;
                _onChanged = onChanged;
            }

            public void Reset()
            {
                x = y = z = _prevX = _prevY = _prevZ = 0;
                _onChanged?.Invoke(new Vector3(x, y, z));
            }

            public void OnValidate()
            {
                if (Mathf.Approximately(_prevX - x, 0f) &&
                    Mathf.Approximately(_prevY - y, 0f) &&
                    Mathf.Approximately(_prevZ - z, 0f)) return;
                _prevX = x;
                _prevY = y;
                _prevZ = z;
                _onChanged?.Invoke(new Vector3(x, y, z));
            }
        }
    }
}
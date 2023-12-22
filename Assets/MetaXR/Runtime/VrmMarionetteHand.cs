using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UniVRM10;

namespace VRMarionette.MetaXR
{
    public class VrmMarionetteHand : MonoBehaviour
    {
        public float grabThresholdDistance = 0.04f;

        public OVRSkeleton skeleton;

        [Header("Spring Bone Colliders")]
        public VRM10SpringBoneCollider palm;

        public VRM10SpringBoneCollider thumb;
        public VRM10SpringBoneCollider index;
        public VRM10SpringBoneCollider ring;

        public UnityEvent<bool> onGrab;

        private Transform _srcPalmTransform;
        private Transform _srcThumbTransform;
        private Transform _srcIndexTransform;
        private Transform _srcRingTransform;

        private Transform _dstPalmTransform;
        private Transform _dstThumbTransform;
        private Transform _dstIndexTransform;
        private Transform _dstRingTransform;

        private bool _isGrabbing;

        // Start is called before the first frame update
        private IEnumerator Start()
        {
            while (!skeleton.IsInitialized) yield return null; // 1フレーム待つ

            foreach (var bone in skeleton.Bones)
            {
                switch (bone.Id)
                {
                    case OVRSkeleton.BoneId.Hand_Middle1:
                        _srcPalmTransform = bone.Transform;
                        break;
                    case OVRSkeleton.BoneId.Hand_Thumb3:
                        _srcThumbTransform = bone.Transform;
                        break;
                    case OVRSkeleton.BoneId.Hand_Index3:
                        _srcIndexTransform = bone.Transform;
                        break;
                    case OVRSkeleton.BoneId.Hand_Ring3:
                        _srcRingTransform = bone.Transform;
                        break;
                }
            }

            _dstPalmTransform = palm.transform;
            _dstThumbTransform = thumb.transform;
            _dstIndexTransform = index.transform;
            _dstRingTransform = ring.transform;
        }

        // Update is called once per frame
        private void Update()
        {
            if (!skeleton.IsDataValid) return;

            // 手の位置と向きを同期する
            var dstHandTransform = transform;
            var srcHandTransform = skeleton.transform;
            dstHandTransform.position = srcHandTransform.position;
            dstHandTransform.rotation = srcHandTransform.rotation;

            // 掌の位置と向きを同期する
            // 掌をZ正方向に向けるために回転を加える(VrmForceGeneratorに依存)
            _dstPalmTransform.position = _srcPalmTransform.position;
            _dstPalmTransform.rotation = _srcPalmTransform.rotation;

            var xAngle = skeleton.GetSkeletonType() switch
            {
                OVRSkeleton.SkeletonType.HandLeft => -90f,
                OVRSkeleton.SkeletonType.HandRight => 90f,
                OVRSkeleton.SkeletonType.Body => 0f,
                OVRSkeleton.SkeletonType.None => 0f,
                _ => throw new ArgumentOutOfRangeException()
            };
            _dstPalmTransform.Rotate(xAngle, 0f, 0f);

            // 親指の位置と向きを同期する
            _dstThumbTransform.position = _srcThumbTransform.position;
            _dstThumbTransform.rotation = _srcThumbTransform.rotation;

            // 人差し指の位置と向きを同期する
            _dstIndexTransform.position = _srcIndexTransform.position;
            _dstIndexTransform.rotation = _srcIndexTransform.rotation;

            // 薬指の位置と向きを同期する
            _dstRingTransform.position = _srcRingTransform.position;
            _dstRingTransform.rotation = _srcRingTransform.rotation;

            var thumbIndexDistance = Vector3.Distance(_dstThumbTransform.position, _dstIndexTransform.position);
            var isGrabbing = _isGrabbing;
            _isGrabbing = thumbIndexDistance < grabThresholdDistance;
            if (isGrabbing != _isGrabbing)
            {
                onGrab.Invoke(_isGrabbing);
            }
        }
    }
}
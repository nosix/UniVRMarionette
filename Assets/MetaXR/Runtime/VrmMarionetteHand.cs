using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UniVRM10;

namespace VRMarionette.MetaXR
{
    public class VrmMarionetteHand : MonoBehaviour
    {
        public float grabThresholdDistance = 0.04f;

        public OVRHand hand;
        public OVRControllerHelper controller;

        [Header("Spring Bone Colliders")]
        [Space]
        public VRM10SpringBoneCollider root;

        public VRM10SpringBoneCollider palm;
        public VRM10SpringBoneCollider thumb;
        public VRM10SpringBoneCollider index;
        public VRM10SpringBoneCollider ring;

        [Space]
        public UnityEvent<bool> onGrab;

        private OVRSkeleton _skeleton;

        private Transform _srcControllerTransform;

        private Transform _srcRootTransform;
        private Transform _srcPalmTransform;
        private Transform _srcThumbTransform;
        private Transform _srcIndexTransform;
        private Transform _srcRingTransform;

        private Transform _dstRootTransform;
        private Transform _dstPalmTransform;
        private Transform _dstThumbTransform;
        private Transform _dstIndexTransform;
        private Transform _dstRingTransform;

        private bool _isHandTracking;
        private bool _isGrabbing;

        private bool IsTracked => hand is not null && hand.IsTracked;

        private HandType SkeletonType
        {
            get
            {
                if (_skeleton is null) return HandType.Unknown;
                return _skeleton.GetSkeletonType() switch
                {
                    OVRSkeleton.SkeletonType.HandLeft => HandType.Left,
                    OVRSkeleton.SkeletonType.HandRight => HandType.Right,
                    _ => HandType.Unknown
                };
            }
        }

        private HandType ControllerType
        {
            get
            {
                if (controller is null) return HandType.Unknown;
                return controller.m_controller switch
                {
                    OVRInput.Controller.LTouch or OVRInput.Controller.LHand => HandType.Left,
                    OVRInput.Controller.RTouch or OVRInput.Controller.RHand => HandType.Right,
                    _ => HandType.Unknown
                };
            }
        }

        public HandType Type
        {
            get
            {
                var skeletonType = SkeletonType;
                return skeletonType != HandType.Unknown ? skeletonType : ControllerType;
            }
        }

        private IEnumerator Start()
        {
            _dstRootTransform = root.transform;
            _dstPalmTransform = palm.transform;
            _dstThumbTransform = thumb.transform;
            _dstIndexTransform = index.transform;
            _dstRingTransform = ring.transform;

            if (controller is not null)
            {
                _srcControllerTransform = controller.transform;
            }

            if (hand is null) yield break;
            _skeleton = hand.GetComponent<OVRSkeleton>();
            if (_skeleton is null) yield break;

            while (!_skeleton.IsInitialized) yield return null; // 1フレーム待つ

            foreach (var bone in _skeleton.Bones)
            {
                switch (bone.Id)
                {
                    case OVRSkeleton.BoneId.Hand_WristRoot:
                        _srcRootTransform = bone.Transform;
                        break;
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
        }

        private void Update()
        {
            // Controller に切り替える
            if (_isHandTracking && !IsTracked)
            {
                _isHandTracking = false;
                _dstRootTransform.gameObject.SetActive(false);
                _dstThumbTransform.gameObject.SetActive(false);
                _dstIndexTransform.gameObject.SetActive(false);
                _dstRingTransform.gameObject.SetActive(false);
            }

            // Hand Tracking に切り替える
            if (!_isHandTracking && IsTracked)
            {
                _isHandTracking = true;
                _dstRootTransform.gameObject.SetActive(true);
                _dstThumbTransform.gameObject.SetActive(true);
                _dstIndexTransform.gameObject.SetActive(true);
                _dstRingTransform.gameObject.SetActive(true);
            }

            if (_isHandTracking) SyncSkeleton();
            else SyncController();
        }

        private void SyncController()
        {
            if (controller is null) return;

            _dstPalmTransform.position = _srcControllerTransform.position;
            _dstPalmTransform.rotation = _srcControllerTransform.rotation;

            var yAngle = ControllerType switch
            {
                HandType.Left => 90f,
                HandType.Right => -90f,
                _ => 0f
            };
            _dstPalmTransform.Rotate(0f, yAngle, 0f);
        }

        private void SyncSkeleton()
        {
            if (_skeleton is null || !_skeleton.IsDataValid) return;

            // 手の位置と向きを同期する
            var dstHandTransform = transform;
            var srcHandTransform = _skeleton.transform;
            dstHandTransform.position = srcHandTransform.position;
            dstHandTransform.rotation = srcHandTransform.rotation;

            // 掌の位置と向きを同期する
            // 掌をZ正方向に向けるために回転を加える(VrmForceGeneratorに依存)
            _dstPalmTransform.position = _srcPalmTransform.position;
            _dstPalmTransform.rotation = _srcPalmTransform.rotation;

            var xAngle = SkeletonType switch
            {
                HandType.Left => -90f,
                HandType.Right => 90f,
                _ => 0f
            };
            _dstPalmTransform.Rotate(xAngle, 0f, 0f);

            // 位置を同期する
            _dstRootTransform.position = _srcRootTransform.position;
            _dstThumbTransform.position = _srcThumbTransform.position;
            _dstIndexTransform.position = _srcIndexTransform.position;
            _dstRingTransform.position = _srcRingTransform.position;

            var thumbIndexDistance = Vector3.Distance(_dstThumbTransform.position, _dstIndexTransform.position);
            var isGrabbing = thumbIndexDistance < grabThresholdDistance;
            if (isGrabbing == _isGrabbing) return;
            _isGrabbing = isGrabbing;
            onGrab.Invoke(_isGrabbing);
        }

        public void Grab(bool on)
        {
            if (on == _isGrabbing) return;
            _isGrabbing = on;
            onGrab.Invoke(_isGrabbing);
        }

        public enum HandType
        {
            Left,
            Right,
            Unknown
        }
    }
}
using System.Collections;
using UnityEngine;
using UniVRM10;

namespace VRMarionette.MetaXR
{
    public class VrmMarionetteHand : MonoBehaviour
    {
        public bool detectPinchingEnabled = true;
        public float pinchThresholdDistance = 0.04f;

        public OVRHand hand;
        public OVRControllerHelper controller;

        [Header("Spring Bone Colliders")]
        [Space]
        [SerializeField]
        private VRM10SpringBoneCollider root;

        [SerializeField]
        private VRM10SpringBoneCollider palm;

        [SerializeField]
        private VRM10SpringBoneCollider thumb;

        [SerializeField]
        private VRM10SpringBoneCollider index;

        [SerializeField]
        private VRM10SpringBoneCollider ring;

        public ForceSource ForceSource { private set; get; }
        public OVRSkeleton Skeleton { private set; get; }
        public bool IsHandTracking { private set; get; }
        public bool IsPinching { private set; get; }

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

        private bool IsTracked => hand is not null && hand.IsTracked;

        private HandType SkeletonType
        {
            get
            {
                if (Skeleton is null) return HandType.Unknown;
                return Skeleton.GetSkeletonType() switch
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
            ForceSource = GetComponentInChildren<ForceSource>(true);

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
            Skeleton = hand.GetComponent<OVRSkeleton>();
            if (Skeleton is null) yield break;

            while (!Skeleton.IsInitialized) yield return null; // 1フレーム待つ

            foreach (var bone in Skeleton.Bones)
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
            if (IsHandTracking && !IsTracked)
            {
                IsHandTracking = false;
                // Controller では Palm だけを有効にする
                _dstRootTransform.gameObject.SetActive(false);
                _dstThumbTransform.gameObject.SetActive(false);
                _dstIndexTransform.gameObject.SetActive(false);
                _dstRingTransform.gameObject.SetActive(false);
            }

            // Hand Tracking に切り替える
            if (!IsHandTracking && IsTracked)
            {
                IsHandTracking = true;
                _dstRootTransform.gameObject.SetActive(true);
                _dstThumbTransform.gameObject.SetActive(true);
                _dstIndexTransform.gameObject.SetActive(true);
                _dstRingTransform.gameObject.SetActive(true);
            }

            if (IsHandTracking)
            {
                SyncSkeleton();
                if (detectPinchingEnabled) DetectPinching();
            }
            else
            {
                SyncController();
            }
        }

        private void SyncController()
        {
            if (controller is null) return;

            var srcPalmPosition = _srcControllerTransform.position;

            _dstPalmTransform.position = srcPalmPosition;
            _dstPalmTransform.rotation = _srcControllerTransform.rotation;

            var yAngle = ControllerType switch
            {
                HandType.Left => 90f,
                HandType.Right => -90f,
                _ => 0f
            };
            _dstPalmTransform.Rotate(0f, yAngle, 0f);

            var dstHandTransform = transform;
            dstHandTransform.position = srcPalmPosition;
            dstHandTransform.rotation = _srcControllerTransform.rotation;
        }

        private void SyncSkeleton()
        {
            if (Skeleton is null || !Skeleton.IsDataValid) return;

            var srcPalmPosition = _srcPalmTransform.position;

            // 掌の位置と向きを同期する
            // 掌をZ正方向に向けるために回転を加える(VrmForceGeneratorに依存)
            _dstPalmTransform.position = srcPalmPosition;
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

            // 手の位置と向きを同期する
            var dstHandTransform = transform;
            dstHandTransform.position = srcPalmPosition;
            dstHandTransform.rotation = Quaternion.AngleAxis(xAngle, _dstPalmTransform.up) * _dstPalmTransform.rotation;
        }

        private void DetectPinching()
        {
            var thumbIndexDistance = Vector3.Distance(_dstThumbTransform.position, _dstIndexTransform.position);
            var isPinching = thumbIndexDistance < pinchThresholdDistance;
            Pinch(isPinching);
        }

        public void Pinch(bool on)
        {
            IsPinching = on;
            ForceSource.hold = IsPinching;
        }

        public enum HandType
        {
            Left,
            Right,
            Unknown
        }
    }
}
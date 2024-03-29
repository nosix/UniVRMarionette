using System.Collections;
using UnityEngine;
using UniVRM10;

namespace VRMarionette.MetaXR
{
    public class VrmMarionetteHand : MonoBehaviour
    {
        public bool detectPinchingEnabled = true;
        public float pinchThresholdDistance = 0.04f;

        public int initialPositionSamples = 60;
        public float poseRecoveryDelay = 1;
        public float maxPositionDelta = 0.05f;
        public float maxRotationDelta = 30f;

        public float sigmaWForPosition = 1e-5f;
        public float sigmaVForPosition = 1e-4f;

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

        private PositionInitializer _positionInitializer;
        private float _positionLockStartTime;
        private float _rotationLockStartTime;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private LocalLevelModelKalmanFilter _positionX;
        private LocalLevelModelKalmanFilter _positionY;
        private LocalLevelModelKalmanFilter _positionZ;

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

        public Vector3 Forward => IsHandTracking ? _dstRootTransform.forward : _dstPalmTransform.forward;
        public Vector3 Up => _dstPalmTransform.up;

        private void OnEnable()
        {
            ResetSkeletonPose();
        }

        private void Awake()
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

            _positionX = new LocalLevelModelKalmanFilter(sigmaWForPosition, sigmaVForPosition);
            _positionY = new LocalLevelModelKalmanFilter(sigmaWForPosition, sigmaVForPosition);
            _positionZ = new LocalLevelModelKalmanFilter(sigmaWForPosition, sigmaVForPosition);
        }

        private IEnumerator Start()
        {
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

        private void ResetSkeletonPose()
        {
            _positionInitializer = new PositionInitializer(initialPositionSamples, maxPositionDelta);
        }

        private void SyncSkeleton()
        {
            if (Skeleton is null || !Skeleton.IsDataValid)
            {
                ResetSkeletonPose();
                return;
            }

            // ハンドトラッキングによって Hand Anchor の位置と回転が変化する
            // Hand Anchor の位置と回転が急激に変化した場合には
            // Hand の位置と回転を調整して移動と回転を打ち消す

            var handAnchorTransform = hand.transform.parent;
            var handAnchorPosition = handAnchorTransform.position;
            var handAnchorRotation = handAnchorTransform.rotation;

            // 初期位置がおかしな位置で固定されない様にする
            if (_positionInitializer is not null)
            {
                _positionInitializer.AddSample(handAnchorPosition);
                if (!_positionInitializer.TryGet(out _lastPosition)) return;
                _lastRotation = handAnchorRotation;
                _positionLockStartTime = Time.time;
                _rotationLockStartTime = Time.time;
                _positionInitializer = null;
                return;
            }

            // 位置が一定時間変化しなかった場合は位置を初期化する
            if (Time.time - _positionLockStartTime > poseRecoveryDelay ||
                Time.time - _rotationLockStartTime > poseRecoveryDelay)
            {
                ResetSkeletonPose();
                return;
            }

            // 急激な移動は無視する
            var positionDelta = _lastPosition - handAnchorPosition;
            if (positionDelta.magnitude > maxPositionDelta)
            {
                hand.transform.localPosition = handAnchorTransform.InverseTransformVector(positionDelta);
            }
            else
            {
                hand.transform.localPosition = Vector3.zero;
                _lastPosition = handAnchorPosition;
                _positionLockStartTime = Time.time;
            }

            // 急激な回転は無視する
            var rotationDelta = Quaternion.Inverse(handAnchorRotation) * _lastRotation;
            if (Quaternion.Angle(Quaternion.identity, rotationDelta) > maxRotationDelta)
            {
                hand.transform.localRotation = rotationDelta;
            }
            else
            {
                hand.transform.localRotation = Quaternion.identity;
                _lastRotation = handAnchorRotation;
                _rotationLockStartTime = Time.time;
            }

            // カルマンフィルターで平滑化する
            _positionX.Update(ref handAnchorPosition.x);
            _positionY.Update(ref handAnchorPosition.y);
            _positionZ.Update(ref handAnchorPosition.z);
            hand.transform.position += handAnchorPosition - _lastPosition;

            // 掌の位置と向きを同期する
            // 掌をZ正方向に向けるために回転を加える(VrmForceGeneratorに依存)
            var srcPalmPosition = _srcPalmTransform.position;
            _dstPalmTransform.position = srcPalmPosition;
            _dstRootTransform.position = _srcRootTransform.position;
            _dstPalmTransform.rotation = _srcPalmTransform.rotation;
            _dstRootTransform.rotation = _srcRootTransform.rotation;

            var xAngle = SkeletonType switch
            {
                HandType.Left => -90f,
                HandType.Right => 90f,
                _ => 0f
            };
            _dstPalmTransform.Rotate(xAngle, 0f, 0f);
            _dstRootTransform.Rotate(xAngle, 0f, 0f);

            // 位置を同期する
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
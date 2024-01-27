using System;
using UnityEngine;

namespace VRMarionette
{
    public class PostureControlFall : MonoBehaviour, IPostureControl
    {
        private HumanoidManipulator _manipulator;
        private ForceResponder _forceResponder;
        private GravityApplier _gravityApplier;
        private Animator _animator;

        private float _upperArmLength;
        private float _upperLegLength;
        private float _lowerLegLength;
        private FallLandingAction _fallLandingAction;

        public void Initialize(GameObject instance)
        {
            _manipulator = instance.GetComponent<HumanoidManipulator>() ?? throw new InvalidOperationException(
                "The PostureControlFall component requires HumanoidManipulator component.");
            _forceResponder = instance.GetComponent<ForceResponder>() ?? throw new InvalidOperationException(
                "The PostureControlFall component requires ForceResponder component.");
            _gravityApplier = instance.GetComponent<GravityApplier>() ?? throw new InvalidOperationException(
                "The PostureControlFall component requires GravityApplier component.");
            _animator = instance.GetComponent<Animator>() ?? throw new InvalidOperationException(
                "The PostureControlFall component requires Animator component.");

            // 身体の中心軸から肘までの長さを測定する
            var neckPosition = _animator.GetBoneTransform(HumanBodyBones.Neck).position;
            var lowerArmPosition = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
            _upperArmLength = Vector3.Distance(neckPosition, lowerArmPosition);

            // 左右の脚の長さは同じと想定して左脚だけを測定する
            var hipsPosition = _animator.GetBoneTransform(HumanBodyBones.Hips).position;
            var lowerLegPosition = _animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position;
            var footPosition = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            _upperLegLength = Vector3.Distance(hipsPosition, lowerLegPosition);
            _lowerLegLength = Vector3.Distance(lowerLegPosition, footPosition);
        }

        public bool SetPostureControlState(bool isEnabled)
        {
            if (isEnabled)
            {
                if (_fallLandingAction is not null) return false;

                _fallLandingAction = ChoiceFallLandingAction();
                _fallLandingAction.Start();
            }
            else
            {
                _fallLandingAction = null;
            }

            return true;
        }

        private void Update()
        {
            if (_fallLandingAction is null) return;

            if (_fallLandingAction.IsFinished)
            {
                _fallLandingAction = ChoiceFallLandingAction();
                _fallLandingAction.Start();
            }
            else
            {
                _fallLandingAction.Update();
            }
        }

        private FallLandingAction ChoiceFallLandingAction()
        {
            var hipsTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);
            var hipsPosition = hipsTransform.position;
            var fallingDirection = GetFallingDirection(hipsPosition, _gravityApplier.CentroidPosition);
            var isFallingForward = IsFallingForward(hipsTransform, fallingDirection);
            var isHandAboveFoot = IsHandAboveFoot(_animator);
            var isFootForward = IsFootForward(
                hipsPosition,
                _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position,
                _animator.GetBoneTransform(HumanBodyBones.RightFoot).position,
                _gravityApplier.CentroidPosition
            );
            return !isHandAboveFoot
                ? new FallAndLandOnFoot(this, _animator)
                : !isFallingForward
                    ? new FallBackwardLandOnHips(this, _animator)
                    : isFootForward
                        ? new FallAndLandOnFoot(this, _animator)
                        : new FallForwardLandOnHand(this, _animator);
        }

        private static bool IsHandAboveFoot(Animator animator)
        {
            var leftHandPosition = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            var rightHandPosition = animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            var leftFootPosition = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            var rightFootPosition = animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
            return Mathf.Min(leftHandPosition.y, rightHandPosition.y) >
                   Mathf.Min(leftFootPosition.y, rightFootPosition.y);
        }

        private static Vector3 GetFallingDirection(Vector3 hipsPosition, Vector3 centroidPosition)
        {
            return (centroidPosition - hipsPosition).normalized;
        }

        private static bool IsFallingForward(Transform hipsTransform, Vector3 fallingDirection)
        {
            var localFallingDirection = hipsTransform.InverseTransformDirection(fallingDirection);
            return localFallingDirection.z > 0f;
        }

        private static bool IsFootForward(
            Vector3 hipsPosition,
            Vector3 leftFootPosition,
            Vector3 rightFootPosition,
            Vector3 centroidPosition
        )
        {
            var localCentroidPosition = centroidPosition - hipsPosition;
            var localLeftFootPosition = leftFootPosition - hipsPosition;
            var localRightFootPosition = rightFootPosition - hipsPosition;
            // XZ 平面における方向と距離を調べる
            localCentroidPosition.y = 0;
            localLeftFootPosition.y = 0;
            localRightFootPosition.y = 0;
            var centroidDistance = localCentroidPosition.magnitude;
            var leftDistance = localLeftFootPosition.magnitude *
                               Vector3.Dot(localLeftFootPosition.normalized, localCentroidPosition.normalized);
            var rightDistance = localRightFootPosition.magnitude *
                                Vector3.Dot(localRightFootPosition.normalized, localCentroidPosition.normalized);
            return leftDistance > centroidDistance || rightDistance > centroidDistance;
        }

        /// <summary>
        /// 指定した点が地面に落下したときの座標を求める
        /// 接地点を中心として、重心と対象の点を回転させて接地した点を用いる
        /// ratio=0 の場合は接地した重心を返す
        /// ratio=1 の場合は接地した対象の点を返す
        /// </summary>
        /// <param name="groundPosition">接地点</param>
        /// <param name="centroidPosition">重心</param>
        /// <param name="position">対象の点</param>
        /// <param name="ratio">接地した点における対象の点の比率</param>
        /// <returns></returns>
        private static Vector3 GetPositionOnGroundPlane(
            Vector3 groundPosition,
            Vector3 centroidPosition,
            Vector3 position,
            float ratio
        )
        {
            var centroidLocalPosition = centroidPosition - groundPosition;
            var localPosition = position - groundPosition;

            // 重心の位置と対象の位置を通る平面上で回転して XZ 平面上の点を求める
            var rotationAxis = centroidLocalPosition.y > localPosition.y
                ? Vector3.Cross(centroidLocalPosition, localPosition)
                : Vector3.Cross(localPosition, centroidLocalPosition);
            var estimatedCentroidLocalPosition = GetPointOnXZPlaneAfterRotation(centroidLocalPosition, rotationAxis);
            var estimatedLocalPosition = GetPointOnXZPlaneAfterRotation(localPosition, rotationAxis);

            // 重心の位置と対象の位置の間にある点を Ground 平面上の点として使う
            var nextLocalPosition = Vector3.Lerp(estimatedCentroidLocalPosition, estimatedLocalPosition, ratio);
            return nextLocalPosition + groundPosition;
        }

        private static Vector3 GetPointOnXZPlaneAfterRotation(Vector3 localPosition, Vector3 rotationAxis)
        {
            // 点 P (localPosition) を XZ 平面上に回転させた時の回転角度の近似値を求める
            // 点 P を Y 軸と平行に XZ 平面上に投影した点 Pxz とする
            // ベクトル OP とベクトル OPxz の作る角度を回転角度の近似値とする
            var approxRotationAngle = Mathf.Asin(localPosition.y / localPosition.magnitude) * Mathf.Rad2Deg;
            var pxz = Quaternion.AngleAxis(approxRotationAngle, rotationAxis) * localPosition;

            // 線分 PPxz 上の点で Y=0 となる点 Q を求める
            // Q = P + t(Pxz - P) であり、Y=0 なので t = - P.y / (Pxz.y - P.y)
            // 但し、Pxz と P の Y 座標がほぼ同じの場合には回転を行わない
            var dp = pxz.y - localPosition.y;
            if (Mathf.Approximately(dp, 0f)) return localPosition;
            var t = -localPosition.y / dp;
            var q = localPosition + t * (pxz - localPosition);

            // 点 Q は XZ 平面上の点であり、Y=0 となる点である
            // ベクトル OQ の方向で長さがベクトル OP と同じになる点が点 P を回転させた点となる
            return q.normalized * localPosition.magnitude;
        }

        private class ControlPoint
        {
            public Transform Transform { get; }

            public Vector3 BottomPosition => _property.ToBottomPosition(Transform.position);

            private readonly BoneProperty _property;

            public ControlPoint(Transform transform, BoneProperties properties)
            {
                Transform = transform;
                _property = properties.Get(transform);
            }

            public Vector3 ToPosition(Vector3 bottomPosition)
            {
                return _property.ToPosition(bottomPosition);
            }

            public Vector3 ToBottomPosition(Vector3 position)
            {
                return _property.ToBottomPosition(position);
            }
        }

        private abstract class FallLandingAction
        {
            private readonly PostureControlFall _instance;

            private float _timeToFall;
            private float _timePassed;

            private float DeltaRatio
            {
                get
                {
                    var timeRemaining = _timeToFall - _timePassed;
                    return Time.deltaTime < timeRemaining ? Time.deltaTime / timeRemaining : 1f;
                }
            }

            protected ControlPoint Head { get; }
            protected ControlPoint Hips { get; }
            protected ControlPoint LeftHand { get; }
            protected ControlPoint RightHand { get; }
            protected ControlPoint LeftFoot { get; }
            protected ControlPoint RightFoot { get; }

            protected Vector3 OriginPosition { private set; get; }
            protected Vector3 BaseGoalPosition { private set; get; }

            protected Vector3 GroundPosition => _instance._gravityApplier.GroundPosition;
            protected Vector3 CentroidPosition => _instance._gravityApplier.CentroidPosition;
            protected float UpperLegLength => _instance._upperLegLength;
            protected float LowerLegLength => _instance._lowerLegLength;

            public bool IsFinished => _timePassed >= _timeToFall;

            protected FallLandingAction(PostureControlFall instance, Animator animator)
            {
                _instance = instance;

                var boneProperties = instance._forceResponder.BoneProperties;
                Head = new ControlPoint(animator.GetBoneTransform(HumanBodyBones.Head), boneProperties);
                Hips = new ControlPoint(animator.GetBoneTransform(HumanBodyBones.Hips), boneProperties);
                LeftHand = new ControlPoint(animator.GetBoneTransform(HumanBodyBones.LeftHand), boneProperties);
                RightHand = new ControlPoint(animator.GetBoneTransform(HumanBodyBones.RightHand), boneProperties);
                LeftFoot = new ControlPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot), boneProperties);
                RightFoot = new ControlPoint(animator.GetBoneTransform(HumanBodyBones.RightFoot), boneProperties);
            }

            private Vector3 GetBoneRotation(HumanBodyBones bone)
            {
                return _instance._manipulator.GetBoneRotation(bone);
            }

            /// <summary>
            /// 現在の位置と地面の位置から落下時間を設定し、経過時間をリセットする
            /// </summary>
            /// <param name="currentPosition"></param>
            /// <param name="groundPosition"></param>
            private void SetTime(Vector3 currentPosition, Vector3 groundPosition)
            {
                _timeToFall = GetTimeToFall(currentPosition, groundPosition);
                _timePassed = 0f;
            }

            /// <summary>
            /// 自由落下に要する時間を求める
            /// </summary>
            /// <param name="currentPosition">現在の位置</param>
            /// <param name="groundPosition">地面の位置</param>
            /// <returns>落下時間(秒)</returns>
            private static float GetTimeToFall(Vector3 currentPosition, Vector3 groundPosition)
            {
                var height = (currentPosition - groundPosition).y;
                var g = Mathf.Abs(Physics.gravity.y);
                return Mathf.Sqrt(2 * height / g);
            }

            /// <summary>
            /// 経過時間を増やす
            /// </summary>
            protected void Tick()
            {
                _timePassed += Time.deltaTime;
            }

            protected void Finish()
            {
                _timePassed = _timeToFall;
            }

            /// <summary>
            /// 基準の目標地点を設定する
            /// </summary>
            /// <param name="controlPoint">基準</param>
            /// <param name="ratio">落下地点を基準に寄せるなら 1.0、重心に寄せるなら 0.0</param>
            protected void SetGoal(ControlPoint controlPoint, float ratio)
            {
                var groundPosition = GroundPosition;
                var centroidPosition = CentroidPosition;
                var basePosition = controlPoint.BottomPosition;

                // 基準が接地していたら移動しない
                if (basePosition.y - groundPosition.y < _instance._gravityApplier.nearDistance) return;

                // 基準が落下する位置を推定する
                var basePositionOnGround = GetPositionOnGroundPlane(
                    groundPosition,
                    centroidPosition,
                    basePosition,
                    ratio
                );

                // 基準の目標地点を決める
                OriginPosition = controlPoint.ToPosition(groundPosition);
                BaseGoalPosition = controlPoint.ToPosition(basePositionOnGround);

                // 基準の移動時間を基準にして落下時間を設定する
                SetTime(basePosition, basePositionOnGround);
            }

            protected bool IsOnGround(ControlPoint controlPoint)
            {
                return controlPoint.BottomPosition.y - GroundPosition.y < _instance._gravityApplier.nearDistance;
            }

            protected bool IsAboveGoalHeight(ControlPoint controlPoint)
            {
                return controlPoint.BottomPosition.y - BaseGoalPosition.y > Mathf.Epsilon;
            }

            private Vector3 GetMovement(Vector3 currentPosition, Vector3 goalPosition)
            {
                var distance = goalPosition - currentPosition;
                return Vector3.Lerp(Vector3.zero, distance, DeltaRatio);
            }

            private Vector3 GetMovement(Vector3 currentPosition, Vector3 goalPosition, Vector3 originPosition)
            {
                // 1: currPosition の円周上、2: goalPosition の円周上
                var currPosition1 = currentPosition - originPosition;
                var goalPosition2 = goalPosition - originPosition;

                var angle = Vector3.Angle(currPosition1, goalPosition2);
                var deltaAngle = angle * DeltaRatio;
                var rotationAxis = Vector3.Cross(currPosition1, goalPosition2);
                var rotation = Quaternion.AngleAxis(deltaAngle, rotationAxis);

                var currPosition2 = goalPosition2.magnitude * currPosition1.normalized;
                var nextPosition1 = rotation * currPosition1;
                var nextPosition2 = rotation * currPosition2;
                var movement1 = nextPosition1 - currPosition1;
                var movement2 = nextPosition2 - currPosition2;

                return Vector3.Lerp(movement1, movement2, DeltaRatio);
            }

            private void Move(Transform target, Vector3 forcePoint, Vector3 movement, bool allowBodyMovement)
            {
                _instance._forceResponder.QueueForce(
                    target,
                    forcePoint,
                    movement,
                    Quaternion.identity,
                    allowMultiSource: false,
                    allowBodyMovement: allowBodyMovement
                );
            }

            protected void UpdatePosition(
                Transform targetTransform,
                Vector3 currentPosition,
                Vector3 goalPosition,
                bool allowBodyMovement
            )
            {
                var groundPosition = GroundPosition;

                if (currentPosition.y - groundPosition.y < Mathf.Epsilon) return;

                var movement = GetMovement(currentPosition, goalPosition);
                Move(targetTransform, currentPosition, movement, allowBodyMovement);
            }

            protected void UpdatePosition(
                Transform targetTransform,
                Vector3 currentPosition,
                Vector3 goalPosition,
                Vector3 originPosition,
                bool allowBodyMovement
            )
            {
                var groundPosition = GroundPosition;

                if (currentPosition.y - groundPosition.y < Mathf.Epsilon) return;

                var movement = GetMovement(currentPosition, goalPosition, originPosition);
                Move(targetTransform, currentPosition, movement, allowBodyMovement);
            }

            private Quaternion GetRotation(Quaternion currentRotation, Quaternion goalRotation)
            {
                var rotation = goalRotation * Quaternion.Inverse(currentRotation);
                return Quaternion.Lerp(Quaternion.identity, rotation, DeltaRatio);
            }

            private void Rotate(Transform target, Quaternion rotation)
            {
                _instance._forceResponder.QueueForce(
                    target,
                    target.position,
                    Vector3.zero,
                    rotation,
                    allowMultiSource: false,
                    allowBodyMovement: true
                );
            }

            protected void UpdateRotation(Transform targetTransform, Quaternion goalRotation)
            {
                var currentRotation = targetTransform.rotation;

                if (Quaternion.Angle(currentRotation, goalRotation) < Mathf.Epsilon) return;

                var rotation = GetRotation(currentRotation, goalRotation);
                Rotate(targetTransform, rotation);
            }

            protected void UpdateHands(ControlPoint baseControlPoint, Vector3 baseGoalPosition)
            {
                baseGoalPosition = baseControlPoint.ToBottomPosition(baseGoalPosition);
                var headYRotation = GetHeadYRotation();

                UpdateHand(
                    LeftHand,
                    HumanBodyBones.LeftUpperArm,
                    Vector3.left,
                    baseGoalPosition,
                    headYRotation
                );
                UpdateHand(
                    RightHand,
                    HumanBodyBones.RightUpperArm,
                    Vector3.right,
                    baseGoalPosition,
                    headYRotation
                );
            }

            private void UpdateHand(
                ControlPoint hand,
                HumanBodyBones upperArmBone,
                Vector3 handDirection,
                Vector3 baseGoalPosition,
                Quaternion headYRotation
            )
            {
                if (!IsAboveGoalHeight(hand)) return;

                UpdateUpperArmRotation(upperArmBone, hand.Transform.parent.parent);
                UpdateHandPosition(
                    hand.Transform,
                    baseGoalPosition,
                    handDirection,
                    headYRotation
                );
                UpdateHandRotation(hand.Transform);
            }

            private Quaternion GetHeadYRotation()
            {
                // XZ 平面における Hips から Head への方向を得る
                var direction = Head.Transform.position - Hips.Transform.position;
                direction.y = 0;
                // forward と比較して Y 軸まわりの回転を得る
                return Quaternion.FromToRotation(Vector3.forward, direction);
            }

            private void UpdateHandPosition(
                Transform handTransform,
                Vector3 targetGoalPosition,
                Vector3 handDirection,
                Quaternion yRotation
            )
            {
                var handGoalPosition =
                    targetGoalPosition + _instance._upperArmLength * (yRotation * handDirection);

                // Hand を動かすと Hand が回転してしまうため LowerArm を移動する
                UpdatePosition(
                    handTransform.parent,
                    handTransform.position,
                    handGoalPosition,
                    allowBodyMovement: false
                );
            }

            private void UpdateUpperArmRotation(HumanBodyBones upperArmBone, Transform upperArmTransform)
            {
                var shoulderTransform = upperArmTransform.parent;
                // UpperChest の向いている方向のベクトルを得る
                var upperChestForwardDirection = shoulderTransform.parent.rotation * Vector3.forward;
                // 真下方向と UpperChest の向いている方向の間の角度を得る
                var deltaAngle = Vector3.Angle(Vector3.down, upperChestForwardDirection);
                // 0度(下)、90度(水平)、180度(上)に合わせて UpperArm の回転を決める
                var upperArmAngleX = deltaAngle - 90f;

                var localEulerAngles = GetBoneRotation(upperArmBone);
                var goalRotation =
                    shoulderTransform.rotation *
                    Quaternion.Euler(upperArmAngleX, localEulerAngles.y, localEulerAngles.z);
                UpdateRotation(upperArmTransform, goalRotation);
            }

            private void UpdateHandRotation(Transform handTransform)
            {
                var lowerArmRotation = handTransform.parent.rotation;
                var handLocalRotation = Quaternion.Inverse(lowerArmRotation);
                UpdateRotation(handTransform, handLocalRotation * lowerArmRotation);
            }

            public abstract void Start();
            public abstract void Update();
        }

        private class FallForwardLandOnHand : FallLandingAction
        {
            public FallForwardLandOnHand(PostureControlFall instance, Animator animator)
                : base(instance, animator)
            {
            }

            public override void Start()
            {
                SetGoal(Head, 1f);
            }

            public override void Update()
            {
                // Head か Head が目標地点の高さに到達していれば終了する
                if (!IsAboveGoalHeight(Head) ||
                    (!IsAboveGoalHeight(LeftHand) && !IsAboveGoalHeight(RightHand)))
                {
                    Finish();
                    return;
                }

                // 首は曲げずに Head の位置を変えるために UpperChest を移動する
                UpdatePosition(
                    Head.Transform.parent.parent,
                    Head.Transform.position,
                    BaseGoalPosition,
                    OriginPosition,
                    allowBodyMovement: true
                );

                UpdateHipsRotation();
                UpdateHands(Head, BaseGoalPosition);
                UpdateFeetPosition();

                Tick();
            }

            private void UpdateHipsRotation()
            {
                // XZ 軸方向では Head の方向に合わせて Hips を回転する
                // Y 軸方向は水平を目標にする
                var headDirection = Head.Transform.position - Hips.Transform.position;
                headDirection.y = 0;
                var hipsGoalRotation = Quaternion.FromToRotation(Vector3.up, headDirection);
                UpdateRotation(Hips.Transform, hipsGoalRotation);
            }

            private void UpdateFeetPosition()
            {
                var hipsPosition = Hips.Transform.position;
                var headToHips = hipsPosition - Head.Transform.position;

                UpdateFootPosition(
                    LeftFoot.Transform,
                    hipsPosition,
                    headToHips
                );
                UpdateFootPosition(
                    RightFoot.Transform,
                    hipsPosition,
                    headToHips
                );
            }

            private void UpdateFootPosition(
                Transform footTransform,
                Vector3 hipsPosition,
                Vector3 direction
            )
            {
                // Foot と Hips を同じ高さにして脚を伸ばした状態にする
                direction.y = 0;
                var footGoalPosition = hipsPosition + (UpperLegLength + LowerLegLength) * direction.normalized;

                // 足は動かさずに足の位置を動かすために LowerLeg を動かす
                UpdatePosition(
                    footTransform.parent,
                    footTransform.position,
                    footGoalPosition,
                    allowBodyMovement: false
                );
            }
        }

        private class FallBackwardLandOnHips : FallLandingAction
        {
            public FallBackwardLandOnHips(PostureControlFall instance, Animator animator)
                : base(instance, animator)
            {
            }

            public override void Start()
            {
                SetGoal(Hips, 0.5f);
            }

            public override void Update()
            {
                var hipsPosition = Hips.Transform.position;

                // Hips が目標地点の高さに到達したら終了する
                if (!IsAboveGoalHeight(Hips))
                {
                    Finish();
                    return;
                }

                UpdatePosition(
                    Hips.Transform,
                    hipsPosition,
                    BaseGoalPosition,
                    OriginPosition,
                    allowBodyMovement: true
                );

                UpdateHands(Head, BaseGoalPosition);

                var centroidPosition = CentroidPosition;
                UpdateFootPosition(LeftFoot, hipsPosition, centroidPosition);
                UpdateFootPosition(RightFoot, hipsPosition, centroidPosition);

                Tick();
            }

            private void UpdateFootPosition(
                ControlPoint foot,
                Vector3 hipsPosition,
                Vector3 centroidPosition
            )
            {
                if (!IsAboveGoalHeight(foot)) return;

                var footPosition = foot.Transform.position;

                // XZ 平面における Hips から Foot への方向を求める
                var v = footPosition - hipsPosition;
                v.y = 0;
                var toFootDirection = v.normalized;

                // Foot の目標地点は Hips を基点とし、Centroid と反対方向に離れていく
                var toCentroid = (centroidPosition - hipsPosition).ProjectOnto(toFootDirection);
                var distance = UpperLegLength + LowerLegLength;
                var footGoalPosition = hipsPosition + distance * toFootDirection - toCentroid;
                footGoalPosition.y = GroundPosition.y;

                // Foot は動かさずに Foot の位置を変えるために UpperLeg を移動する
                UpdatePosition(
                    foot.Transform.parent,
                    footPosition,
                    footGoalPosition,
                    allowBodyMovement: false
                );
            }
        }

        private class FallAndLandOnFoot : FallLandingAction
        {
            private ControlPoint _baseFoot;
            private ControlPoint _otherFoot;

            public FallAndLandOnFoot(PostureControlFall instance, Animator animator)
                : base(instance, animator)
            {
            }

            public override void Start()
            {
                // 接地していない Foot のうち低い方の Foot を Base Foot とする
                var isRightFootLowerThanLeftFoot = RightFoot.Transform.position.y < LeftFoot.Transform.position.y;

                if (isRightFootLowerThanLeftFoot)
                {
                    if (!IsOnGround(RightFoot)) _baseFoot = RightFoot;
                    if (!IsOnGround(LeftFoot)) _otherFoot = LeftFoot;
                }
                else
                {
                    if (!IsOnGround(LeftFoot)) _baseFoot = LeftFoot;
                    if (!IsOnGround(RightFoot)) _otherFoot = RightFoot;
                }

                if (_baseFoot is null)
                {
                    _baseFoot = _otherFoot;
                    _otherFoot = null;
                }

                if (_baseFoot is not null) SetGoal(_baseFoot, 0.5f);
            }

            public override void Update()
            {
                if (!IsAboveGoalHeight(_baseFoot))
                {
                    Finish();
                    return;
                }

                UpdatePosition(
                    _baseFoot.Transform.parent,
                    _baseFoot.Transform.position,
                    BaseGoalPosition,
                    OriginPosition,
                    allowBodyMovement: false
                );

                if (_otherFoot is not null)
                {
                    UpdatePosition(
                        _otherFoot.Transform.parent,
                        _otherFoot.Transform.position,
                        BaseGoalPosition,
                        OriginPosition,
                        allowBodyMovement: false
                    );
                }

                UpdateHipsRotation();

                if (_baseFoot.BottomPosition.y < Head.BottomPosition.y)
                {
                    UpdateHands(_baseFoot, BaseGoalPosition);
                }
                else
                {
                    UpdateHands(Head, Head.Transform.position);
                }

                Tick();
            }

            private void UpdateHipsRotation()
            {
                var isRightFootLowerThanLeftFoot = RightFoot.Transform.position.y < LeftFoot.Transform.position.y;
                var foot = isRightFootLowerThanLeftFoot ? RightFoot : LeftFoot;

                // Foot から Hips の方向に合わせて Hips を回転する
                var hipsDirection = Hips.Transform.position - foot.Transform.position;
                var hipsGoalRotation = Quaternion.FromToRotation(Vector3.up, hipsDirection);
                UpdateRotation(Hips.Transform, hipsGoalRotation);
            }
        }
    }
}
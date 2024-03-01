using System;
using UnityEngine;

namespace VRMarionette
{
    public class PostureControlFall : MonoBehaviour, IPostureControl
    {
        // TODO 後で消す
        public Transform goal;

        private const float Epsilon = 0.001f;

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
            // TODO 後で消す
            Debug.Log($"SetPostureControlState {isEnabled}");
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
            if (!IsOnGround()) return new FreeFall(this, _animator);
            if (!IsHandAboveFoot()) return new FallAndLandOnFoot(this, _animator);
            if (!IsFallingForward()) return new FallBackwardLandOnHips(this, _animator);
            return IsFootForward()
                ? new FallAndLandOnFoot(this, _animator)
                : new FallForwardLandOnHand(this, _animator);
        }

        private bool IsHandAboveFoot()
        {
            var leftHandPosition = _animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            var rightHandPosition = _animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            var leftFootPosition = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            var rightFootPosition = _animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
            return Mathf.Min(leftHandPosition.y, rightHandPosition.y) >
                   Mathf.Min(leftFootPosition.y, rightFootPosition.y);
        }

        private Vector3 GetFallingDirection()
        {
            // BottomPosition で接地している前提とする
            var centroidPosition = _gravityApplier.CentroidPosition;
            var groundPosition = _gravityApplier.BottomPosition;
            return new Vector3(
                centroidPosition.x - groundPosition.x,
                groundPosition.y - centroidPosition.y,
                centroidPosition.z - groundPosition.z
            ).normalized;
        }

        private bool IsFallingForward()
        {
            var fallingDirection = GetFallingDirection();
            var hipsTransform = _animator.GetBoneTransform(HumanBodyBones.Hips);
            var localFallingDirection = hipsTransform.InverseTransformDirection(fallingDirection);
            return localFallingDirection.z > 0f;
        }

        private bool IsFootForward()
        {
            // BottomPosition で接地している前提とする
            var hipsPosition = _animator.GetBoneTransform(HumanBodyBones.Hips).position;
            var leftFootPosition = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            var rightFootPosition = _animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
            var centroidPosition = _gravityApplier.CentroidPosition;
            var groundPosition = _gravityApplier.BottomPosition;

            var estimatedHipsPositionOnGround =
                GetPositionOnGroundPlane(groundPosition, centroidPosition, hipsPosition, 1f);
            var baseDirection = estimatedHipsPositionOnGround - groundPosition;

            var hipsAngle = Vector3.Angle(baseDirection, hipsPosition - groundPosition);
            var leftFootAngle = Vector3.Angle(baseDirection, leftFootPosition - groundPosition);
            var rightFootAngle = Vector3.Angle(baseDirection, rightFootPosition - groundPosition);

            var hipsDistance = Vector3.Distance(groundPosition, hipsPosition);
            var leftFootDistance = Vector3.Distance(groundPosition, leftFootPosition);
            var rightFootDistance = Vector3.Distance(groundPosition, rightFootPosition);

            var isLeftFootForward = leftFootAngle < hipsAngle && leftFootDistance > hipsDistance;
            var isRightFootForward = rightFootAngle < hipsAngle && rightFootDistance > hipsDistance;

            return isLeftFootForward || isRightFootForward;
        }

        private bool IsOnGround()
        {
            // 誤差により僅かに地面を突き抜けている場合があるので少し上の場所から下方向に地面を探す
            var bottomPosition = _gravityApplier.BottomPosition;
            bottomPosition.y += Epsilon;
            var distanceToGround = MeasureDistanceToGround(bottomPosition);
            return distanceToGround < _gravityApplier.nearDistance;
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
            var localCentroidPosition = centroidPosition - groundPosition;
            var localPosition = position - groundPosition;

            var estimatedLocalCentroidPosition = GetPointOnXZPlaneAfterRotation(localCentroidPosition);
            var estimatedLocalPosition = GetPointOnXZPlaneAfterRotation(localPosition);

            var nextLocalPosition = Vector3.Lerp(estimatedLocalCentroidPosition, estimatedLocalPosition, ratio);
            return nextLocalPosition + groundPosition;
        }

        private static Vector3 GetPointOnXZPlaneAfterRotation(Vector3 localPosition)
        {
            // 回転して XZ 平面に到達したとき
            // 方向は localPosition を XZ 平面に投影したベクトルの延長線上であり
            // 長さは localPosition に等しい
            var direction = localPosition;
            direction.y = 0;
            direction = direction.normalized;
            return localPosition.magnitude * direction;
        }

        /// <summary>
        /// 指定した地点の真下にある地面までの距離を測る
        /// </summary>
        /// <param name="position">指定地点</param>
        /// <returns>地面までの距離(地面が存在しない場合は Infinity)</returns>
        private static float MeasureDistanceToGround(Vector3 position)
        {
            var ray = new Ray(position, Vector3.down);
            return Physics.Raycast(ray, out var hit) ? hit.distance : Mathf.Infinity;
        }

        private class ControlPoint
        {
            public Transform Transform => _property.Transform;

            public Vector3 BottomPosition => _property.GetBottomPosition();

            private readonly BoneProperty _property;

            public ControlPoint(Transform transform, BoneProperties properties)
            {
                _property = properties.Get(transform);
            }
        }

        private abstract class FallLandingAction
        {
            private readonly PostureControlFall _instance;

            private float _timeToFall = Mathf.Infinity;
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

            protected Vector3 BottomPosition => _instance._gravityApplier.BottomPosition;
            protected Vector3 CentroidPosition => _instance._gravityApplier.CentroidPosition;
            protected float UpperLegLength => _instance._upperLegLength;
            protected float LowerLegLength => _instance._lowerLegLength;
            protected bool IsOnGround => _instance.IsOnGround();

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
                // TODO 後で消す
                if (IsFinished) Debug.Log("Time up");
            }

            protected void Finish()
            {
                // TODO 後で消す
                Debug.Log("Finish");
                _timePassed = _timeToFall;
            }

            /// <summary>
            /// 基準の目標地点を設定する
            /// </summary>
            /// <param name="controlPoint">基準</param>
            /// <param name="ratio">落下地点を基準に寄せるなら 1.0、重心に寄せるなら 0.0</param>
            protected void SetGoal(ControlPoint controlPoint, float ratio)
            {
                // BottomPosition は接地している前提とする
                var groundPosition = BottomPosition;
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
                OriginPosition = groundPosition;
                BaseGoalPosition = basePositionOnGround;

                // TODO 後で消す
                if (_instance.goal) _instance.goal.position = BaseGoalPosition;

                // 基準の移動時間を基準にして落下時間を設定する
                SetTime(basePosition, basePositionOnGround);
            }

            protected bool IsOnBottom(ControlPoint controlPoint)
            {
                return controlPoint.BottomPosition.y - BottomPosition.y < Epsilon;
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
                    isPushing: false,
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
                var bottomPosition = BottomPosition;

                if (currentPosition.y - bottomPosition.y < Mathf.Epsilon) return;

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
                var bottomPosition = BottomPosition;

                if (currentPosition.y - bottomPosition.y < Mathf.Epsilon) return;

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
                    isPushing: false,
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

            protected void UpdateHands(Vector3 baseGoalPosition)
            {
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
                if (IsOnBottom(hand)) return;

                UpdateUpperArmRotation(upperArmBone, hand.Transform.parent.parent);
                UpdateHandPosition(
                    hand,
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
                ControlPoint hand,
                Vector3 targetGoalPosition,
                Vector3 handDirection,
                Quaternion yRotation
            )
            {
                var handGoalPosition =
                    targetGoalPosition + _instance._upperArmLength * (yRotation * handDirection);

                // Hand を動かすと Hand が回転してしまうため LowerArm を移動する
                UpdatePosition(
                    hand.Transform.parent,
                    hand.BottomPosition,
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

        private class FreeFall : FallLandingAction
        {
            public FreeFall(PostureControlFall instance, Animator animator) : base(instance, animator)
            {
            }

            public override void Start()
            {
                // TODO 後で消す
                Debug.Log("Start FreeFall");
            }

            public override void Update()
            {
                if (IsOnGround) Finish();
            }
        }

        private class FallForwardLandOnHand : FallLandingAction
        {
            public FallForwardLandOnHand(PostureControlFall instance, Animator animator)
                : base(instance, animator)
            {
            }

            public override void Start()
            {
                // TODO 後で消す
                Debug.Log("Start FallForwardLandOnHand");
                SetGoal(Head, 1f);
            }

            public override void Update()
            {
                // Head か Head が目標地点の高さに到達していれば終了する
                if (IsOnBottom(Head) || (IsOnBottom(LeftHand) && IsOnBottom(RightHand)))
                {
                    Finish();
                    return;
                }

                // 首は曲げずに Head の位置を変えるために UpperChest を移動する
                UpdatePosition(
                    Head.Transform.parent.parent,
                    Head.BottomPosition,
                    BaseGoalPosition,
                    OriginPosition,
                    allowBodyMovement: true
                );

                UpdateHipsRotation();
                UpdateHands(BaseGoalPosition);
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
                // TODO 後で消す
                Debug.Log("Start FallBackwardLandOnHips");
                SetGoal(Hips, 0.5f);
            }

            public override void Update()
            {
                var hipsPosition = Hips.Transform.position;

                // Hips が目標地点の高さに到達したら終了する
                if (IsOnBottom(Hips))
                {
                    Finish();
                    return;
                }

                UpdatePosition(
                    Hips.Transform,
                    Hips.BottomPosition,
                    BaseGoalPosition,
                    OriginPosition,
                    allowBodyMovement: true
                );

                UpdateHands(BaseGoalPosition);

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
                var footPosition = foot.Transform.position;

                // XZ 平面における Hips から Foot への方向を求める
                var v = footPosition - hipsPosition;
                v.y = 0;
                var toFootDirection = v.normalized;

                // Foot の目標地点は Hips を基点とし、Centroid と反対方向に離れていく
                var toCentroid = (centroidPosition - hipsPosition).ProjectOnto(toFootDirection);
                var distance = UpperLegLength + LowerLegLength;
                var footGoalPosition = hipsPosition + distance * toFootDirection - toCentroid;
                footGoalPosition.y = BottomPosition.y;

                // Foot は動かさずに Foot の位置を変えるために UpperLeg を移動する
                UpdatePosition(
                    foot.Transform.parent,
                    foot.BottomPosition,
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
                // TODO 後で消す
                Debug.Log("Start FallAndLandOnFoot");
                // 接地していない Foot のうち低い方の Foot を Base Foot とする
                var isRightFootLowerThanLeftFoot = RightFoot.Transform.position.y < LeftFoot.Transform.position.y;

                if (isRightFootLowerThanLeftFoot)
                {
                    if (!IsOnBottom(RightFoot)) _baseFoot = RightFoot;
                    if (!IsOnBottom(LeftFoot)) _otherFoot = LeftFoot;
                }
                else
                {
                    if (!IsOnBottom(LeftFoot)) _baseFoot = LeftFoot;
                    if (!IsOnBottom(RightFoot)) _otherFoot = RightFoot;
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
                if (IsOnBottom(_baseFoot))
                {
                    Finish();
                    return;
                }

                UpdatePosition(
                    _baseFoot.Transform.parent,
                    _baseFoot.BottomPosition,
                    BaseGoalPosition,
                    OriginPosition,
                    allowBodyMovement: false
                );

                if (_otherFoot is not null)
                {
                    UpdatePosition(
                        _otherFoot.Transform.parent,
                        _otherFoot.BottomPosition,
                        BaseGoalPosition,
                        OriginPosition,
                        allowBodyMovement: false
                    );
                }

                UpdateHipsRotation();

                UpdateHands(_baseFoot.BottomPosition.y < Head.BottomPosition.y
                    ? BaseGoalPosition
                    : Head.BottomPosition
                );

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
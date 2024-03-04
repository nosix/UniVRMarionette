using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRMarionette.ForceTask;

namespace VRMarionette
{
    public class ForceResponder : MonoBehaviour, IForceTaskExecutor
    {
        [Tooltip("When moving the hips," +
                 " if the position of the head is used as the pivot point, then it is 1;" +
                 " if the position of the hips is used as the pivot point, then it is 0.")]
        public float balancePoint = 0.5f;

        [Space]
        public bool trace;

        public bool verbose;
        public bool filterZero;

        public BoneProperties BoneProperties => _bones.BoneProperties;

        private HumanoidManipulator _manipulator;
        private HumanoidBones _bones;
        private float _hipsToHeadLength;

        private readonly ForceTaskManager _forceTaskManager = new();

        public void Initialize(ForceFieldContainer forceFields)
        {
            if (forceFields is null)
            {
                throw new InvalidOperationException(
                    "The ForceResponder requires the ForceFieldContainer object.");
            }

            _manipulator = GetComponent<HumanoidManipulator>() ?? throw new InvalidOperationException(
                "The ForceResponder requires the HumanoidManipulator component.");

            var animator = GetComponent<Animator>() ?? throw new InvalidOperationException(
                "The ForceResponder requires the Animator component.");

            var hipsBone = animator.GetBoneTransform(HumanBodyBones.Hips) ?? throw new InvalidOperationException(
                "The Animator has not hips bone.");
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head) ?? throw new InvalidOperationException(
                "The Animator has not head bone.");

            _hipsToHeadLength = (headBone.position - hipsBone.position).magnitude;

            gameObject.GetOrAddComponent<Rigidbody>().isKinematic = true;

            _bones = new HumanoidBones(animator, _manipulator, forceFields, hipsBone);
        }

        /// <summary>
        /// 押す力を積む (Updateで積まれた力が処理される)
        /// </summary>
        /// <param name="target">力を働かせる対象の身体部位</param>
        /// <param name="forcePoint">力が発生する位置</param>
        /// <param name="force">力の大きさ(移動量)</param>
        /// <param name="allowBodyMovement">回転で消費されなかった力を移動に使うなら true</param>
        public ForceEvent? QueueForce(
            Transform target,
            Vector3 forcePoint,
            Vector3 force,
            bool allowBodyMovement
        )
        {
            if (!BoneProperties.TryGetValue(target, out var boneProperty)) return null;
            if (trace)
            {
                Debug.Log("QueueForce " +
                          $"{Time.frameCount} " +
                          $"{boneProperty.Bone} " +
                          $"{(100 * forcePoint).ToString().RemoveSpace()} " +
                          $"{(100 * force).ToString().RemoveSpace()} " +
                          "null " +
                          "True " +
                          $"{allowBodyMovement}");
            }

            _forceTaskManager.Enqueue(new SingleForceTask(
                boneProperty,
                forcePoint,
                force,
                rotation: null,
                isPushing: true,
                allowBodyMovement
            ));
            return boneProperty.CreateForceEvent(hold: false, forcePoint);
        }

        /// <summary>
        /// 力を積む (Updateで積まれた力が処理される)
        /// 複数の発生源と連動する場合には、複数の力による身体部位の軸回転が行われる
        /// </summary>
        /// <param name="target">力を働かせる対象の身体部位</param>
        /// <param name="forcePoint">力が発生する位置</param>
        /// <param name="force">力の大きさ(移動量)</param>
        /// <param name="rotation">回転量</param>
        /// <param name="isPushing">押す力なら true</param>
        /// <param name="allowBodyMovement">回転で消費されなかった力を移動に使うなら true</param>
        public ForceEvent? QueueForce(
            Transform target,
            Vector3 forcePoint,
            Vector3 force,
            Quaternion rotation,
            bool isPushing,
            bool allowBodyMovement
        )
        {
            if (!BoneProperties.TryGetValue(target, out var boneProperty)) return null;
            if (trace)
            {
                Debug.Log("QueueForce " +
                          $"{Time.frameCount} " +
                          $"{boneProperty.Bone} " +
                          $"{(100 * forcePoint).ToString().RemoveSpace()} " +
                          $"{(100 * force).ToString().RemoveSpace()} " +
                          $"{rotation.eulerAngles.ToString().RemoveSpace()} " +
                          $"{isPushing} " +
                          $"{allowBodyMovement}");
            }

            _forceTaskManager.Enqueue(new SingleForceTask(
                boneProperty,
                forcePoint,
                force,
                rotation,
                isPushing,
                allowBodyMovement
            ));
            return boneProperty.CreateForceEvent(hold: true, forcePoint);
        }

        private void Update()
        {
            _forceTaskManager.Execute(this);
        }

        public SingleForceTask ExecuteTask(IForceTask task)
        {
            switch (task)
            {
                case SingleForceTask singleForceTask:
                    return ExecuteSingleForceTask(singleForceTask);
                case MultiForceTask multiForceTask:
                    return ExecuteMultiForceTask(multiForceTask);
                default:
                    throw new InvalidOperationException();
            }
        }

        private SingleForceTask ExecuteSingleForceTask(SingleForceTask task)
        {
            if (verbose && (!filterZero || !task.IsZero()))
            {
                Debug.Log($"ExecuteSingleForceTask: {task.Target.Bone}\n" + task.ToLogString());
            }

            var context = CreateContext(task, _bones);
            var force = task.Force;

            if (context.Target.Bone == HumanBodyBones.Hips)
            {
                AdjustHipsRotation(context.Target.Transform);
                ApplyForceToHipsBone(context, force);
                AdjustHipsRotation(context.Target.Transform);
                return null;
            }

            if (task.Rotation.HasValue)
            {
                if (context.CanRotate())
                {
                    ApplyRotationToBone(context, task.Rotation.Value);
                }
                else if (context.CanAxisRotate())
                {
                    ApplyAxisRotationToBone(context, task.Rotation.Value);
                }
            }

            if (context.Target.Bone is
                HumanBodyBones.LeftLowerArm or
                HumanBodyBones.RightLowerArm or
                HumanBodyBones.LeftLowerLeg or
                HumanBodyBones.RightLowerLeg)
            {
                force = ApplyForceToLowerBone(context, force);
            }

            force = ApplyForceToBone(context, force);

            // Pull の場合は他の骨の移動をフィードバックして位置調整するために目標位置を保存する
            if (!context.IsPushing)
            {
                var goalPosition = task.ForcePoint + task.Force;
                var localSourcePosition = context.Target.Transform.InverseTransformPoint(context.SourcePosition);
                _forceTaskManager.Enqueue(new FeedbackTask(
                    context.Target,
                    goalPosition,
                    localSourcePosition
                ));
            }

            var nextTarget = context.Bones.Next(context.TargetGroup);
            if (nextTarget is not null)
                return new SingleForceTask(
                    nextTarget,
                    context.SourcePosition,
                    force,
                    rotation: null,
                    isPushing: true,
                    task.AllowBodyMovement
                );

            if (task.AllowBodyMovement) transform.position += force;

            return null;
        }

        private SingleForceTask ExecuteMultiForceTask(MultiForceTask task)
        {
            var context = CreateContext(task, _bones);

            if (task.Target.Bone == HumanBodyBones.Hips)
            {
                ApplyRotationToBone(context, task.Rotation);
                var force = context.CalculateForce();
                transform.position += force;
                ApplyAxisRotationToBone(context, context.CalculateRotation(force));
            }
            else
            {
                ApplyAxisRotationToBone(context, context.CalculateRotation(Vector3.zero));
                var force = context.CalculateForce();

                // TODO forcePoint の位置を見直す?
                var isAxisAligned = context.Target.IsAxisAligned ?? throw new InvalidOperationException();
                var sourcePosition = context.Target.Transform.TransformPoint(
                    (isAxisAligned ? 1 : -1) * context.Target.Length * context.Target.AxisDirection.ToAxis());
                var singleForceContext = new SingleForceContext(
                    context.Target,
                    sourcePosition,
                    isPushing: true,
                    context.Bones
                );
                ApplyForceToBone(singleForceContext, force);
            }

            return null;
        }

        private void AdjustHipsRotation(Transform hipsTransform)
        {
            var spineAngles = _manipulator.GetBoneRotation(HumanBodyBones.Spine);
            var leftLegAngles = _manipulator.GetBoneRotation(HumanBodyBones.LeftUpperLeg);
            var rightLegAngles = _manipulator.GetBoneRotation(HumanBodyBones.RightUpperLeg);
            var legAngles = (leftLegAngles + rightLegAngles) / 2f;
            var deltaAngles = (spineAngles + legAngles) / 2f;

            deltaAngles.y = 0;

            if (Mathf.Approximately(deltaAngles.magnitude, 0f)) return;

            _manipulator.Rotate(HumanBodyBones.Spine, -deltaAngles);
            _manipulator.Rotate(HumanBodyBones.LeftUpperLeg, -deltaAngles);
            _manipulator.Rotate(HumanBodyBones.RightUpperLeg, -deltaAngles);

            var hipsRotation = hipsTransform.localRotation;
            var xAxis = hipsRotation * Vector3.right;
            hipsRotation = Quaternion.AngleAxis(deltaAngles.x, xAxis) * hipsRotation;
            var zAxis = hipsRotation * Vector3.forward;
            hipsRotation = Quaternion.AngleAxis(deltaAngles.z, zAxis) * hipsRotation;
            hipsTransform.localRotation = hipsRotation;

            if (verbose)
            {
                Debug.Log("AdjustHipsRotation\n" +
                          $"deltaAngles: {deltaAngles}\n" +
                          $"legAngles: {legAngles}\n" +
                          $"rightLegAngles: {rightLegAngles}\n" +
                          $"leftLegAngles: {leftLegAngles}\n" +
                          $"spineAngles: {spineAngles}\n"
                );
            }
        }

        private void ApplyForceToHipsBone(SingleForceContext context, Vector3 force)
        {
            var sourcePosition = context.Target.Transform.TransformPoint(context.LocalSourcePosition);
            var hipsPosition = context.Target.Transform.position;
            var hipsAxis = context.Target.Transform.up;
            var hipsProperty = BoneProperties.Get(context.Target.Transform);

            // sourcePosition を Hips の軸上に投影する
            var sourceOnAxis = (sourcePosition - hipsPosition).ProjectOnto(hipsAxis);

            // 軸と直交する力の成分のみが身体を曲げる働きをする
            var forceToAxis = force.OrthogonalComponent(hipsAxis);

            // child へ力を加える点までの長さ
            var length = balancePoint * _hipsToHeadLength;

            // Hips の半径
            var r = hipsProperty.Collider.radius;

            var spineProperty = BoneProperties.Get(HumanBodyBones.Spine);
            var spinePosition = spineProperty.Transform.position;

            // Hips と Spine の軸が一致していれば 1、直交していれば 0、逆方向であれば -1 となる係数
            var spineAxis = spineProperty.Transform.up;
            var directionFactor = Vector3.Dot(spineAxis, hipsAxis);

            // spinePosition と sourcePosition が最も近い時に 0 (回転しない)、最も遠い時に 2 (2 倍回転する)
            var sign = Vector3.Dot(sourceOnAxis, hipsAxis) < 0 ? -1 : 1;
            var distanceOnAxis = r + directionFactor * sign * sourceOnAxis.magnitude;
            var ratio = Mathf.Clamp(distanceOnAxis / r, 0f, 2f);

            if (verbose && (!filterZero || !Mathf.Approximately(forceToAxis.magnitude, 0f)))
            {
                Debug.Log(
                    $"ApplyForceToHipsBone\n" +
                    $"ratio={ratio}\n" +
                    $"distanceOnAxis={distanceOnAxis}\n" +
                    $"directionFactor={directionFactor}\n" +
                    $"spinePosition={spinePosition}\n" +
                    $"hipsPosition={hipsPosition}\n" +
                    $"sourcePosition={sourcePosition}\n" +
                    $"sourceOnAxis={sourceOnAxis}\n" +
                    $"forceToAxis={forceToAxis}\n" +
                    $"r={r}\n" +
                    $"length={length}\n"
                );
            }

            // Spine もしくは Legs を回転する
            if (ratio >= 1f)
            {
                // 加える力は Hips が動く方向とは逆方向
                var forceToChild = -ratio * forceToAxis;

                var deltaAngles = ApplyForceToHipsChildBone(spineProperty, length, forceToChild);

                // Y 軸回転を無視して正規化する
                deltaAngles.y = 0f;
                deltaAngles /= ratio;
                ratio = 2f - ratio;

                if (!Mathf.Approximately(ratio, 0f))
                {
                    // Legs を回転させるとき X,Z 軸回転は反対方向になる
                    var angles = -ratio * deltaAngles;

                    _manipulator.Rotate(HumanBodyBones.LeftUpperLeg, angles);
                    _manipulator.Rotate(HumanBodyBones.RightUpperLeg, angles);
                }
            }
            else
            {
                ratio = 2f - ratio;

                // 加える力は Hips が動く方向とは逆方向
                var forceToChild = -ratio * forceToAxis;

                var leftLegProperty = BoneProperties.Get(HumanBodyBones.LeftUpperLeg);
                var rightLegProperty = BoneProperties.Get(HumanBodyBones.RightUpperLeg);
                var leftDeltaAngles = ApplyForceToHipsChildBone(leftLegProperty, -length, forceToChild);
                var rightDeltaAngles = ApplyForceToHipsChildBone(rightLegProperty, -length, forceToChild);
                var deltaAngles = (leftDeltaAngles + rightDeltaAngles) / 2f;

                // Y 軸回転を無視して正規化する
                deltaAngles.y = 0f;
                deltaAngles /= ratio;
                ratio = 2f - ratio;

                if (!Mathf.Approximately(ratio, 0f))
                {
                    // Spine を回転させるとき X,Z 軸回転は反対方向になる
                    var angles = -ratio * deltaAngles;

                    _manipulator.Rotate(HumanBodyBones.Spine, angles);
                }
            }

            transform.position += force;
        }

        private Vector3 ApplyForceToHipsChildBone(BoneProperty boneProperty, float length, Vector3 force)
        {
            // 力を加える点は position から hipsToHeadLength 離れた位置
            var boneTransform = boneProperty.Transform;
            var forceSourcePosition = boneTransform.position + length * boneTransform.up;

            var context = new SingleForceContext(
                boneProperty,
                forceSourcePosition,
                isPushing: true,
                _bones
            );

            var prevAngles = _manipulator.GetBoneRotation(boneProperty.Bone);
            ApplyForceToBone(context, force);
            var currAngles = _manipulator.GetBoneRotation(boneProperty.Bone);
            return currAngles - prevAngles;
        }

        /// <summary>
        /// Upper と Lower で連動する骨に対して力を適用する。
        /// </summary>
        /// <param name="context">処理対象の状況</param>
        /// <param name="force">力を表すベクトル(大きさは移動量、座標系はワールド)</param>
        /// <returns>処理しきれなかった力を表すベクトル(大きさは移動量、座標系はワールド)</returns>
        private Vector3 ApplyForceToLowerBone(SingleForceContext context, Vector3 force)
        {
            // 連動して動かす方向を求める
            // LowerBone の親と子を直線で結んだ方向が動かす方向になる
            var movePath = context.ChildToParent;

            // 力の発生源が動かす方向の先にある場合には動かさずに終了する
            var sourceToParentOnMovePath = context.SourceToParent.ProjectOnto(movePath);
            if (Vector3.Dot(movePath, sourceToParentOnMovePath) < 0) return force;

            // 動かす方向における力(移動量)を求める
            var forceOnPath = force.ProjectOnto(movePath);

            // 力の発生源が離れている程に力(移動量)を増減させる
            // 近付く力は弱くし、遠ざかる力は強くする
            var deltaOnMovePath = sourceToParentOnMovePath - movePath;
            if (Vector3.Dot(movePath, deltaOnMovePath) > 0)
            {
                forceOnPath -= deltaOnMovePath;
            }

            // movePath と同一方向ならば正、逆方向ならば負、量は forceOnPath の長さ
            var moveAmount = Vector3.Dot(forceOnPath, movePath) / movePath.magnitude;

            // Parent, Target, Child の 3 点の間の距離を求め、距離に基づいて角度を求める
            // Child-Parent 間に点 P を設定して、2 つの直角三角形 Parent-Target-P と Child-Target-P を考える

            var l = movePath.magnitude - moveAmount;
            var pl = movePath.magnitude;
            var hl1 = context.ChildToTarget.magnitude;
            var hl2 = context.TargetToParent.magnitude;

            // 移動後の配置において求める
            if (!CalculateBaseLengthsForTriangles(
                    l, // Child-Target 間の長さ
                    hl1, // Target-Parent 間の長さ
                    hl2, // Child-Parent 間の長さ
                    out var l1, // Child-P 間の長さ
                    out var l2 // P-Parent 間の長さ
                )) return force;

            // 移動前の配置において求める
            if (!CalculateBaseLengthsForTriangles(
                    pl, // Child-Target 間の長さ
                    hl1, // Target-Parent 間の長さ
                    hl2, // Child-Parent 間の長さ
                    out var pl1, // Child-P 間の長さ
                    out var pl2 // P-Parent 間の長さ
                )) return force;

            // 直角三角形の 2 辺の長さに基づいて、その間の角度を求める
            var theta1 = Mathf.Acos(l1 / hl1);
            var theta2 = Mathf.Acos(l2 / hl2);
            var prevTheta1 = Mathf.Acos(pl1 / hl1);
            var prevTheta2 = Mathf.Acos(pl2 / hl2);

            // 回転角度を求める
            var deltaAngle1 = (theta1 - prevTheta1) * Mathf.Rad2Deg;
            var deltaAngle2 = (theta2 - prevTheta2) * Mathf.Rad2Deg;
            deltaAngle1 += deltaAngle2;

            if (verbose && (!filterZero || !Mathf.Approximately(force.magnitude, 0f)))
            {
                Debug.Log(
                    $"ApplyForceToLowerBone: {context.Target.Bone}\n" +
                    $"lowerDeltaAngle: {deltaAngle1} = {theta1 * Mathf.Rad2Deg} - {prevTheta1 * Mathf.Rad2Deg}\n" +
                    $"upperDeltaAngle: {deltaAngle2} = {theta2 * Mathf.Rad2Deg} - {prevTheta2 * Mathf.Rad2Deg}\n" +
                    $"totalLength: {l} = {l1} + {l2}\n" +
                    $"prevTotalLength: {pl} = {pl1} + {pl2}\n" +
                    $"lowerHypotenuse: {hl1}\n" +
                    $"upperHypotenuse: {hl2}\n" +
                    $"moveAmount: {moveAmount}\n" +
                    $"forceOnPath: {forceOnPath}\n" +
                    $"movePath: {movePath}\n" +
                    $"force: {force}\n"
                );
            }

            if (Mathf.Approximately(deltaAngle1, 0f) || Mathf.Approximately(deltaAngle2, 0f)) return force;

            var parentTransform = context.Target.Transform.parent;

            var lowerBone = context.Target.Bone;
            var upperBone = BoneProperties.Get(parentTransform).Bone;

            var lowerRotationAngles = lowerBone switch
            {
                HumanBodyBones.LeftLowerLeg => new Vector3(deltaAngle1, 0, 0),
                HumanBodyBones.RightLowerLeg => new Vector3(deltaAngle1, 0, 0),
                HumanBodyBones.LeftLowerArm => new Vector3(0, deltaAngle1, 0),
                HumanBodyBones.RightLowerArm => new Vector3(0, -deltaAngle1, 0),
                _ => throw new InvalidOperationException($"This process cannot be performed on bone {lowerBone}.")
            };

            // Lower Bone の回転の反対方向の回転を Upper Bone の回転とする
            // Upper Bone の骨の軸回りの回転を考慮する
            var axis = context.Target.AxisDirection.ToAxis();
            var localLowerRotation =
                (deltaAngle2 / deltaAngle1 * lowerRotationAngles).ToRotationWithAxis(context.Target.AxisDirection);
            var upperAxisRotation = Vector3.Scale(
                parentTransform.localRotation.ToEulerAnglesWithAxis(context.Target.AxisDirection),
                axis
            );
            var localUpperRotation =
                upperAxisRotation.ToRotationWithAxis(context.Target.AxisDirection) *
                Quaternion.Inverse(localLowerRotation);
            var upperRotationAngles =　Vector3.Scale(
                localUpperRotation.ToEulerAnglesWithAxis(context.Target.AxisDirection),
                Vector3.one - axis
            );

            // 回転する
            var prevChildPosition = context.ChildPosition;
            var actualLowerRotationAngles = _manipulator.Rotate(lowerBone, lowerRotationAngles);
            var actualUpperRotationAngles = _manipulator.Rotate(upperBone, upperRotationAngles);
            var actualMove = context.ChildPosition - prevChildPosition;

            if (verbose && (!filterZero || !Mathf.Approximately(force.magnitude, 0f)))
            {
                Debug.Log(
                    $"ApplyForceToLowerBone: {context.Target.Bone}\n" +
                    $"actualMove: {actualMove}\n" +
                    $"actualLowerRotationAngles: {actualLowerRotationAngles}\n" +
                    $"actualUpperRotationAngles: {actualUpperRotationAngles}\n" +
                    $"lowerRotationAngles: {lowerRotationAngles}\n" +
                    $"upperRotationAngles: {upperRotationAngles}\n" +
                    $"force: {force}\n"
                );
            }

            return force - actualMove;
        }

        /// <summary>
        /// 高さが同じである 2 つの直角三角形の底辺の長さを計算する
        /// </summary>
        /// <param name="l">底辺の合計</param>
        /// <param name="hl1">斜辺1</param>
        /// <param name="hl2">斜辺2</param>
        /// <param name="l1">底辺1</param>
        /// <param name="l2">底辺2</param>
        /// <returns>解が得られた場合は true</returns>
        private static bool CalculateBaseLengthsForTriangles(float l, float hl1, float hl2, out float l1, out float l2)
        {
            l1 = 0f;
            l2 = 0f;

            // 2 つの直角三角形の高さが同じであることから、ピタゴラスの定理を使って
            //   hl1^2 - l1^2 = hl2^2 - l2^2
            // l = l1 + l2 なので
            //   l1 = l/2 + (hl1^2 - hl2^2) / 2l
            //   l2 = l/2 - (hl1^2 - hl2^2) / 2l
            // a = hl1^2 - hl2^2 としたとき l1 > 0, l2 > 0 でなければならないので
            //   l + a/l > 0
            //   l - a/l > 0
            // となる必要があり、a が正の場合は l > a/l、負の場合は l > -a/l でなければならない
            var a = Mathf.Pow(hl1, 2) - Mathf.Pow(hl2, 2);

            if (l <= Mathf.Abs(a) / l) return false;

            var b = l / 2f;
            var c = a / (2f * l);
            l1 = b + c;
            l2 = b - c;

            return hl1 > l1 && hl2 > l2;
        }

        /// <summary>
        /// １つの骨に対して力を適用する。
        /// </summary>
        /// <param name="context">処理対象の状況</param>
        /// <param name="force">力を表すベクトル(大きさは移動量、座標系はワールド)</param>
        /// <returns>処理しきれなかった力を表すベクトル(大きさは移動量、座標系はワールド)</returns>
        private Vector3 ApplyForceToBone(SingleForceContext context, Vector3 force)
        {
            var axis = context.Target.AxisDirection.ToAxis();

            // 骨の軸回りの回転が存在すると骨の軸回りの回転により力の方向が変わってしまう
            // 骨の軸回りの回転の分だけ力の向きを元に戻す
            var localAxisRotation = Vector3.Scale(
                context.Target.Transform.localRotation.ToEulerAnglesWithAxis(context.Target.AxisDirection),
                axis
            ).ToRotationWithAxis(context.Target.AxisDirection);
            var expectedLocalMove = localAxisRotation * context.ToLocalDirection(force);

            // 骨の軸と同一方向の力で回転すると動きが不自然になるので行わない
            var localMove = expectedLocalMove - Vector3.Scale(expectedLocalMove, axis);

            if (Mathf.Approximately(localMove.magnitude, 0f)) return force;

            // 各軸回りの回転を求める
            var radius = BoneProperties.TryGetValue(context.Target.Transform, out var boneProperty)
                ? boneProperty.Collider.radius
                : 0f;

            var v1 = context.LocalSourcePosition;
            var v2 = v1 + localMove;

            bool doAxisRotation;
            var tiltAngle = Vector3.zero;
            var rotationAngle = Vector3.zero;
            switch (context.Target.AxisDirection)
            {
                case Direction.XAxis:
                    // TODO x,y,z が 0 の場合を考慮する
                    tiltAngle.y = v2.AngleYForXAxis() - v1.AngleYForXAxis();
                    tiltAngle.z = v2.AngleZForXAxis() - v1.AngleZForXAxis();
                    doAxisRotation = v1.MagnitudeForXAxis() > radius && v2.MagnitudeForXAxis() > radius;
                    if (doAxisRotation) rotationAngle.x = v2.AngleXForXAxis() - v1.AngleXForXAxis();
                    if (Mathf.Abs(rotationAngle.x) > 90f) rotationAngle.x = 0f;
                    break;
                case Direction.YAxis:
                    // TODO x,y,z が 0 の場合を考慮する
                    tiltAngle.z = v2.AngleZForYAxis() - v1.AngleZForYAxis();
                    tiltAngle.x = v2.AngleXForYAxis() - v1.AngleXForYAxis();
                    doAxisRotation = v1.MagnitudeForYAxis() > radius && v2.MagnitudeForYAxis() > radius;
                    if (doAxisRotation) rotationAngle.y = v2.AngleYForYAxis() - v1.AngleYForYAxis();
                    if (Mathf.Abs(rotationAngle.y) > 90f) rotationAngle.y = 0f;
                    break;
                case Direction.ZAxis:
                    // TODO x,y,z が 0 の場合を考慮する
                    tiltAngle.x = v2.AngleXForZAxis() - v1.AngleXForZAxis();
                    tiltAngle.y = v2.AngleYForZAxis() - v1.AngleYForZAxis();
                    doAxisRotation = v1.MagnitudeForZAxis() > radius && v2.MagnitudeForZAxis() > radius;
                    if (doAxisRotation) rotationAngle.z = v2.AngleZForZAxis() - v1.AngleZForZAxis();
                    if (Mathf.Abs(rotationAngle.z) > 90f) rotationAngle.z = 0f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var ratio = context.SourceDistancePerLength;
            var scale = context.Target.Transform.lossyScale;
            tiltAngle = new Vector3(
                Utils.NormalizeTo180(tiltAngle.x) * ratio / scale.x,
                Utils.NormalizeTo180(tiltAngle.y) * ratio / scale.y,
                Utils.NormalizeTo180(tiltAngle.z) * ratio / scale.z
            );

            // 軸を傾ける
            var sourcePosition = context.ToWorldDirection(context.LocalSourcePosition);
            var actualRotationAngle = _manipulator.Rotate(context.Target.Bone, tiltAngle);
            var rotatedSourcePosition = context.ToWorldDirection(context.LocalSourcePosition);
            var actualMove = rotatedSourcePosition - sourcePosition;

            // 軸を回転する
            var remaining = Mathf.Max(1f - actualMove.magnitude / localMove.magnitude, 0f);
            rotationAngle = new Vector3(
                remaining / scale.x * Utils.NormalizeTo180(rotationAngle.x),
                remaining / scale.y * Utils.NormalizeTo180(rotationAngle.y),
                remaining / scale.z * Utils.NormalizeTo180(rotationAngle.z)
            );
            actualRotationAngle += _manipulator.Rotate(context.Target.Bone, rotationAngle);
            rotatedSourcePosition = context.ToWorldDirection(context.LocalSourcePosition);
            actualMove = rotatedSourcePosition - sourcePosition;

            if (verbose && (!filterZero || !Mathf.Approximately(expectedLocalMove.magnitude, 0f)))
            {
                Debug.Log(
                    $"ApplyForceToBone: {context.Target.Bone}\n" +
                    $"move: {force} -> {actualMove}\n" +
                    $"sourcePosition: {sourcePosition} -> {rotatedSourcePosition}\n" +
                    $"rotationAngle: {tiltAngle + rotationAngle} -> {actualRotationAngle}\n" +
                    $"tiltAngle: {tiltAngle}\n" +
                    $"axisRotationAngle: {rotationAngle}\n" +
                    $"localSourcePosition: {v1} -> {v2}\n" +
                    $"localMove: {localMove}\n" +
                    $"axis: {axis}\n" +
                    $"expectedLocalMove: {expectedLocalMove}\n"
                );
            }

            var remainingForce = force - actualMove;
            return context.IsPushing
                ? remainingForce.ProjectOnto(force)
                : remainingForce;
        }

        private void ApplyAxisRotationToBone(ForceContext context, Quaternion rotation)
        {
            if (rotation.eulerAngles.magnitude < 0.01) return;
            var axisDirection = context.Target.AxisDirection;
            var currLocalRotation = context.Target.Transform.localRotation;
            var nextLocalRotation = context.GetLocalRotation(rotation);
            var deltaRotation = Quaternion.Inverse(currLocalRotation) * nextLocalRotation;
            deltaRotation = deltaRotation.ExtractAxisRotation(axisDirection);
            nextLocalRotation = currLocalRotation * deltaRotation;
            var localEulerAngles = nextLocalRotation.ToEulerAnglesWithAxis(context.Target.AxisDirection);
            var rotatedAngles = _manipulator.SetBoneRotation(context.Target.Bone, localEulerAngles, isBoneAngle: true);

            if (verbose)
            {
                Debug.Log(
                    $"ApplyAxisRotationToBone: {context.Target.Bone}\n" +
                    $"rotatedAngles: {rotatedAngles}\n" +
                    $"localEulerAngles: {localEulerAngles}\n" +
                    $"localRotation: {currLocalRotation.eulerAngles} + {deltaRotation.eulerAngles} -> {nextLocalRotation.eulerAngles}\n"
                );
            }
        }

        private void ApplyAxisRotationToBone(MultiForceContext context, Quaternion rotation)
        {
            if (Quaternion.Angle(Quaternion.identity, rotation) < 0.1f) return;
            var axisDirection = context.Target.AxisDirection;
            // rotation は World 座標系における回転量なので、Target の座標系における回転量を求める
            var targetRotation = context.Target.Transform.rotation;
            var deltaRotation = Quaternion.Inverse(targetRotation) * rotation * targetRotation;
            // 軸まわりの回転のみを抽出する
            var deltaAngles = Vector3.Scale(
                deltaRotation.ToEulerAnglesWithAxis(axisDirection),
                axisDirection.ToAxis()
            );
            // 回転後の姿勢を求めて関節を回転する
            var currLocalAngles = _manipulator.GetBoneRotation(context.Target.Bone);
            var nextLocalAngles = currLocalAngles + deltaAngles;
            var rotatedAngles = _manipulator.SetBoneRotation(context.Target.Bone, nextLocalAngles, isBoneAngle: false);
            // TODO isBoneAngle: true は未使用なコードなので削除する

            if (verbose)
            {
                Debug.Log(
                    $"ApplyAxisRotationToBone: {context.Target.Bone}\n" +
                    $"rotatedAngles: {rotatedAngles}\n" +
                    $"localAngles: {currLocalAngles} + {deltaAngles} -> {nextLocalAngles}\n" +
                    $"deltaRotation: {deltaRotation.eulerAngles}\n" +
                    $"targetRotation: {targetRotation.eulerAngles}\n"
                );
            }
        }

        private void ApplyRotationToBone(ForceContext context, Quaternion rotation)
        {
            if (rotation.eulerAngles.magnitude < 0.01) return;
            var localRotation = context.GetLocalRotation(rotation);
            var localEulerAngles = localRotation.ToEulerAnglesWithAxis(context.Target.AxisDirection);
            var rotatedAngles = _manipulator.SetBoneRotation(context.Target.Bone, localEulerAngles);

            if (verbose)
            {
                Debug.Log(
                    $"ApplyRotationToBone: {context.Target.Bone}\n" +
                    $"rotatedAngles: {rotatedAngles}\n" +
                    $"localEulerAngles: {localEulerAngles}\n"
                );
            }
        }

        private static SingleForceContext CreateContext(SingleForceTask task, HumanoidBones bones)
        {
            return new SingleForceContext(
                task.Target,
                task.ForcePoint,
                task.IsPushing,
                bones
            );
        }

        private static MultiForceContext CreateContext(MultiForceTask task, HumanoidBones bones)
        {
            return new MultiForceContext(
                task.Target,
                task.Tasks.Select(t => t.ForcePoint),
                task.Tasks.Select(t => t.ForcePoint + t.Force),
                bones
            );
        }

        private class ForceContext
        {
            /// <summary>
            /// 回転対象の骨
            /// 骨が連結している場合は衝突が発生した末端の骨
            /// </summary>
            public BoneProperty Target { get; }

            public BoneGroupProperty TargetGroup { get; }

            public HumanoidBones Bones { get; }

            /// <summary>
            /// Bone の親の位置 (World 座標)
            /// </summary>
            protected Vector3 ParentPosition => Target.Transform.parent.position;

            /// <summary>
            /// Bone の子の位置 (World 座標、子が 1 つの場合にのみ使用する)
            /// </summary>
            public Vector3 ChildPosition => Target.Transform.GetChild(0).position;

            /// <summary>
            /// TargetTransform の子から親へのベクトル (World 座標系)
            /// </summary>
            public Vector3 ChildToParent => ParentPosition - ChildPosition;

            /// <summary>
            /// TargetTransform の子から TargetTransform へのベクトル (World 座標系)
            /// </summary>
            public Vector3 ChildToTarget => Target.Transform.position - ChildPosition;

            /// <summary>
            /// TargetTransform から TargetTransform の親へのベクトル (World 座標系)
            /// </summary>
            public Vector3 TargetToParent => ParentPosition - Target.Transform.position;

            protected ForceContext(BoneProperty target, HumanoidBones bones)
            {
                // BUG UpperChest の場合に Collider なしの Context が作られてしまう
                Target = target;
                TargetGroup = bones.GetBoneGroupProperty(target.Bone);
                Bones = bones;
            }

            public Vector3 ToLocalDirection(Vector3 worldDirection)
            {
                return Target.Transform.InverseTransformDirection(worldDirection);
            }

            public Vector3 ToWorldDirection(Vector3 localDirection)
            {
                return Target.Transform.TransformDirection(localDirection);
            }

            public Quaternion GetLocalRotation(Quaternion rotation)
            {
                return Quaternion.Inverse(Target.Transform.parent.rotation) * rotation * Target.Transform.rotation;
            }

            public bool CanRotate()
            {
                return Target.Bone is
                    HumanBodyBones.LeftFoot or
                    HumanBodyBones.RightFoot or
                    HumanBodyBones.LeftHand or
                    HumanBodyBones.RightHand;
            }

            public bool CanAxisRotate()
            {
                return Target.Bone is
                    HumanBodyBones.LeftUpperArm or
                    HumanBodyBones.LeftUpperLeg or
                    HumanBodyBones.RightUpperArm or
                    HumanBodyBones.RightUpperLeg;
            }
        }

        private class SingleForceContext : ForceContext
        {
            /// <summary>
            /// OriginTransform を原点とした時の SourceCollider の中心座標
            /// 回転の中心を原点とした場合の力点の位置
            /// </summary>
            public Vector3 LocalSourcePosition { get; }

            public bool IsPushing { get; }

            /// <summary>
            /// Source の位置から TargetTransform の親へのベクトル (World 座標系)
            /// </summary>
            public Vector3 SourceToParent => ParentPosition - Target.Transform.TransformPoint(LocalSourcePosition);

            /// <summary>
            /// LocalSourcePosition に対応したワールド座標
            /// </summary>
            public Vector3 SourcePosition => BoneProperty.ToWorldPosition(
                Target.Transform,
                TargetGroup.OriginTransform,
                LocalSourcePosition
            );

            public float SourceDistancePerLength
            {
                get
                {
                    var isAxisAligned = Target.IsAxisAligned ?? throw new InvalidOperationException();
                    var localSourcePositionOnAxis = Target.GetPositionOnAxis(LocalSourcePosition);
                    var distanceFromOrigin = (isAxisAligned ? 1 : -1) * localSourcePositionOnAxis;
                    return Mathf.Clamp01(distanceFromOrigin / TargetGroup.Length);
                }
            }

            public SingleForceContext(
                BoneProperty target,
                Vector3 sourcePosition,
                bool isPushing,
                HumanoidBones bones
            ) : base(target, bones)
            {
                // localSourcePosition は OriginTransform を原点とする位置
                // 骨が連結している場合は連結した骨を一直線の骨と見立てた時の位置
                LocalSourcePosition = TargetGroup.ToLocalPosition(sourcePosition);
                IsPushing = isPushing;
            }
        }

        private class MultiForceContext : ForceContext
        {
            /// <summary>
            /// Context 生成時の TargetTransform.position を原点とした移動前の座標
            /// TargetTransform.rotation で逆回転している
            /// </summary>
            private readonly IReadOnlyList<Vector3> _localSourcePositions;

            /// <summary>
            /// Context 生成時の TargetTransform.position を原点とした移動後の座標
            /// TargetTransform.rotation を考慮しない
            /// </summary>
            private readonly IReadOnlyList<Vector3> _localNextSourcePositions;

            public MultiForceContext(
                BoneProperty target,
                IEnumerable<Vector3> forcePoints,
                IEnumerable<Vector3> nextForcePoints,
                HumanoidBones bones
            ) : base(target, bones)
            {
                var localSourcePositions =
                    forcePoints.Select(p => Target.Transform.InverseTransformPoint(p));
                _localSourcePositions = localSourcePositions.ToList();

                var localNextSourcePositions =
                    nextForcePoints.Select(p => p - Target.Transform.position);
                _localNextSourcePositions = localNextSourcePositions.ToList();
            }

            public Vector3 CalculateForce()
            {
                Vector3 force;

                // TargetTransform は移動後の rotation になっている前提とする
                var rotation = Target.Transform.rotation;

                if (_localSourcePositions.Count == 2)
                {
                    var a = rotation * _localSourcePositions[0];
                    var b = rotation * _localSourcePositions[1];
                    var aDash = _localNextSourcePositions[0];
                    var bDash = _localNextSourcePositions[1];
                    var deltaA = aDash - a;
                    var deltaB = bDash - b;

                    // a : b = a' - p : b' - p となる点 p を求める
                    // a * (b' - p) = b * (a' - p)
                    // p = (a * b' - b * a') / (a - b)
                    var numerator = Vector3.Scale(a, bDash) - Vector3.Scale(b, aDash);
                    var denominator = a - b;

                    // a,b が原点を挟まない場合は比率で求めず移動量の平均を使う
                    var delta = (deltaA + deltaB) / 2f;

                    var p = new Vector3(
                        a.x * b.x >= 0f ? delta.x : numerator.x / denominator.x,
                        a.y * b.y >= 0f ? delta.y : numerator.y / denominator.y,
                        a.z * b.z >= 0f ? delta.z : numerator.z / denominator.z
                    );

                    force = p;
                }
                else
                {
                    // 移動前と移動後の点の rotation を揃えた上で移動量を求め、移動量の平均を求める
                    var startPositions = _localSourcePositions.Select(p => rotation * p);
                    var delta = startPositions.Zip(_localNextSourcePositions, (s, e) => e - s).ToArray();
                    var total = delta.Aggregate(Vector3.zero, (r, d) => r + d);
                    force = total / delta.Length;
                }

                return force;
            }

            public Quaternion CalculateRotation(Vector3 movement)
            {
                var rotationAvg = Quaternion.identity;

                // TargetTransform は移動後の rotation と position になっている前提とする
                var rotation = Target.Transform.rotation;

                for (var i = 0; i < _localSourcePositions.Count; i++)
                {
                    var startPosition = rotation * _localSourcePositions[i];
                    var endPosition = _localNextSourcePositions[i];
                    var delta = endPosition - startPosition - movement;
                    var r = Quaternion.FromToRotation(startPosition, startPosition + delta);
                    rotationAvg = Quaternion.Lerp(rotationAvg, r, 1f / (i + 1));
                }

                return rotationAvg;
            }
        }
    }
}
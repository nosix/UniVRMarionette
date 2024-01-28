using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    public class ForceResponder : MonoBehaviour
    {
        [Space]
        public bool verbose;

        public bool filterZero;

        public BoneProperties BoneProperties { get; private set; }

        private HumanoidManipulator _manipulator;
        private Transform _rootTransform;

        private readonly Queue<ForceGenerationTask> _forceGenerationQueue = new();

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

            _rootTransform = hipsBone;

            gameObject.GetOrAddComponent<Rigidbody>().isKinematic = true;

            var bonePropertiesBuilder = new BoneProperties.Builder();

            CreateColliderForEachBones(
                animator,
                _manipulator.HumanLimits,
                forceFields.forceFields.ToDictionary(e => e.bone, e => e),
                bonePropertiesBuilder
            );

            BoneProperties = bonePropertiesBuilder.Build();
        }

        private static void CreateColliderForEachBones(
            Animator animator,
            HumanLimits humanLimits,
            IReadOnlyDictionary<HumanBodyBones, ForceField> forceFields,
            BoneProperties.Builder bonePropertiesBuilder
        )
        {
            for (HumanBodyBones bone = 0; bone < HumanBodyBones.LastBone; bone++)
            {
                var boneTransform = animator.GetBoneTransform(bone);
                if (boneTransform is null) continue;

                if (humanLimits.TryGetValue(bone, out var humanLimit) &&
                    forceFields.TryGetValue(bone, out var forceField))
                {
                    if (bone == HumanBodyBones.Head)
                    {
                        bonePropertiesBuilder.Add(
                            bone,
                            boneTransform,
                            humanLimit,
                            CreateHeadCapsule(forceField, boneTransform)
                        );
                    }
                    else
                    {
                        switch (boneTransform.childCount)
                        {
                            case 1:
                                bonePropertiesBuilder.Add(
                                    bone,
                                    boneTransform,
                                    humanLimit,
                                    CreateCapsuleForSingleChildBone(
                                        forceField,
                                        headBoneTransform: boneTransform,
                                        tailBoneTransform: boneTransform.GetChild(0)
                                    )
                                );
                                break;
                            case > 1:
                                bonePropertiesBuilder.Add(
                                    bone,
                                    boneTransform,
                                    humanLimit,
                                    CreateCapsuleForMultiChildrenBone(
                                        forceField,
                                        headBoneTransform: boneTransform,
                                        tailBoneTransforms: boneTransform.GetChildren()
                                    )
                                );
                                break;
                        }
                    }
                }
                else
                {
                    bonePropertiesBuilder.Add(
                        bone,
                        boneTransform,
                        humanLimits.TryGetValue(bone, out humanLimit) ? humanLimit : null,
                        null
                    );
                }
            }
        }

        private static CapsuleCollider CreateCapsuleForSingleChildBone(
            ForceField forceField,
            Transform headBoneTransform,
            Transform tailBoneTransform
        )
        {
            var headPosition = headBoneTransform.position;
            var tailPosition = tailBoneTransform.position;
            var localTailPosition = tailPosition - headPosition;
            var height = forceField.direction switch
            {
                Direction.XAxis => Mathf.Abs(localTailPosition.x),
                Direction.YAxis => Mathf.Abs(localTailPosition.y),
                Direction.ZAxis => Mathf.Abs(localTailPosition.z),
                _ => throw new ArgumentOutOfRangeException()
            };
            return AddCapsuleCollider(
                headBoneTransform.gameObject,
                (tailPosition - headPosition) / 2f + forceField.centerOffset,
                height,
                forceField.radius,
                forceField.direction
            );
        }

        private static CapsuleCollider CreateCapsuleForMultiChildrenBone(
            ForceField forceField,
            Transform headBoneTransform,
            IEnumerable<Transform> tailBoneTransforms
        )
        {
            var localPositions = tailBoneTransforms
                .Select(tailBoneTransform => tailBoneTransform.position - headBoneTransform.position)
                .ToList();
            var minX = localPositions.Min(p => p.x);
            var maxX = localPositions.Max(p => p.x);
            var minY = localPositions.Min(p => p.y);
            var maxY = localPositions.Max(p => p.y);
            var minZ = localPositions.Min(p => p.z);
            var maxZ = localPositions.Max(p => p.z);
            var center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
            var height = forceField.direction switch
            {
                Direction.XAxis => maxX - minX,
                Direction.YAxis => maxY - minY,
                Direction.ZAxis => maxZ - minZ,
                _ => throw new ArgumentOutOfRangeException()
            };
            return AddCapsuleCollider(
                headBoneTransform.gameObject,
                center + forceField.centerOffset,
                height,
                forceField.radius,
                forceField.direction
            );
        }

        private static CapsuleCollider CreateHeadCapsule(ForceField forceField, Transform boneTransform)
        {
            var tailLocalPosition = forceField.centerOffset * 2f;
            var height = forceField.direction switch
            {
                Direction.XAxis => Mathf.Abs(tailLocalPosition.x),
                Direction.YAxis => Mathf.Abs(tailLocalPosition.y),
                Direction.ZAxis => Mathf.Abs(tailLocalPosition.z),
                _ => throw new ArgumentOutOfRangeException()
            };
            return AddCapsuleCollider(
                boneTransform.gameObject,
                forceField.centerOffset,
                height,
                forceField.radius,
                forceField.direction
            );
        }

        private static CapsuleCollider AddCapsuleCollider(
            GameObject target,
            Vector3 center,
            float height,
            float radius,
            Direction direction
        )
        {
            var capsule = target.AddComponent<CapsuleCollider>();
            capsule.center = center;
            capsule.height = height;
            capsule.radius = radius;
            capsule.direction = (int)direction;
            return capsule;
        }

        /// <summary>
        /// 押す力を積む (Updateで積まれた力が処理される)
        /// </summary>
        /// <param name="target">力を働かせる対象の身体部位</param>
        /// <param name="forcePoint">力が発生する位置</param>
        /// <param name="force">力の大きさ(移動量)</param>
        /// <param name="allowBodyMovement">回転で消費されなかった力を移動に使うなら true</param>
        public void QueueForce(
            Transform target,
            Vector3 forcePoint,
            Vector3 force,
            bool allowBodyMovement
        )
        {
            if (!BoneProperties.TryGetValue(target, out var boneProperty)) return;
            var context = new Context(forcePoint, boneProperty, _rootTransform);
            _forceGenerationQueue.Enqueue(new ForceGenerationTask(
                context,
                force,
                rotation: null,
                allowMultiSource: false,
                allowBodyMovement
            ));
        }

        /// <summary>
        /// 力を積む (Updateで積まれた力が処理される)
        /// 複数の発生源と連動する場合には、複数の力による身体部位の軸回転が行われる
        /// </summary>
        /// <param name="target">力を働かせる対象の身体部位</param>
        /// <param name="forcePoint">力が発生する位置</param>
        /// <param name="force">力の大きさ(移動量)</param>
        /// <param name="rotation">回転量</param>
        /// <param name="allowMultiSource">複数の発生源と連動することを許可するなら true</param>
        /// <param name="allowBodyMovement">回転で消費されなかった力を移動に使うなら true</param>
        public void QueueForce(
            Transform target,
            Vector3 forcePoint,
            Vector3 force,
            Quaternion rotation,
            bool allowMultiSource,
            bool allowBodyMovement
        )
        {
            if (!BoneProperties.TryGetValue(target, out var boneProperty)) return;
            var context = new Context(forcePoint, boneProperty, _rootTransform);
            _forceGenerationQueue.Enqueue(new ForceGenerationTask(
                context,
                force,
                rotation,
                allowMultiSource,
                allowBodyMovement
            ));
        }

        public void Update()
        {
            switch (_forceGenerationQueue.Count)
            {
                case 0:
                    return;
                case 1:
                    ExecuteTask(_forceGenerationQueue.Dequeue());
                    return;
            }

            // 同じ骨を対象とするタスクは併合する
            var tasks = new Dictionary<HumanBodyBones, ForceGenerationTask>();

            while (_forceGenerationQueue.TryDequeue(out var queuedTask))
            {
                var bone = queuedTask.Context.Bone;
                if (tasks.TryGetValue(bone, out var task))
                {
                    task.Merge(queuedTask);
                }
                else
                {
                    tasks.Add(bone, queuedTask);
                }
            }

            foreach (var task in tasks.Values)
            {
                ExecuteTask(task);
            }
        }

        private void ExecuteTask(ForceGenerationTask task)
        {
            var context = task.Context;

            if (verbose && (!filterZero || !task.IsZero()))
            {
                Debug.Log($"ExecuteTask: {context.Bone}\n" + task.ToLogString());
            }

            var force = task.Force;

            if (task.IsMultiSource)
            {
                if (context.Bone == HumanBodyBones.Spine)
                {
                    context = new Context(context, BoneProperties.Get(_rootTransform));
                }

                if (context.Bone == HumanBodyBones.Hips && task.Rotation.HasValue)
                {
                    ApplyRotationToBone(context, task.Rotation.Value);
                }

                if (task.HasAxisRotation)
                {
                    ApplyAxisRotationToBone(context, task.AxisRotationAngle, context.TargetAxisDirection);
                }

                if (context.Bone == HumanBodyBones.Hips)
                {
                    transform.position += force;
                }

                return;
            }

            if (context.Bone == HumanBodyBones.Hips)
            {
                ApplyForceToHipsBone(context, force);
                return;
            }

            if (task.Rotation.HasValue)
            {
                if (context.CanRotate())
                {
                    ApplyRotationToBone(context, task.Rotation.Value);
                }
                else if (context.CanAxisRotate())
                {
                    ApplyAxisRotationToBone(context, task.Rotation.Value, context.TargetAxisDirection);
                }
            }

            while (context != null && !Mathf.Approximately(force.magnitude, 0f))
            {
                if (context.Bone is
                    HumanBodyBones.LeftLowerArm or
                    HumanBodyBones.RightLowerArm or
                    HumanBodyBones.LeftLowerLeg or
                    HumanBodyBones.RightLowerLeg)
                {
                    force = ApplyForceToLowerBone(context, force);
                }

                force = ApplyForceToBone(context, force);
                context = context.Next(BoneProperties);
            }

            if (verbose && (!filterZero || !Mathf.Approximately(force.magnitude, 0f)))
            {
                Debug.Log($"RemainingForce: {force}");
            }

            if (task.AllowBodyMovement) transform.position += force;
        }

        private void ApplyForceToHipsBone(Context context, Vector3 force)
        {
            var sourcePosition = context.TargetTransform.TransformPoint(context.SourceLocalPosition);
            var targetPosition = context.TargetTransform.position;

            foreach (var childTransform in context.TargetTransform.GetChildren())
            {
                if (!BoneProperties.TryGetValue(childTransform, out var boneProperty)) continue;
                var childPosition = childTransform.position;
                var ratio = (sourcePosition - childPosition).magnitude / (targetPosition - childPosition).magnitude;
                var forceSourcePosition = Vector3.Lerp(childPosition, childTransform.GetChild(0).position, ratio);
                var childContext = new Context(forceSourcePosition, boneProperty, _rootTransform);
                ApplyForceToBone(childContext, -force);
            }

            transform.position += force;
        }

        /// <summary>
        /// Upper と Lower で連動する骨に対して力を適用する。
        /// </summary>
        /// <param name="context">処理対象の状況</param>
        /// <param name="force">力を表すベクトル(大きさは移動量、座標系はワールド)</param>
        /// <returns>処理しきれなかった力を表すベクトル(大きさは移動量、座標系はワールド)</returns>
        private Vector3 ApplyForceToLowerBone(Context context, Vector3 force)
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
                    $"ApplyForceToLowerBone: {context.Bone}\n" +
                    $"lowerDeltaAngle: {deltaAngle1} = {theta1 * Mathf.Rad2Deg} - {prevTheta1 * Mathf.Rad2Deg}\n" +
                    $"upperDeltaAngle: {deltaAngle2} = {theta2 * Mathf.Rad2Deg} - {prevTheta2 * Mathf.Rad2Deg}\n" +
                    $"totalLength: {l} = {l1} + {l2}\n" +
                    $"prevTotalLength: {pl} = {pl1} + {pl2}\n" +
                    $"lowerHypotenuse: {hl1}\n" +
                    $"upperHypotenuse: {hl2}\n" +
                    $"force: {force}\n"
                );
            }

            var lowerBone = context.Bone;
            var lowerRotationAngles = lowerBone switch
            {
                HumanBodyBones.LeftLowerLeg => new Vector3(deltaAngle1, 0, 0),
                HumanBodyBones.RightLowerLeg => new Vector3(deltaAngle1, 0, 0),
                HumanBodyBones.LeftLowerArm => new Vector3(0, deltaAngle1, 0),
                HumanBodyBones.RightLowerArm => new Vector3(0, -deltaAngle1, 0),
                _ => throw new InvalidOperationException($"This process cannot be performed on bone {lowerBone}.")
            };

            var upperBone = BoneProperties.Get(context.TargetTransform.parent).Bone;
            var upperRotationAngles = upperBone switch
            {
                HumanBodyBones.LeftUpperLeg => new Vector3(-deltaAngle2, 0, 0),
                HumanBodyBones.RightUpperLeg => new Vector3(-deltaAngle2, 0, 0),
                HumanBodyBones.LeftUpperArm => new Vector3(0, -deltaAngle2, 0),
                HumanBodyBones.RightUpperArm => new Vector3(0, deltaAngle2, 0),
                _ => throw new InvalidOperationException($"This process cannot be performed on bone {upperBone}.")
            };

            var prevChildPosition = context.ChildPosition;
            var actualLowerRotationAngles = _manipulator.Rotate(lowerBone, lowerRotationAngles);
            var actualUpperRotationAngles = _manipulator.Rotate(upperBone, upperRotationAngles);
            var actualMove = context.ChildPosition - prevChildPosition;

            if (verbose && (!filterZero || !Mathf.Approximately(force.magnitude, 0f)))
            {
                Debug.Log(
                    $"ApplyForceToLowerBone: {context.Bone}\n" +
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
        private Vector3 ApplyForceToBone(Context context, Vector3 force)
        {
            var axis = context.TargetAxisDirection.ToAxis();

            // 骨の軸回りの回転が存在すると骨の軸回りの回転により力の方向が変わってしまう
            // 骨の軸回りの回転の分だけ力の向きを元に戻す
            var localAxisRotation = Vector3.Scale(
                context.TargetTransform.localRotation.ToEulerAnglesWithAxis(context.TargetAxisDirection),
                axis
            ).ToRotationWithAxis(context.TargetAxisDirection);
            var expectedLocalMove = localAxisRotation * context.ToLocalDirection(force);

            // 骨の軸と同一方向の力で回転すると動きが不自然になるので行わない
            var localMove = expectedLocalMove - Vector3.Scale(expectedLocalMove, axis);

            if (Mathf.Approximately(localMove.magnitude, 0f)) return force;

            // 各軸回りの回転を求める
            var radius = BoneProperties.TryGetValue(context.TargetTransform, out var boneProperty)
                ? boneProperty.Collider.radius
                : 0f;

            var v1 = context.SourceLocalPosition;
            var v2 = v1 + localMove;

            bool doAxisRotation;
            var tiltAngle = Vector3.zero;
            var rotationAngle = Vector3.zero;
            switch (context.TargetAxisDirection)
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

            // 軸を傾ける
            var sourcePosition = context.ToWorldDirection(context.SourceLocalPosition);
            var actualRotationAngle = _manipulator.Rotate(context.Bone, tiltAngle);
            var rotatedSourcePosition = context.ToWorldDirection(context.SourceLocalPosition);
            var actualMove = rotatedSourcePosition - sourcePosition;

            // 軸を回転する
            var remaining = Mathf.Max(1f - actualMove.magnitude / localMove.magnitude, 0f);
            rotationAngle *= remaining;
            actualRotationAngle += _manipulator.Rotate(context.Bone, rotationAngle);
            rotatedSourcePosition = context.ToWorldDirection(context.SourceLocalPosition);
            actualMove = rotatedSourcePosition - sourcePosition;

            if (verbose && (!filterZero || !Mathf.Approximately(expectedLocalMove.magnitude, 0f)))
            {
                Debug.Log(
                    $"ApplyForceToBone: {context.Bone}\n" +
                    $"move: {force} -> {actualMove}\n" +
                    $"sourcePosition: {sourcePosition} -> {rotatedSourcePosition}\n" +
                    $"rotationAngle: {tiltAngle + rotationAngle} -> {actualRotationAngle}\n" +
                    $"localSourcePosition: {v1} -> {v2}\n" +
                    $"localMove: {localMove}\n" +
                    $"axis: {axis}\n" +
                    $"expectedLocalMove: {expectedLocalMove}\n"
                );
            }

            // 力と同一方向のみを残さないと思わぬ方向に移動してしまう
            return (force - actualMove).ProjectOnto(force);
        }

        private void ApplyAxisRotationToBone(Context context, float rotationAngle, Direction axisDirection)
        {
            if (Mathf.Abs(rotationAngle) < 0.01) return;
            var eulerAngles = axisDirection.ToAxis() * rotationAngle;
            var actualRotationAngle = _manipulator.Rotate(context.Bone, eulerAngles);

            if (verbose)
            {
                Debug.Log(
                    $"ApplyAxisRotationToBone: {context.Bone}\n" +
                    $"actualRotationAngle: {actualRotationAngle.magnitude}\n" +
                    $"expectedRotationAngle: {rotationAngle}\n" +
                    $"axisDirection: {axisDirection}\n"
                );
            }
        }

        private void ApplyAxisRotationToBone(Context context, Quaternion rotation, Direction axisDirection)
        {
            if (rotation.eulerAngles.magnitude < 0.01) return;
            var currLocalRotation = context.TargetTransform.localRotation;
            var nextLocalRotation = context.GetLocalRotation(rotation);
            var deltaRotation = Quaternion.Inverse(currLocalRotation) * nextLocalRotation;
            deltaRotation = deltaRotation.ExtractAxisRotation(axisDirection);
            nextLocalRotation = currLocalRotation * deltaRotation;
            var localEulerAngles = nextLocalRotation.ToEulerAnglesWithAxis(context.TargetAxisDirection);
            var rotatedAngles = _manipulator.SetBoneRotation(context.Bone, localEulerAngles);

            if (verbose)
            {
                Debug.Log(
                    $"ApplyAxisRotationToBone: {context.Bone}\n" +
                    $"rotatedAngles: {rotatedAngles}\n" +
                    $"localEulerAngles: {localEulerAngles}\n" +
                    $"localRotation: {currLocalRotation.eulerAngles} + {deltaRotation.eulerAngles} -> {nextLocalRotation.eulerAngles}\n"
                );
            }
        }

        private void ApplyRotationToBone(Context context, Quaternion rotation)
        {
            if (rotation.eulerAngles.magnitude < 0.01) return;
            var localRotation = context.GetLocalRotation(rotation);
            var localEulerAngles = localRotation.ToEulerAnglesWithAxis(context.TargetAxisDirection);
            var rotatedAngles = _manipulator.SetBoneRotation(context.Bone, localEulerAngles);

            if (verbose)
            {
                Debug.Log(
                    $"ApplyRotationToBone: {context.Bone}\n" +
                    $"rotatedAngles: {rotatedAngles}\n" +
                    $"localEulerAngles: {localEulerAngles}\n"
                );
            }
        }

        private class Context
        {
            /// <summary>
            /// 回転対象の骨
            /// </summary>
            public HumanBodyBones Bone { get; }

            /// <summary>
            /// OriginTransform を原点とした時の SourceCollider の中心座標
            /// 回転の中心を原点とした場合の力点の位置
            /// </summary>
            public Vector3 SourceLocalPosition { get; }

            /// <summary>
            /// TargetTransform を基準とした時の骨の軸方向
            /// 力を受ける骨の軸の方向
            /// </summary>
            public Direction TargetAxisDirection { get; }

            /// <summary>
            /// TargetCollider の Transform
            /// 複数の骨が連結して回転する場合には、衝突が発生した骨や連結の末端の骨の関節を示す
            /// 力を受ける骨の関節
            /// </summary>
            public Transform TargetTransform { get; }

            /// <summary>
            /// 回転の中心となる Transform
            /// 複数の骨が連結して回転する場合には、根元の骨の関節を示す
            /// 回転の中心になる骨の関節
            /// </summary>
            private Transform OriginTransform { get; }

            /// <summary>
            /// 骨の Root (Hips) の Transform
            /// </summary>
            private Transform RootTransform { get; }

            /// <summary>
            /// TargetTransform の親の位置 (World 座標)
            /// </summary>
            private Vector3 ParentPosition => TargetTransform.parent.position;

            /// <summary>
            /// TargetTransform の子の位置 (World 座標、子が 1 つの場合にのみ使用する)
            /// </summary>
            public Vector3 ChildPosition => TargetTransform.GetChild(0).position;

            /// <summary>
            /// TargetTransform の子から親へのベクトル (World 座標系)
            /// </summary>
            public Vector3 ChildToParent => ParentPosition - ChildPosition;

            /// <summary>
            /// TargetTransform の子から TargetTransform へのベクトル (World 座標系)
            /// </summary>
            public Vector3 ChildToTarget => TargetTransform.position - ChildPosition;

            /// <summary>
            /// TargetTransform から TargetTransform の親へのベクトル (World 座標系)
            /// </summary>
            public Vector3 TargetToParent => ParentPosition - TargetTransform.position;

            /// <summary>
            /// Source の位置から TargetTransform の親へのベクトル (World 座標系)
            /// </summary>
            public Vector3 SourceToParent => ParentPosition - TargetTransform.TransformPoint(SourceLocalPosition);

            /// <summary>
            /// SourceLocalPosition に対応したワールド座標
            /// </summary>
            private Vector3 SourcePosition => BoneProperty.ToWorldPosition(
                TargetTransform,
                OriginTransform,
                SourceLocalPosition
            );

            public Context(Vector3 sourcePosition, BoneProperty target, Transform rootTransform)
            {
                Bone = target.Bone;

                TargetTransform = target.Collider.transform;
                RootTransform = rootTransform;
                TargetAxisDirection = target.Limit.axis;

                var sourceLocalPosition =
                    TargetTransform.InverseTransformPoint(sourcePosition)
                    - target.Collider.center.OrthogonalComponent(TargetAxisDirection.ToAxis());
                OriginTransform = target.FindOrigin(TargetTransform, ref sourceLocalPosition);
                SourceLocalPosition = sourceLocalPosition;
            }

            public Context(Context source, BoneProperty target)
            {
                Bone = target.Bone;

                TargetTransform = target.Collider.transform;
                RootTransform = source.RootTransform;
                TargetAxisDirection = target.Limit.axis;

                var sourceLocalPosition = TargetTransform.InverseTransformPoint(source.SourcePosition);
                OriginTransform = target.FindOrigin(TargetTransform, ref sourceLocalPosition);
                SourceLocalPosition = sourceLocalPosition;
            }

            private Context(Context source, Transform target)
            {
                Bone = HumanBodyBones.LastBone;

                TargetTransform = target;
                RootTransform = source.RootTransform;
                TargetAxisDirection = Direction.YAxis;
                OriginTransform = TargetTransform;
                SourceLocalPosition = TargetTransform.InverseTransformPoint(source.SourcePosition);
            }

            public Context(Context context1, Context context2, float context1Ratio)
            {
                if (context1.Bone != context2.Bone)
                    throw new ArgumentException("Bones must be identical to be merged.");
                if (context1.TargetTransform != context2.TargetTransform) throw new ArgumentException();
                if (context1.RootTransform != context2.RootTransform) throw new ArgumentException();
                if (context1.TargetAxisDirection != context2.TargetAxisDirection) throw new ArgumentException();

                Bone = context1.Bone;

                TargetTransform = context1.TargetTransform;
                RootTransform = context1.RootTransform;
                TargetAxisDirection = context1.TargetAxisDirection;
                OriginTransform = TargetTransform;
                SourceLocalPosition =
                    Vector3.Lerp(context1.SourceLocalPosition, context2.SourceLocalPosition, context1Ratio);
            }

            public Vector3 ToLocalDirection(Vector3 worldDirection)
            {
                return TargetTransform.InverseTransformDirection(worldDirection);
            }

            public Vector3 ToWorldDirection(Vector3 localDirection)
            {
                return TargetTransform.TransformDirection(localDirection);
            }

            public Quaternion GetLocalRotation(Quaternion rotation)
            {
                return Quaternion.Inverse(TargetTransform.parent.rotation) * rotation * TargetTransform.rotation;
            }

            public float ToRotationAngle(Vector3 force)
            {
                var axis = TargetAxisDirection.ToAxis();
                // 回転軸と直交するベクトルを求める
                var r = SourceLocalPosition.OrthogonalComponent(axis);
                // 回転方向を表すベクトルを求める
                var rotationDirection = Vector3.Cross(axis, r).normalized;

                // rotationDirection と大体同方向であれば正方向に回転
                // 大体逆方向であれば負方向に回転
                // (rotationDirection へ force を射影すると移動量が小さくなりすぎて回転しないので大体で対応する)
                var move = Vector3.Dot(ToLocalDirection(force), rotationDirection) switch
                {
                    > 0 => force.magnitude,
                    < 0 => -force.magnitude,
                    _ => 0
                };

                // 円周の移動量と半径から回転角度を求める
                return Mathf.Round(move / r.magnitude * Mathf.Rad2Deg);
            }

            public bool CanRotate()
            {
                return Bone is
                    HumanBodyBones.LeftFoot or
                    HumanBodyBones.RightFoot or
                    HumanBodyBones.LeftHand or
                    HumanBodyBones.RightHand;
            }

            public bool CanAxisRotate()
            {
                return Bone is
                    HumanBodyBones.LeftUpperArm or
                    HumanBodyBones.LeftUpperLeg or
                    HumanBodyBones.RightUpperArm or
                    HumanBodyBones.RightUpperLeg;
            }

            /// <summary>
            /// 親の骨を処理対象とした Context を得る
            /// </summary>
            /// <param name="properties">各 Bone の情報</param>
            /// <returns>Hips Bone に到達したら null を返す</returns>
            public Context Next(BoneProperties properties)
            {
                var currentContext = this;
                for (
                    var transform = TargetTransform.parent;
                    transform != RootTransform;
                    transform = transform.parent
                )
                {
                    var property = properties.Get(transform);
                    if (!property.HasCollider)
                    {
                        currentContext = new Context(currentContext, transform);
                        continue;
                    }

                    if (property.IsLinked(Bone)) continue;
                    return new Context(currentContext, property);
                }

                return null;
            }
        }

        private class ForceGenerationTask
        {
            public Context Context { private set; get; }
            public Vector3 Force { private set; get; }
            public Quaternion? Rotation { private set; get; }
            public float AxisRotationAngle { private set; get; }
            public bool IsMultiSource { private set; get; }
            public bool AllowBodyMovement { private set; get; }

            public bool HasAxisRotation => !Mathf.Approximately(AxisRotationAngle, 0f);

            private bool _allowMultiSource;
            private int _mergedTasks;

            public ForceGenerationTask(
                Context context,
                Vector3 force,
                Quaternion? rotation,
                bool allowMultiSource,
                bool allowBodyMovement
            )
            {
                Context = context;
                Force = force;
                Rotation = rotation;
                AxisRotationAngle = 0f;
                IsMultiSource = false;
                AllowBodyMovement = allowBodyMovement;
                _allowMultiSource = allowMultiSource;
                _mergedTasks = 1;
            }

            public void Merge(ForceGenerationTask other)
            {
                if (_allowMultiSource && other._allowMultiSource)
                {
                    var rotationAngle1 = Context.ToRotationAngle(Force);
                    var rotationAngle2 = other.Context.ToRotationAngle(other.Force);
                    Rotation = null;
                    AxisRotationAngle = (rotationAngle1 + rotationAngle2) / 2;
                    IsMultiSource = true;
                    AllowBodyMovement = false;
                    _allowMultiSource = false;
                }

                if (Rotation.HasValue && other.Rotation.HasValue)
                {
                    Rotation = Quaternion.Lerp(Rotation.Value, other.Rotation.Value, 0.5f);
                }
                else
                {
                    if (other.Rotation.HasValue) Rotation = other.Rotation;
                }

                var sourceRatio = _mergedTasks / (_mergedTasks + other._mergedTasks);
                Force = Force * sourceRatio + other.Force * (1f - sourceRatio);
                Context = new Context(Context, other.Context, sourceRatio);

                _mergedTasks += other._mergedTasks;
            }

            public bool IsZero()
            {
                return Mathf.Approximately(Force.magnitude, 0f) &&
                       Mathf.Approximately(Rotation.GetValueOrDefault(Quaternion.identity).eulerAngles.magnitude, 0f) &&
                       Mathf.Approximately(AxisRotationAngle, 0f);
            }

            public string ToLogString()
            {
                return $"force: {Force}\n" +
                       $"rotation: {Rotation.GetValueOrDefault(Quaternion.identity).eulerAngles}\n" +
                       $"axisRotationAngle: {AxisRotationAngle}\n";
            }
        }
    }
}
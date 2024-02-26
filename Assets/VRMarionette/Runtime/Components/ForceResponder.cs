using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRMarionette
{
    public class ForceResponder : MonoBehaviour
    {
        [Tooltip("When moving the hips," +
                 " if the position of the head is used as the pivot point, then it is 1;" +
                 " if the position of the hips is used as the pivot point, then it is 0.")]
        public float balancePoint = 0.5f;

        [Space]
        public bool verbose;

        public bool filterZero;

        public BoneProperties BoneProperties { get; private set; }

        private HumanoidManipulator _manipulator;
        private Transform _rootTransform;
        private float _hipsToHeadLength;

        private readonly Queue<SingleForceTask> _forceTaskQueue = new();

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

            _rootTransform = hipsBone;
            _hipsToHeadLength = (headBone.position - hipsBone.position).magnitude;

            gameObject.GetOrAddComponent<Rigidbody>().isKinematic = true;

            var bonePropertiesBuilder = new BoneProperties.Builder(_manipulator.BoneGroups);

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

                if (!humanLimits.TryGetValue(bone, out var humanLimit) ||
                    !forceFields.TryGetValue(bone, out var forceField) ||
                    bone == HumanBodyBones.UpperChest)
                {
                    // Property だけ登録して Collider は作らない
                    bonePropertiesBuilder.Add(
                        bone,
                        boneTransform,
                        humanLimits.TryGetValue(bone, out humanLimit) ? humanLimit : null,
                        null
                    );
                    continue;
                }

                // 頭は末端になるので特別に処理する
                if (bone == HumanBodyBones.Head)
                {
                    bonePropertiesBuilder.Add(
                        bone,
                        boneTransform,
                        humanLimit,
                        CreateHeadCapsule(forceField, boneTransform)
                    );
                    continue;
                }

                var children = boneTransform.GetChildren().ToList();

                // UpperChest が存在する場合は無視する(UpperChest がある場合は子が１つだけ)
                if (bone == HumanBodyBones.Chest && children.Count == 1)
                {
                    children = boneTransform.GetChild(0).GetChildren().ToList();
                }

                if (children.Count == 1)
                {
                    bonePropertiesBuilder.Add(
                        bone,
                        boneTransform,
                        humanLimit,
                        CreateCapsuleForSingleChildBone(
                            forceField,
                            headBoneTransform: boneTransform,
                            tailBoneTransform: children[0]
                        )
                    );
                }
                else
                {
                    bonePropertiesBuilder.Add(
                        bone,
                        boneTransform,
                        humanLimit,
                        CreateCapsuleForMultiChildrenBone(
                            forceField,
                            headBoneTransform: boneTransform,
                            tailBoneTransforms: children
                        )
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
            var localTailPosition = forceField.centerOffset * 2f;
            var height = forceField.direction switch
            {
                Direction.XAxis => Mathf.Abs(localTailPosition.x),
                Direction.YAxis => Mathf.Abs(localTailPosition.y),
                Direction.ZAxis => Mathf.Abs(localTailPosition.z),
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
        public ForceEvent? QueueForce(
            Transform target,
            Vector3 forcePoint,
            Vector3 force,
            bool allowBodyMovement
        )
        {
            if (!BoneProperties.TryGetValue(target, out var boneProperty)) return null;
            _forceTaskQueue.Enqueue(new SingleForceTask(
                boneProperty,
                forcePoint,
                force,
                rotation: null,
                allowMerge: false,
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
        /// <param name="allowMultiSource">複数の発生源と連動することを許可するなら true</param>
        /// <param name="allowBodyMovement">回転で消費されなかった力を移動に使うなら true</param>
        public ForceEvent? QueueForce(
            Transform target,
            Vector3 forcePoint,
            Vector3 force,
            Quaternion rotation,
            bool allowMultiSource,
            bool allowBodyMovement
        )
        {
            if (!BoneProperties.TryGetValue(target, out var boneProperty)) return null;
            _forceTaskQueue.Enqueue(new SingleForceTask(
                boneProperty,
                forcePoint,
                force,
                rotation,
                allowMultiSource,
                allowBodyMovement
            ));
            return boneProperty.CreateForceEvent(hold: true, forcePoint);
        }

        public void Update()
        {
            switch (_forceTaskQueue.Count)
            {
                case 0:
                    return;
                case 1:
                    ExecuteSingleForceTask(_forceTaskQueue.Dequeue());
                    return;
            }

            // 同じ骨を対象とするタスクは併合する
            var tasks = new Dictionary<HumanBodyBones, IForceTask>();

            while (_forceTaskQueue.TryDequeue(out var queuedTask))
            {
                if (!queuedTask.AllowMerge)
                {
                    ExecuteSingleForceTask(queuedTask);
                    continue;
                }

                var bone = queuedTask.Target.Bone;
                if (tasks.TryGetValue(bone, out var cachedTask))
                {
                    tasks[bone] = cachedTask.Merge(queuedTask);
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

        private void ExecuteTask(IForceTask task)
        {
            switch (task)
            {
                case SingleForceTask singleForceTask:
                    ExecuteSingleForceTask(singleForceTask);
                    break;
                case MultiForceTask multiForceTask:
                    ExecuteMultiForceTask(multiForceTask);
                    break;
            }
        }

        private void ExecuteSingleForceTask(SingleForceTask task)
        {
            if (verbose && (!filterZero || !task.IsZero()))
            {
                Debug.Log($"ExecuteSingleForceTask: {task.Target.Bone}\n" + task.ToLogString());
            }

            var context = task.CreateContext(BoneProperties, _rootTransform);
            var force = task.Force;

            if (context.Bone == HumanBodyBones.Hips)
            {
                AdjustHipsRotation(context.TargetTransform);
                ApplyForceToHipsBone(context, force);
                AdjustHipsRotation(context.TargetTransform);
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

        private void ExecuteMultiForceTask(MultiForceTask task)
        {
            var context = task.CreateContext(_rootTransform);

            if (task.Target.Bone == HumanBodyBones.Hips)
            {
                ApplyRotationToBone(context, task.Rotation);
                transform.position += context.CalculateForce();
            }

            ApplyAxisRotationToBone(context, context.CalculateRotationOffset(), context.TargetAxisDirection);
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
            var sourcePosition = context.TargetTransform.TransformPoint(context.LocalSourcePosition);
            var hipsPosition = context.TargetTransform.position;
            var hipsAxis = context.TargetTransform.up;
            var hipsProperty = BoneProperties.Get(context.TargetTransform);

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
                BoneProperties,
                _rootTransform
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
                    $"ApplyForceToLowerBone: {context.Bone}\n" +
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

            var parentTransform = context.TargetTransform.parent;

            var lowerBone = context.Bone;
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
            var axis = context.TargetAxisDirection.ToAxis();
            var localLowerRotation =
                (deltaAngle2 / deltaAngle1 * lowerRotationAngles).ToRotationWithAxis(context.TargetAxisDirection);
            var upperAxisRotation = Vector3.Scale(
                parentTransform.localRotation.ToEulerAnglesWithAxis(context.TargetAxisDirection),
                axis
            );
            var localUpperRotation =
                upperAxisRotation.ToRotationWithAxis(context.TargetAxisDirection) *
                Quaternion.Inverse(localLowerRotation);
            var upperRotationAngles =　Vector3.Scale(
                localUpperRotation.ToEulerAnglesWithAxis(context.TargetAxisDirection),
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
        private Vector3 ApplyForceToBone(SingleForceContext context, Vector3 force)
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

            var v1 = context.LocalSourcePosition;
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

            var scale = context.TargetTransform.lossyScale;
            tiltAngle = new Vector3(
                Utils.NormalizeTo180(tiltAngle.x) / scale.x,
                Utils.NormalizeTo180(tiltAngle.y) / scale.y,
                Utils.NormalizeTo180(tiltAngle.z) / scale.z
            );

            // 軸を傾ける
            var sourcePosition = context.ToWorldDirection(context.LocalSourcePosition);
            var actualRotationAngle = _manipulator.Rotate(context.Bone, tiltAngle);
            var rotatedSourcePosition = context.ToWorldDirection(context.LocalSourcePosition);
            var actualMove = rotatedSourcePosition - sourcePosition;

            // 軸を回転する
            var remaining = Mathf.Max(1f - actualMove.magnitude / localMove.magnitude, 0f);
            rotationAngle = new Vector3(
                remaining / scale.x * Utils.NormalizeTo180(rotationAngle.x),
                remaining / scale.y * Utils.NormalizeTo180(rotationAngle.y),
                remaining / scale.z * Utils.NormalizeTo180(rotationAngle.z)
            );
            actualRotationAngle += _manipulator.Rotate(context.Bone, rotationAngle);
            rotatedSourcePosition = context.ToWorldDirection(context.LocalSourcePosition);
            actualMove = rotatedSourcePosition - sourcePosition;

            if (verbose && (!filterZero || !Mathf.Approximately(expectedLocalMove.magnitude, 0f)))
            {
                Debug.Log(
                    $"ApplyForceToBone: {context.Bone}\n" +
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

            // 力と同一方向のみを残さないと思わぬ方向に移動してしまう
            return (force - actualMove).ProjectOnto(force);
        }

        private void ApplyAxisRotationToBone(ForceContext context, Quaternion rotation, Direction axisDirection)
        {
            if (rotation.eulerAngles.magnitude < 0.01) return;
            var currLocalRotation = context.TargetTransform.localRotation;
            var nextLocalRotation = context.GetLocalRotation(rotation);
            var deltaRotation = Quaternion.Inverse(currLocalRotation) * nextLocalRotation;
            deltaRotation = deltaRotation.ExtractAxisRotation(axisDirection);
            nextLocalRotation = currLocalRotation * deltaRotation;
            var localEulerAngles = nextLocalRotation.ToEulerAnglesWithAxis(context.TargetAxisDirection);
            var rotatedAngles = _manipulator.SetBoneRotation(context.Bone, localEulerAngles, isBoneAngle: true);

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

        private void ApplyRotationToBone(ForceContext context, Quaternion rotation)
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

        private class ForceContext
        {
            /// <summary>
            /// 回転対象の骨
            /// </summary>
            public HumanBodyBones Bone { get; }

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
            /// 骨の Root (Hips) の Transform
            /// </summary>
            protected Transform RootTransform { get; }

            /// <summary>
            /// TargetTransform の親の位置 (World 座標)
            /// </summary>
            protected Vector3 ParentPosition => TargetTransform.parent.position;

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

            protected ForceContext(BoneProperty target, Transform rootTransform)
            {
                Bone = target.Bone;

                TargetTransform = target.Transform;
                RootTransform = rootTransform;
                TargetAxisDirection = target.Limit.axis;
            }

            protected ForceContext(ForceContext source, BoneProperty target)
            {
                Bone = target.Bone;

                TargetTransform = target.Transform;
                RootTransform = source.RootTransform;
                TargetAxisDirection = target.Limit.axis;
            }

            protected ForceContext(ForceContext source, Transform target)
            {
                Bone = HumanBodyBones.LastBone;

                TargetTransform = target;
                RootTransform = source.RootTransform;
                TargetAxisDirection = Direction.YAxis;
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
        }

        private class SingleForceContext : ForceContext
        {
            /// <summary>
            /// OriginTransform を原点とした時の SourceCollider の中心座標
            /// 回転の中心を原点とした場合の力点の位置
            /// </summary>
            public Vector3 LocalSourcePosition { get; }

            /// <summary>
            /// 回転の中心となる Transform
            /// 複数の骨が連結して回転する場合には、根元の骨の関節を示す
            /// 回転の中心になる骨の関節
            /// </summary>
            private Transform OriginTransform { get; }

            /// <summary>
            /// Source の位置から TargetTransform の親へのベクトル (World 座標系)
            /// </summary>
            public Vector3 SourceToParent => ParentPosition - TargetTransform.TransformPoint(LocalSourcePosition);

            /// <summary>
            /// LocalSourcePosition に対応したワールド座標
            /// </summary>
            private Vector3 SourcePosition => BoneProperty.ToWorldPosition(
                TargetTransform,
                OriginTransform,
                LocalSourcePosition
            );

            public SingleForceContext(
                BoneProperty target,
                Vector3 sourcePosition,
                BoneProperties properties,
                Transform rootTransform
            ) : base(target, rootTransform)
            {
                var localSourcePosition =
                    TargetTransform.InverseTransformPoint(sourcePosition)
                    - target.Collider.center.OrthogonalComponent(TargetAxisDirection.ToAxis());
                OriginTransform = FindOrigin(TargetTransform, properties, ref localSourcePosition);
                LocalSourcePosition = localSourcePosition;
            }

            private SingleForceContext(
                SingleForceContext source,
                BoneProperty target,
                BoneProperties properties
            ) : base(source, target)
            {
                var localSourcePosition = TargetTransform.InverseTransformPoint(source.SourcePosition);
                OriginTransform = FindOrigin(TargetTransform, properties, ref localSourcePosition);
                LocalSourcePosition = localSourcePosition;
            }

            private SingleForceContext(
                SingleForceContext source,
                Transform target
            ) : base(source, target)
            {
                OriginTransform = TargetTransform;
                LocalSourcePosition = TargetTransform.InverseTransformPoint(source.SourcePosition);
            }

            /// <summary>
            /// 骨が連結して回転する場合の根元の骨の関節を探す。
            /// 根元の骨の関節が OriginTransform になり、
            /// 関節の位置を原点としたときの力の発生源の位置を SourceLocalPosition として保持する。
            /// </summary>
            /// <param name="targetTransform">連結した骨の末端側の関節</param>
            /// <param name="boneProperties">骨に関する情報</param>
            /// <param name="sourceLocalPosition">力の発生源の位置(末端側の関節基準の位置を受け取り、根元の関節基準の位置を返す)</param>
            /// <returns>根元の関節</returns>
            private static Transform FindOrigin(
                Transform targetTransform,
                BoneProperties boneProperties,
                ref Vector3 sourceLocalPosition
            )
            {
                var targetProperty = boneProperties.Get(targetTransform);
                if (!targetProperty.HasCollider || targetProperty.GroupSpec is null) return targetTransform;

                var originTransform = targetTransform;
                var parentTransform = originTransform.parent;
                while (true)
                {
                    var parentProperty = boneProperties.Get(parentTransform);
                    if (!targetProperty.GroupSpec.Contains(parentProperty.Bone)) break;
                    sourceLocalPosition += parentTransform.InverseTransformPoint(originTransform.position);
                    originTransform = parentTransform;
                    parentTransform = originTransform.parent;
                }

                return originTransform;
            }

            /// <summary>
            /// 親の骨を処理対象とした Context を得る
            /// </summary>
            /// <param name="properties">各 Bone の情報</param>
            /// <returns>Hips Bone に到達したら null を返す</returns>
            public SingleForceContext Next(BoneProperties properties)
            {
                var currentContext = this;
                for (
                    var transform = OriginTransform.parent;
                    transform != RootTransform;
                    transform = transform.parent
                )
                {
                    var property = properties.Get(transform);

                    // Collider があれば処理対象なので Context を生成する
                    if (property.HasCollider) return new SingleForceContext(currentContext, property, properties);

                    // Collider が無い場合は処理対象ではないので Context を更新しつつ継続する
                    currentContext = new SingleForceContext(currentContext, transform);
                }

                return null;
            }
        }

        private class MultiForceContext : ForceContext
        {
            private readonly Vector3 _originPosition;

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
                Transform rootTransform
            ) : base(target, rootTransform)
            {
                _originPosition = TargetTransform.position;

                var localSourcePositions =
                    forcePoints.Select(p => TargetTransform.InverseTransformPoint(p));
                _localSourcePositions = localSourcePositions.ToList();

                var localNextSourcePositions =
                    nextForcePoints.Select(p => p - TargetTransform.position);
                _localNextSourcePositions = localNextSourcePositions.ToList();
            }

            public Vector3 CalculateForce()
            {
                Vector3 force;

                // TargetTransform は移動後の rotation になっている前提とする
                var rotation = TargetTransform.rotation;

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

            public Quaternion CalculateRotationOffset()
            {
                var rotationOffset = Quaternion.identity;

                // TargetTransform は移動後の rotation と position になっている前提とする
                var rotation = TargetTransform.rotation;
                var deltaPosition = TargetTransform.position - _originPosition;

                for (var i = 0; i < _localSourcePositions.Count; i++)
                {
                    var startPosition = rotation * _localSourcePositions[i];
                    var endPosition = _localNextSourcePositions[i] - deltaPosition;
                    var r = Quaternion.FromToRotation(startPosition, endPosition);
                    rotationOffset = Quaternion.Lerp(rotationOffset, r, 1f / (i + 1));
                }

                return rotationOffset;
            }
        }

        private interface IForceTask
        {
            IForceTask Merge(SingleForceTask task);
        }

        private class SingleForceTask : IForceTask
        {
            public BoneProperty Target { private set; get; }
            public Vector3 ForcePoint { get; }
            public Vector3 Force { get; }
            public Quaternion? Rotation { get; }
            public bool AllowMerge { get; }
            public bool AllowBodyMovement { get; }

            public SingleForceTask(
                BoneProperty target,
                Vector3 forcePoint,
                Vector3 force,
                Quaternion? rotation,
                bool allowMerge,
                bool allowBodyMovement
            )
            {
                Target = target;
                ForcePoint = forcePoint;
                Force = force;
                Rotation = rotation;
                AllowMerge = allowMerge;
                AllowBodyMovement = allowBodyMovement;
            }

            public IForceTask Merge(SingleForceTask task)
            {
                IForceTask newTask = new MultiForceTask(Target);
                return newTask.Merge(this).Merge(task);
            }

            public SingleForceContext CreateContext(BoneProperties properties, Transform rootTransform)
            {
                return new SingleForceContext(Target, ForcePoint, properties, rootTransform);
            }

            public bool IsZero()
            {
                return Mathf.Approximately(Force.magnitude, 0f) &&
                       Mathf.Approximately(Rotation.GetValueOrDefault(Quaternion.identity).eulerAngles.magnitude, 0f);
            }

            public string ToLogString()
            {
                return $"force: {Force}\n" +
                       $"rotation: {Rotation.GetValueOrDefault(Quaternion.identity).eulerAngles}\n";
            }
        }

        private class MultiForceTask : IForceTask
        {
            public BoneProperty Target;

            private readonly List<SingleForceTask> _tasks = new();

            public Quaternion Rotation
            {
                get
                {
                    var count = 0;
                    var rotation = Quaternion.identity;

                    foreach (var task in _tasks)
                    {
                        if (!task.Rotation.HasValue) continue;

                        rotation = Quaternion.Lerp(rotation, task.Rotation.Value, 1f / ++count);
                    }

                    return rotation;
                }
            }

            public MultiForceTask(BoneProperty target)
            {
                Target = target;
            }

            public IForceTask Merge(SingleForceTask task)
            {
                _tasks.Add(task);
                return this;
            }

            public MultiForceContext CreateContext(Transform rootTransform)
            {
                return new MultiForceContext(
                    Target,
                    _tasks.Select(t => t.ForcePoint),
                    _tasks.Select(t => t.ForcePoint + t.Force),
                    rootTransform
                );
            }
        }
    }
}
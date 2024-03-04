using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRMarionette.ForceTask
{
    public class ForceTaskManager
    {
        private static readonly HumanBodyBones[] ExecutionOrder =
        {
            HumanBodyBones.Head,
            HumanBodyBones.Neck,
            HumanBodyBones.LeftHand,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.RightHand,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Chest,
            HumanBodyBones.Spine,
            HumanBodyBones.Hips
        };

        private readonly Queue<SingleForceTask> _forceTaskQueue = new();
        private readonly Queue<FeedbackTask> _feedbackTaskQueue = new();

        public void Enqueue(SingleForceTask task)
        {
            _forceTaskQueue.Enqueue(task);
        }

        public void Execute(IForceTaskExecutor executor)
        {
            // 同じ骨を対象とするタスクを併合する
            var tasks = new Dictionary<HumanBodyBones, IForceTask>();

            while (_forceTaskQueue.TryDequeue(out var queuedTask))
            {
                var bone = queuedTask.Target.Bone;
                tasks[bone] = tasks.TryGetValue(bone, out var cachedTask)
                    ? Merge(cachedTask, queuedTask)
                    : queuedTask;
            }

            // 末端(手足頭)から順にタスクを実行する
            foreach (var bone in ExecutionOrder)
            {
                if (!tasks.TryGetValue(bone, out var task)) continue;
                var newTask = executor.ExecuteTask(task);
                if (newTask is null) continue;

                // 新しい Task の対象 Bone は実行順が後のものしか存在しない前提とする
                tasks[newTask.Target.Bone] = tasks.TryGetValue(newTask.Target.Bone, out var cachedTask)
                    ? Merge(cachedTask, newTask)
                    : newTask;
            }

            Feedback(executor);
        }

        private static IForceTask Merge(IForceTask cachedTask, SingleForceTask task)
        {
            switch (cachedTask)
            {
                case MultiForceTask multiForceTask:
                    multiForceTask.Merge(task);
                    return multiForceTask;
                case SingleForceTask singleForceTask:
                {
                    var newTask = new MultiForceTask(singleForceTask.Target);
                    newTask.Merge(singleForceTask);
                    newTask.Merge(task);
                    return newTask;
                }
                default:
                    throw new InvalidOperationException();
            }
        }

        public void Enqueue(FeedbackTask task)
        {
            _feedbackTaskQueue.Enqueue(task);
        }

        private void Feedback(IForceTaskExecutor executor)
        {
            // ForceTask に変換する
            var tasks = new Dictionary<HumanBodyBones, IForceTask>();

            while (_feedbackTaskQueue.TryDequeue(out var queuedTask))
            {
                var task = queuedTask.ToForceTask();
                // １つの骨に対して複数の力をフィードバックすることはない
                tasks.Add(task.Target.Bone, task);
            }

            // 末端(手足頭)から順にタスクを実行する
            foreach (var bone in ExecutionOrder)
            {
                // UpperChest 以降にはフィードバックしない
                // (根元側を回転すると末端側の位置がずれるため)
                if (bone == HumanBodyBones.UpperChest) break;

                if (!tasks.TryGetValue(bone, out var task)) continue;
                var newTask = executor.ExecuteTask(task);
                if (newTask is null) continue;

                // 新しい Task の対象 Bone は実行順が後のものしか存在しない前提とする
                // 同一の Bone に複数の Feedback が発生する場合は末端側のみを適用する
                // (根元側を適用すると末端側の位置がずれるため)
                tasks[newTask.Target.Bone] = newTask;
            }
        }
    }
}
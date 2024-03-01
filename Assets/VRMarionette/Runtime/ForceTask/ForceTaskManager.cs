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

                // 新しい Task の対象 Bone は実行順が後のものしか存在しない
                tasks[newTask.Target.Bone] = tasks.TryGetValue(newTask.Target.Bone, out var cachedTask)
                    ? Merge(cachedTask, newTask)
                    : newTask;
            }
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
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace VRMarionette.ForceTask
{
    public class MultiForceTask : IForceTask
    {
        public BoneProperty Target { get; }

        public IEnumerable<SingleForceTask> Tasks => _tasks;

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

        public void Merge(SingleForceTask task)
        {
            _tasks.Add(task);
        }
    }
}
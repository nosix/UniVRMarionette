using UnityEngine;

namespace VRMarionette.ForceTask
{
    public class FeedbackTask
    {
        private readonly BoneProperty _target;
        private readonly Vector3 _goalPosition;
        private readonly Vector3 _localSourcePosition;

        public FeedbackTask(BoneProperty target, Vector3 goalPosition, Vector3 localSourcePosition)
        {
            _target = target;
            _goalPosition = goalPosition;
            _localSourcePosition = localSourcePosition;
        }

        public SingleForceTask ToForceTask()
        {
            var currSourcePosition = _target.Transform.TransformPoint(_localSourcePosition);
            var force = _goalPosition - currSourcePosition;
            var forceTask = new SingleForceTask(
                _target,
                currSourcePosition,
                force,
                rotation: null,
                isPushing: true,
                allowBodyMovement: false
            );
            return forceTask;
        }
    }
}
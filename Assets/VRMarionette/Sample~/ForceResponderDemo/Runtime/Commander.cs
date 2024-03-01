using System;
using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace VRMarionette_Sample.ForceResponderDemo.Runtime
{
    public class Commander : MonoBehaviour
    {
        public Transform indicator;

        public Command[] commands;

        private ForceResponder _forceResponder;
        private Animator _animator;

        private readonly BoneSnapshot _snapshot = new();

        public void SetUp(Vrm10Instance instance)
        {
            _forceResponder = instance.GetComponent<ForceResponder>();
            _animator = instance.GetComponent<Animator>();
        }

        public void Reset()
        {
            _snapshot.Restore(_animator);
        }

        public void Execute()
        {
            SetPosition();
            _snapshot.Capture(_animator);

            foreach (var c in commands)
            {
                var targetTransform = _animator.GetBoneTransform(c.targetBone);
                var forcePoint = c.StartTransform.position;
                var force = c.GoalTransform.position - forcePoint;
                var rotation = c.StartTransform.rotation;
                c.StartTransform.name = $"{c.startBone} Start";
                c.GoalTransform.name = $"{c.startBone} Goal";

                _forceResponder.QueueForce(
                    targetTransform,
                    forcePoint,
                    force,
                    rotation,
                    c.isPushing,
                    c.allowBodyMovement
                );
            }
        }

        public void SetPosition()
        {
            foreach (var c in commands)
            {
                c.StartTransform ??= Instantiate(indicator, transform);
                c.GoalTransform ??= Instantiate(indicator, transform);
                var startBoneTransform = _animator.GetBoneTransform(c.startBone);
                var startPosition = startBoneTransform.position + c.startOffset;
                c.StartTransform.position = startPosition;
                c.GoalTransform.position = startPosition + c.movement;
            }
        }

        [Serializable]
        public class Command
        {
            public HumanBodyBones targetBone;
            public HumanBodyBones startBone;
            public Vector3 startOffset;
            public Vector3 movement;
            public bool isPushing;
            public bool allowBodyMovement;

            public Transform StartTransform { set; get; }
            public Transform GoalTransform { set; get; }
        }
    }
}
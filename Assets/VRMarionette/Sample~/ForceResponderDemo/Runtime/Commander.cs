using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace VRMarionette_Sample.ForceResponderDemo.Runtime
{
    public class Commander : MonoBehaviour
    {
        public HumanBodyBones targetBone;
        public bool allowMultiSource;
        public bool allowBodyMovement;

        public HumanBodyBones startBone;
        public Vector3 startOffset;
        public Vector3 movement;

        public Transform startTransform;
        public Transform goalTransform;

        private ForceResponder _forceResponder;
        private HumanoidMixer _mixer;
        private Animator _animator;

        public void SetUp(Vrm10Instance instance)
        {
            _forceResponder = instance.GetComponent<ForceResponder>();
            _animator = instance.GetComponent<Animator>();
            _mixer = GameObjectExtensions.GetOrAddComponent<HumanoidMixer>(instance.gameObject);
        }

        public void Reset()
        {
            _mixer.Reset();
        }

        public void Execute()
        {
            SetPosition();
            var targetTransform = _animator.GetBoneTransform(targetBone);
            var forcePoint = startTransform.position;
            var force = goalTransform.position - forcePoint;
            var rotation = startTransform.rotation;

            _forceResponder.QueueForce(
                targetTransform,
                forcePoint,
                force,
                rotation,
                allowMultiSource,
                allowBodyMovement
            );
        }

        public void SetPosition()
        {
            var startBoneTransform = _animator.GetBoneTransform(startBone);
            var startPosition = startBoneTransform.position + startOffset;
            startTransform.position = startPosition;
            goalTransform.position = startPosition + movement;
        }
    }
}
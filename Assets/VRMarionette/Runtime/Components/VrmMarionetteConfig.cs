using System;
using UnityEngine;
using UniVRM10;

namespace VRMarionette
{
    public class VrmMarionetteConfig : MonoBehaviour
    {
        [Header("HumanoidMixer")] [Space]
        public bool enableMixer;

        [Header("ForceResponder")] [Space]
        public bool verbose;

        public bool filterZero = true;

        [Header("GravityApplier")] [Space]
        public bool isKinematic;

        [Tooltip(
            "This value range is considered to be almost the same height " +
            "when comparing the height of body parts.")]
        public float nearDistance = 0.03f;

        [Tooltip(
            "If the distance between the center of gravity and the ground contact point is within this value range, " +
            "the body will stand upright.")]
        public Vector3 uprightThresholdDistance = new(0.2f, 1f, 0.2f);

        public Transform centroid;
        public Transform bottom;

        [Header("PostureControl")] [Space]
        public PostureControlType postureControl = PostureControlType.Fall;

        public void Setup(Vrm10Instance instance)
        {
            SetupControlRigMixer(instance.gameObject);
            SetupForceGenerator(instance.gameObject);
            SetupRigidbody(instance.gameObject);
            SetupPostureControl(instance.gameObject);
        }

        private void SetupControlRigMixer(GameObject instance)
        {
            if (!enableMixer) return;
            var mixer = instance.GetOrAddComponent<HumanoidMixer>();
            if (mixer.manipulator is not null) return;
            mixer.manipulator = instance.GetComponent<HumanoidManipulator>();
        }

        private void SetupForceGenerator(GameObject instance)
        {
            var forceGenerator = instance.GetComponent<ForceResponder>();
            if (forceGenerator is null) return;
            forceGenerator.verbose = verbose;
            forceGenerator.filterZero = filterZero;
        }

        private void SetupRigidbody(GameObject instance)
        {
            var vrmRigidbody = instance.GetComponent<GravityApplier>();
            if (vrmRigidbody is null) return;
            vrmRigidbody.isKinematic = isKinematic;
            vrmRigidbody.nearDistance = nearDistance;
            vrmRigidbody.uprightThresholdDistance = uprightThresholdDistance;
            vrmRigidbody.centroid = centroid;
            vrmRigidbody.bottom = bottom;
        }

        private void SetupPostureControl(GameObject instance)
        {
            var vrmRigidbody = instance.GetComponent<GravityApplier>();
            if (vrmRigidbody is null) return;

            var postureControlInstance = postureControl switch
            {
                PostureControlType.Fall => gameObject.GetOrAddComponent<PostureControlFall>(),
                PostureControlType.Custom => gameObject.GetComponent<IPostureControl>(),
                _ => throw new ArgumentOutOfRangeException()
            };
            postureControlInstance?.Initialize(instance);
            vrmRigidbody.PostureControl = postureControlInstance;
        }

        public enum PostureControlType
        {
            Fall,
            Custom
        }
    }
}
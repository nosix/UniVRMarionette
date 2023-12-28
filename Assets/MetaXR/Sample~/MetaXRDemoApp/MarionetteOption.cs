using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace MetaXR_Sample.MetaXRDemoApp
{
    public class MarionetteOption : MonoBehaviour
    {
        public bool useRemainingForceForMovement;
        public bool useRigidbody;

        private VrmRigidbody _rigidbody;

        public void Initialize(Vrm10Instance instance)
        {
            var forceGenerator = instance.GetComponent<VrmForceGenerator>();
            forceGenerator.useRemainingForceForMovement = useRemainingForceForMovement;

            _rigidbody = instance.GetComponent<VrmRigidbody>();
            _rigidbody.isKinematic = !useRigidbody;
        }

        public void OnFocus(FocusEvent focusEvent)
        {
            if (_rigidbody is null) return;

            if (!useRigidbody)
            {
                _rigidbody.isKinematic = !useRigidbody;
                return;
            }

            if (!focusEvent.On)
            {
                _rigidbody.isKinematic = false;
            }
            else
            {
                _rigidbody.isKinematic = focusEvent.Bone == HumanBodyBones.Hips;
            }
        }
    }
}
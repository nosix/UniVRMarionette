using UnityEngine;
using UniVRM10;
using VRMarionette;

namespace MetaXR_Sample.MetaXRDemoApp
{
    public class RigidbodyMode : MonoBehaviour
    {
        public bool on;

        private VrmRigidbody _rigidbody;

        public void Initialize(Vrm10Instance instance)
        {
            _rigidbody = instance.GetComponent<VrmRigidbody>();
            _rigidbody.isKinematic = !on;
        }

        public void OnFocus(FocusEvent focusEvent)
        {
            if (_rigidbody is null) return;

            if (!on)
            {
                _rigidbody.isKinematic = !on;
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
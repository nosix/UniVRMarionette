using UnityEngine;
using VRMarionette;
using VRMarionette.Util;

namespace MetaXR_Sample.MetaXRDemoApp
{
    public class FocusIndicator : MonoBehaviour, IFocusIndicator
    {
        public Capsule capsule;

        public Transform Transform => capsule.transform;

        public void SetCapsule(CapsuleCollider capsuleCollider)
        {
            capsule.SetCapsule(capsuleCollider);
        }

        public void Activate(bool activate)
        {
            capsule.Activate(activate);
        }
    }
}
using UnityEngine;
using VRMarionette;

namespace MetaXRSupport_Samples.MetaXRDemoApp
{
    public class FocusIndicator : MonoBehaviour, IFocusIndicator
    {
        public Capsule.Capsule capsule;

        public Transform Transform => capsule.transform;

        public Color Color
        {
            set => capsule.Color = value;
        }

        public void SetCapsule(CapsuleCollider capsuleCollider)
        {
            capsule.SetCapsule(capsuleCollider);
        }
    }
}
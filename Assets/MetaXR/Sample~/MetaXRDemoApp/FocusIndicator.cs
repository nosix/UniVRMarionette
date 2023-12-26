using UnityEngine;
using VRMarionette;
using VRMarionette.Util;

namespace MetaXR_Sample.MetaXRDemoApp
{
    public class FocusIndicator : MonoBehaviour, IFocusIndicator
    {
        public Capsule capsule;

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
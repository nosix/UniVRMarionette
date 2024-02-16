using UnityEngine;

namespace VRMarionette
{
    public interface IFocusIndicator
    {
        Transform Transform { get; }
        void SetCapsule(CapsuleCollider capsuleCollider);
        void Activate(bool activate);
    }
}
using UnityEngine;

namespace VRMarionette
{
    public interface IFocusIndicator
    {
        Transform Transform { get; }
        Color Color { set; }
        void SetCapsule(CapsuleCollider capsuleCollider);
    }
}
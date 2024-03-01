using UnityEngine;

namespace VRMarionette
{
    [System.Serializable]
    public class ForceField
    {
        public HumanBodyBones bone;
        public float radius;
        public Vector3 centerOffset;
        public bool isAxisAligned;
    }
}
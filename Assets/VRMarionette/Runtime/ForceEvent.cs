using UnityEngine;

namespace VRMarionette
{
    public struct ForceEvent
    {
        public HumanBodyBones Bone;
        public bool Hold;
        public Vector3 Distance;
        public Direction AxisDirection;
    }
}
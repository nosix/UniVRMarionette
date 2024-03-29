using UnityEngine;

namespace VRMarionette
{
    [System.Serializable]
    public class HumanLimit
    {
        public HumanBodyBones bone;
        public Direction axis;
        public Vector3 min;
        public Vector3 max;
    }
}
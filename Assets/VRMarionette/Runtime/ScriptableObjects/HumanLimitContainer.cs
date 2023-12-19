using UnityEngine;

namespace VRMarionette
{
    [CreateAssetMenu(fileName = "HumanLimitContainer", menuName = "VRMarionette/HumanLimits", order = 0)]
    public class HumanLimitContainer : ScriptableObject
    {
        public HumanLimit[] humanLimits;
    }
}
using UnityEngine;

namespace VRMarionette
{
    [CreateAssetMenu(fileName = "BodyWeightContainer", menuName = "VRMarionette/BodyWeight", order = 2)]
    public class BodyWeightContainer : ScriptableObject
    {
        public BodyWeight[] bodyWeights;
    }
}
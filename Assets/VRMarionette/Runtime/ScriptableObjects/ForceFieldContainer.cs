using UnityEngine;

namespace VRMarionette
{
    [CreateAssetMenu(fileName = "ForceFieldContainer", menuName = "VRMarionette/ForceField", order = 1)]
    public class ForceFieldContainer : ScriptableObject
    {
        public ForceField[] forceFields;
    }
}
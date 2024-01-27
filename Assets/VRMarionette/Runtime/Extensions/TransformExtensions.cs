using System.Collections.Generic;
using UnityEngine;

namespace VRMarionette
{
    public static class TransformExtensions
    {
        public static IEnumerable<Transform> GetChildren(this Transform self)
        {
            foreach (Transform child in self)
            {
                yield return child;
            }
        }
    }
}
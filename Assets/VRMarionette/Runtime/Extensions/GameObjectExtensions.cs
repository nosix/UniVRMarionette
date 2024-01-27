using UnityEngine;

namespace VRMarionette
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            var component = self.GetComponent<T>();
            return !component ? self.AddComponent<T>() : component;
        }
    }
}
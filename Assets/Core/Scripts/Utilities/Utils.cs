using UnityEngine;

namespace Utilities
{
    public static class Utils
    {
        public static void  DisableComponentsInChildren<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponentsInChildren<T>();
            foreach (T component in components)
            {
                // component.enabled = false;
            }

        }
    
    }
}

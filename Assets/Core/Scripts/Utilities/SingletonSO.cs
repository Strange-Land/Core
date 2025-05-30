using UnityEngine;

namespace Core.Utilities
{
    public abstract class SingletonSO<T> : ScriptableObject where T : ScriptableObject
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<T>(typeof(T).Name);

                    if (instance == null)
                    {
                        Debug.LogError($"SingletonSO<{typeof(T).Name}> not found in Resources folder!!!");
                    }
                }
                return instance;
            }
        }

        protected virtual void OnDisable()
        {
            instance = null;
        }
    }
}
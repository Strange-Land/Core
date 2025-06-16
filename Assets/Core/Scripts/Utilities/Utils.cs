using UnityEngine;
using System.Runtime.CompilerServices;
namespace Core.Utilities
{
    public static class Utils
    {
        public static void DisableComponentsInChildren<T>(GameObject gameObject) where T : Component
        {
            T[] components = gameObject.GetComponentsInChildren<T>();
            foreach (T component in components)
            {
                // component.enabled = false;
            }

        }
        
        
        /// <summary>
        /// Linearly remaps [value] from [inMin,inMax] to [outMin,outMax].
        /// If clampOutput is true, the normalized t is clamped to [0,1] before remapping.
        /// Marked AggressiveInlining so the branch on clampOutput can be folded away
        /// when called with a constant.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Map(
            float value,
            float inMin,
            float inMax,
            float outMin,
            float outMax,
            bool clampOutput = false
        )
        {
            float invRange = 1f / (inMax - inMin);
            float t = (value - inMin) * invRange;
            if (clampOutput)
                t = Mathf.Clamp01(t);
            return t * (outMax - outMin) + outMin;
        }

    }
    [System.Serializable]
    public class Pid {
        public float pFactor, iFactor, dFactor;
		
        float integral;
        float lastError;
	
	
        public Pid(float pFactor, float iFactor, float dFactor) {
            this.pFactor = pFactor;
            this.iFactor = iFactor;
            this.dFactor = dFactor;
        }
	
	
        public float Update(float setpoint, float actual, float timeFrame) {
            float present = setpoint - actual;
            integral += present * timeFrame;
            float deriv = (present - lastError) / timeFrame;
            lastError = present;
            return present * pFactor + integral * iFactor + deriv * dFactor;
        }
    }
}

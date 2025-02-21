using UnityEngine;

namespace Core.SceneEntities.NetworkedComponents
{
    public class StrangeLandTransform : MonoBehaviour
    {
        public string OverrideName = "";

        public bool LogPosition = true;
        public bool LogRotation = true;
        public bool LogScale = true;

        public bool SyncTransforms = true;
    }
}
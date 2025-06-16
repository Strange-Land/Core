using System.Collections.Generic;
using Core.Utilities;
using UnityEngine;

namespace Core.Networking
{
    public class ConnectionAndSpawningSO : ScriptableObject
    {
        public List<GameObject> ResearcherPrefabs;
        public GameObject ResearcherCameraPrefab;
        public SceneField WaitingRoomScene;
        public List<SceneField> ScenarioScenes = new List<SceneField>();
    }
}
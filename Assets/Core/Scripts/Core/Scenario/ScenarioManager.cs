using System.Collections.Generic;
using Core.Networking;
using UnityEngine;
using Core.Utilities;

namespace Core.Scenario
{
    public class ScenarioManager : MonoBehaviour
    {
        [SerializeField] private SceneField _visualSceneToUse;

        private Dictionary<ParticipantOrder, Pose> _mySpawnPositions;
        public bool IsInitialized { get; private set; } = false;

        private void Start()
        {
            UpdateSpawnPoints();
            IsInitialized = true;
        }

        public bool HasVisualScene()
        {
            if (_visualSceneToUse != null && _visualSceneToUse.SceneName.Length > 0)
            {
                Debug.Log("Visual Scene is set to: " + _visualSceneToUse.SceneName);
                return true;
            }
            return false;
        }

        public string GetVisualSceneName()
        {
            return _visualSceneToUse.SceneName;
        }


        public Pose GetSpawnPose(ParticipantOrder participantOrder)
        {
            Pose ret;

            // Ensure _mySpawnPositions is initialized before accessing
            if (!IsInitialized || _mySpawnPositions == null)
            {
                Debug.LogError($"ScenarioManager not fully initialized or spawn points dictionary is null when trying to get spawn pose for {participantOrder}.");
                return new Pose(Vector3.zero, Quaternion.identity);
            }

            if (_mySpawnPositions.TryGetValue(participantOrder, out var position))
            {
                ret = position;
            }
            else
            {
                Debug.LogWarning($"Did not find an assigned spawn point for {participantOrder}!");
                ret = new Pose(Vector3.zero, Quaternion.identity); // Return a default pose
            }

            return ret;
        }
        private void UpdateSpawnPoints()
        {
            _mySpawnPositions = new Dictionary<ParticipantOrder, Pose>();

            foreach (var spawnPoint in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
            {
                if (_mySpawnPositions.ContainsKey(spawnPoint.PO))
                {
                    Debug.LogError($"Duplicate ParticipantOrder found: {spawnPoint.PO}! Check your setting!");
                    continue;
                }
                _mySpawnPositions.Add(spawnPoint.PO, new Pose(spawnPoint.transform.position, spawnPoint.transform.rotation));
            }
        }

    }
}
using System.Collections.Generic;
using Core.Networking;
using UnityEngine;
using Utilities.SceneField;

namespace Core.Scenario
{
    public class ScenarioManager : MonoBehaviour
    {
        [SerializeField] private SceneField _visualSceneToUse;

        private Dictionary<ParticipantOrder, Pose> _mySpawnPositions;

        private void Start()
        {
            UpdateSpawnPoints();
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

            if (_mySpawnPositions != null)
            {
                if (_mySpawnPositions.TryGetValue(participantOrder, out var position))
                {
                    ret = position;
                }
                else
                {
                    Debug.LogWarning($"Did not find an assigned spawn point for {participantOrder}!");
                    ret = new Pose();
                }
            }
            else
            {
                Debug.LogError("Spawn points dictionary is null!");
                ret = new Pose();
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

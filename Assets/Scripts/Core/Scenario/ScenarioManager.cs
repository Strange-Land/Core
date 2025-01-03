using System;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    [SerializeField] private SceneField _visualSceneToUse;

    private Dictionary<ParticipantOrder, Pose> _mySpawnPositions;

    private void Start()
    {
        UpdateSpawnPoints();
    }

    public bool HasVisualScene() {
        if (_visualSceneToUse != null && _visualSceneToUse.SceneName.Length > 0) {
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
        
        if (_mySpawnPositions != null) {
            if (_mySpawnPositions.TryGetValue(participantOrder, out var position)) {
                ret = position;
            }
            else
            {            
                Debug.Log("Did not find an assigned spawn point");
                ret = new Pose();
            }
        }
        else
        {
            Debug.Log("SpawnPoint is null");
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

using System.Collections.Generic;
using Core.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.Utilities;

namespace Core.UI
{
    public class ResearcherUI : MonoBehaviour
    {
        public GameObject SceneButtonPrefab;

        public Transform SceneButtonParent;

        public TMP_Text ServerState;

        private void Start()
        {
            List<SceneField> scenes = ConnectionAndSpawning.Instance.ScenarioScenes;
            foreach (var scene in scenes)
            {
                var sceneButton = Instantiate(SceneButtonPrefab, SceneButtonParent);
                sceneButton.GetComponent<Button>().onClick.AddListener(() => SwitchToScenario(scene.SceneName));
                sceneButton.GetComponentInChildren<TMP_Text>().text = scene.SceneName;
            }
        }

        private void Update()
        {
            string serverState = ConnectionAndSpawning.Instance.GetServerState();
            serverState = serverState.Substring(5);
            ServerState.text = $"Server State: {serverState}";
        }

        private void SwitchToScenario(string scenarioName)
        {
            ConnectionAndSpawning.Instance.SwitchToLoading(scenarioName);
        }

        [ContextMenu("SwitchToInteract")]
        public void SwitchToInteract()
        {
            ConnectionAndSpawning.Instance.SwitchToInteract();
        }

        public void SwitchToWaiting()
        {
            ConnectionAndSpawning.Instance.BackToWaitingRoom();
        }
    }
}


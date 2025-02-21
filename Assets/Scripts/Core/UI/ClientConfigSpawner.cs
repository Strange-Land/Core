using System.IO;
using System.Linq;
using Core.Networking;
using Core.SceneEntities.NetworkedComponents.ClientInterface;
using Core.SceneEntities.NetworkedComponents.InteractableObject;
using UnityEngine;

/* My design thought is that, we don't need update the config file on every change,
So this script handles the spawning of elements and keeps a local reference,
And startup script simply asks this script to update the clientoptions when needed. 
*/

namespace Core.UI
{
    public class ClientConfigSpawner : MonoBehaviour
    {
        public GameObject clientConfigPrefab;

        private const string FILE_NAME = "ClientOptions.json";
        private string FilePath => Application.persistentDataPath + "/" + FILE_NAME;
    
        private ClientConfigUI[] spawnedConfigUIs;

        public void SpawnConfigs()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
            
                ClientOptions loadedInstance = JsonUtility.FromJson<ClientOptions>(json);
                if (loadedInstance != null && loadedInstance.Options.Count == 6)
                {
                    ClientOptions.Instance = loadedInstance;
                    Debug.Log("Loaded existing ClientOptions from disk.");
                }
                else
                {
                    Debug.LogWarning("File found but had invalid data!");
                }
            }
            else
            {
                Debug.Log("No config file found. Using default ClientOptions.");
            }
        
            spawnedConfigUIs = new ClientConfigUI[6];

            for (int i = 0; i < 6; i++)
            {
                var option = ClientOptions.Instance.Options[i];

                var go = Instantiate(clientConfigPrefab, transform);
                var ui = go.GetComponent<ClientConfigUI>();
                spawnedConfigUIs[i] = ui;

                // ??? using color tag gives errors -- will investigate later
                // ui.POText.text = $"""Participant Order <color="blue">{option.PO}</color>""";
                ui.POText.text = $"Participant Order {option.PO}";
            
                var interfaceNames = ClientDisplaysSO.Instance.ClientDisplays
                    .Select(ci => ci.ID)
                    .ToList();

                ui.ClientDisplayDropdown.ClearOptions();
                ui.ClientDisplayDropdown.AddOptions(interfaceNames);

                ui.ClientDisplayDropdown.value = option.ClientDisplay;
                ui.ClientDisplayDropdown.RefreshShownValue();

                var objNames = InteractableObjectsSO.Instance.InteractableObjects
                    .Select(io => io.ID)
                    .ToList();

                ui.SpawnTypeDropdown.ClearOptions();
                ui.SpawnTypeDropdown.AddOptions(objNames);

                ui.SpawnTypeDropdown.value = option.InteractableObject;
                ui.SpawnTypeDropdown.RefreshShownValue();
            
            }
        }

        public void UpdateClientOptionsFromUI()
        {
            if (spawnedConfigUIs == null || spawnedConfigUIs.Length < 6) return;

            for (int i = 0; i < 6; i++)
            {
                var ui = spawnedConfigUIs[i];
                var option = ClientOptions.Instance.Options[i];
                option.ClientDisplay = ui.ClientDisplayDropdown.value;
                option.InteractableObject = ui.SpawnTypeDropdown.value;

                ClientOptions.Instance.Options[i] = option;
            }
        }
    }
}

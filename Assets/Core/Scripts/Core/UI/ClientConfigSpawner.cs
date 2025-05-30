using System.Collections.Generic;
using System.Linq;
using Core.Networking;
using Core.SceneEntities;
using UnityEngine;

namespace Core.UI
{
    public class ClientConfigSpawner : MonoBehaviour
    {
        public GameObject clientConfigPrefab;

        private ClientConfigUI[] spawnedConfigUIs;

        public void SpawnConfigs()
        {
            spawnedConfigUIs = new ClientConfigUI[6];

            for (int i = 0; i < 6; i++)
            {
                var option = ClientOptions.Instance.Options[i];

                var go = Instantiate(clientConfigPrefab, transform);
                var ui = go.GetComponent<ClientConfigUI>();
                spawnedConfigUIs[i] = ui;

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

            var updatedOptions = new List<ClientOption>();

            for (int i = 0; i < 6; i++)
            {
                var ui = spawnedConfigUIs[i];
                var option = new ClientOption
                {
                    PO = (ParticipantOrder)i,
                    ClientDisplay = ui.ClientDisplayDropdown.value,
                    InteractableObject = ui.SpawnTypeDropdown.value
                };
                updatedOptions.Add(option);
            }

            GlobalConfig.SetClientOptions(updatedOptions);
        }
    }
}
using System.Collections;
using System.Diagnostics;
using System.IO;
using Core.Networking;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class ResearcherStartupUI : MonoBehaviour
    {
        [SerializeField] private ClientConfigSpawner clientConfigSpawner;
        [SerializeField] private Button OpenDataFolderButton;
        [SerializeField] private Button selectDataFolderButton;
        [SerializeField] private TMP_Text currentDataPathText;

        private const string FILE_NAME = "ClientOptions.json";
        private string FilePath => Application.persistentDataPath + "/" + FILE_NAME;

        private void Start()
        {
            clientConfigSpawner.SpawnConfigs();
            UpdateDataPathDisplay();

            if (selectDataFolderButton != null)
            {
                selectDataFolderButton.onClick.AddListener(SelectDataStorageFolder);
            }

            if (OpenDataFolderButton != null)
            {
                OpenDataFolderButton.onClick.AddListener(OpenDataFolder);
            }
        }

        public void StartServer()
        {
            SaveConfigToJson();
            ConnectionAndSpawning.Instance.StartAsServer();
        }

        public void StartHost()
        {
            ConnectionAndSpawning.Instance.StartAsHost();
        }

        [ContextMenu("Open Data Folder")]
        public void OpenDataFolder()
        {
            string path = GlobalConfig.GetDataStoragePath();
#if UNITY_STANDALONE_WIN
            Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_STANDALONE_OSX
            Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
            Process.Start("xdg-open", path);
#else
            UnityEngine.Debug.Log("Open file explorer is not implemented on this platform.");
#endif
        }

        [ContextMenu("Select Data Storage Folder")]
        public void SelectDataStorageFolder()
        {
            StartCoroutine(ShowSelectFolderDialog());
        }

        private IEnumerator ShowSelectFolderDialog()
        {
            yield return FileBrowser.WaitForLoadDialog(
                FileBrowser.PickMode.Folders,
                false,
                GlobalConfig.GetDataStoragePath(),
                null,
                "Select Data Storage Folder",
                "Select"
            );

            if (FileBrowser.Success)
            {
                string selectedPath = FileBrowser.Result[0];
                GlobalConfig.SetDataStoragePath(selectedPath);
                UpdateDataPathDisplay();
                UnityEngine.Debug.Log($"Data storage path updated to: {selectedPath}");
            }
        }

        private void UpdateDataPathDisplay()
        {
            if (currentDataPathText != null)
            {
                string currentPath = GlobalConfig.GetDataStoragePath();
                bool isCustom = GlobalConfig.Data.UseCustomDataPath;

                if (isCustom)
                {
                    currentDataPathText.text = $"Data Path: {currentPath}";
                    currentDataPathText.color = Color.green;
                }
                else
                {
                    currentDataPathText.text = $"Please select a data storage folder";
                    currentDataPathText.color = Color.red;
                }
            }
        }

        [ContextMenu("Reset Data Path")]
        public void ResetDataPath()
        {
            GlobalConfig.SetDataStoragePath("");
            UpdateDataPathDisplay();
            UnityEngine.Debug.Log("Data storage path reset to default.");
        }

        private void SaveConfigToJson()
        {
            clientConfigSpawner.UpdateClientOptionsFromUI();

            string json = JsonUtility.ToJson(ClientOptions.Instance, true);

            File.WriteAllText(FilePath, json);
        }
    }
}
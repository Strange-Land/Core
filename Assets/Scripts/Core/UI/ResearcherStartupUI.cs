using System.Collections;
using System.Diagnostics;
using Core.Networking;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class ResearcherStartupUI : MonoBehaviour
    {
        [SerializeField] private ClientConfigSpawner _clientConfigSpawner;
        [SerializeField] private Button _openDataFolderButton;
        [SerializeField] private Button _selectDataFolderButton;
        [SerializeField] private TMP_Text _currentDataPathText;
        [SerializeField] private TMP_InputField _ipAddressInputField;

        private void Start()
        {
            _clientConfigSpawner.SpawnConfigs();
            UpdateDataPathDisplay();

            if (_selectDataFolderButton != null)
            {
                _selectDataFolderButton.onClick.AddListener(SelectDataStorageFolder);
            }

            if (_openDataFolderButton != null)
            {
                _openDataFolderButton.onClick.AddListener(OpenDataFolder);
            }
        }

        public void StartServer()
        {
            _clientConfigSpawner.UpdateClientOptionsFromUI();
            ConnectionAndSpawning.Instance.StartAsServer();
        }

        public void StartHost()
        {
            ConnectionAndSpawning.Instance.StartAsHost();
        }

        public void StartClient()
        {
            ConnectionAndSpawning.Instance.StartAsClient(_ipAddressInputField.text);
        }

        [ContextMenu("Open Data Folder")]
        public void OpenDataFolder()
        {
            string path = GlobalConfig.GetDataStoragePath();
            GUIUtility.systemCopyBuffer = path;
            StartCoroutine(ShowClipboardButtonFeedback());
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
            if (_currentDataPathText != null)
            {
                string currentPath = GlobalConfig.GetDataStoragePath();
                bool isCustom = GlobalConfig.Data.UseCustomDataPath;

                if (isCustom)
                {
                    _currentDataPathText.text = $"Data Path: {currentPath}";
                    _currentDataPathText.color = Color.green;
                }
                else
                {
                    _currentDataPathText.text = $"Please select a data storage folder";
                    _currentDataPathText.color = Color.red;
                }
            }
        }

        private IEnumerator ShowClipboardButtonFeedback()
        {
            if (_openDataFolderButton != null)
            {
                TMP_Text buttonTMPText = _openDataFolderButton.GetComponentInChildren<TMP_Text>();

                if (buttonTMPText != null)
                {
                    string originalText = buttonTMPText.text;
                    Color originalColor = buttonTMPText.color;
                    buttonTMPText.color = Color.red;
                    buttonTMPText.text = "Copied to clipboard!";
                    yield return new WaitForSeconds(1.5f);
                    buttonTMPText.text = originalText;
                    buttonTMPText.color = originalColor;
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
    }
}
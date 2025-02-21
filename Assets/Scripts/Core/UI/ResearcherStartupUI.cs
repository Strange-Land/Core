using System.Diagnostics;
using System.IO;
using Core.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class ResearcherStartupUI : MonoBehaviour
    {
        [SerializeField] private ClientConfigSpawner clientConfigSpawner;
        [SerializeField] private Button saveConfigButton;

        private const string FILE_NAME = "ClientOptions.json";
        private string FilePath => Application.persistentDataPath + "/" + FILE_NAME;

        private void Start()
        {
            clientConfigSpawner.SpawnConfigs();
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

        [ContextMenu("Data Folder")]
        public void OpenDataFolder()
        {
            string path = Application.persistentDataPath;
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

        // pull -> serialize -> write
        private void SaveConfigToJson()
        {
            clientConfigSpawner.UpdateClientOptionsFromUI();

            string json = JsonUtility.ToJson(ClientOptions.Instance,true);

            File.WriteAllText(FilePath, json);
        }
    }
}

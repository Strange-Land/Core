using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Core.Networking
{
    [System.Serializable]
    public class GlobalConfigData
    {
        public string DataStoragePath = "";
        public bool UseCustomDataPath = false;
        public List<ClientOption> ClientOptions = new List<ClientOption>();
        public string IPAddress = "127.0.0.1";
    }

    public static class GlobalConfig
    {
        private const string CONFIG_FILE_NAME = "GlobalConfig.json";
        private static string ConfigFilePath => Path.Combine(Application.persistentDataPath, CONFIG_FILE_NAME);

        private static GlobalConfigData _data;
        private static bool _isLoaded = false;

        public static GlobalConfigData Data
        {
            get
            {
                if (!_isLoaded)
                {
                    LoadConfig();
                }
                return _data;
            }
        }

        public static string GetDataStoragePath()
        {
            var data = Data;
            if (data.UseCustomDataPath && !string.IsNullOrEmpty(data.DataStoragePath))
            {
                return data.DataStoragePath;
            }
            return Application.persistentDataPath;
        }

        public static void SetDataStoragePath(string path)
        {
            if (!_isLoaded)
            {
                LoadConfig();
            }

            _data.DataStoragePath = path;
            _data.UseCustomDataPath = !string.IsNullOrEmpty(path);
            SaveConfig();
        }

        public static string GetIPAddress()
        {
            var data = Data;
            if (string.IsNullOrEmpty(data.IPAddress))
            {
                data.IPAddress = "127.0.0.1";
                SaveConfig();
            }
            return data.IPAddress;
        }

        public static void SetIPAddress(string ipAddress)
        {
            if (!_isLoaded)
            {
                LoadConfig();
            }

            _data.IPAddress = string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress;
            SaveConfig();
        }

        public static List<ClientOption> GetClientOptions()
        {
            if (!_isLoaded)
            {
                LoadConfig();
            }

            if (_data.ClientOptions == null || _data.ClientOptions.Count == 0)
            {
                InitializeDefaultClientOptions();
            }

            return _data.ClientOptions;
        }

        public static void SetClientOptions(List<ClientOption> options)
        {
            if (!_isLoaded)
            {
                LoadConfig();
            }

            _data.ClientOptions = options ?? new List<ClientOption>();
            SaveConfig();
        }

        public static ClientOption GetClientOption(ParticipantOrder po)
        {
            var options = GetClientOptions();
            return options.Find(x => x.PO == po);
        }

        private static void InitializeDefaultClientOptions()
        {
            _data.ClientOptions = new List<ClientOption>();
            for (int i = 0; i < 6; i++)
            {
                ClientOption co = new ClientOption();
                co.PO = (ParticipantOrder)i;
                co.ClientDisplay = 0;
                co.InteractableObject = 0;
                _data.ClientOptions.Add(co);
            }
        }

        public static void LoadConfig()
        {
            if (_isLoaded)
                return;

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    _data = JsonUtility.FromJson<GlobalConfigData>(json);

                    if (_data == null)
                    {
                        _data = new GlobalConfigData();
                        InitializeDefaultClientOptions();
                    }
                    else
                    {
                        if (_data.ClientOptions == null)
                        {
                            InitializeDefaultClientOptions();
                        }

                        if (string.IsNullOrEmpty(_data.IPAddress))
                        {
                            _data.IPAddress = "127.0.0.1";
                        }
                    }
                }
                else
                {
                    _data = new GlobalConfigData();
                    InitializeDefaultClientOptions();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load global config: {e.Message}");
                _data = new GlobalConfigData();
                InitializeDefaultClientOptions();
            }

            _isLoaded = true;
        }

        public static void SaveConfig()
        {
            try
            {
                if (_data == null)
                {
                    _data = new GlobalConfigData();
                    InitializeDefaultClientOptions();
                }

                string json = JsonUtility.ToJson(_data, true);
                File.WriteAllText(ConfigFilePath, json);
                Debug.Log($"Global config saved to: {ConfigFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save global config: {e.Message}");
            }
        }

        public static void ResetToDefault()
        {
            _data = new GlobalConfigData();
            InitializeDefaultClientOptions();
            SaveConfig();
        }
    }
}
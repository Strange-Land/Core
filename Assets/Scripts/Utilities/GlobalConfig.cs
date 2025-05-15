using System;
using System.IO;
using UnityEngine;

namespace Core.Networking
{
    [System.Serializable]
    public class GlobalConfigData
    {
        public string DataStoragePath = "";
        public bool UseCustomDataPath = false;
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
            if (Data.UseCustomDataPath && !string.IsNullOrEmpty(Data.DataStoragePath))
            {
                return Data.DataStoragePath;
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

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    _data = JsonUtility.FromJson<GlobalConfigData>(json);

                    if (_data == null)
                    {
                        CreateDefaultConfig();
                    }
                }
                else
                {
                    CreateDefaultConfig();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load global config: {e.Message}");
                CreateDefaultConfig();
            }

            _isLoaded = true;
        }

        public static void SaveConfig()
        {
            try
            {
                if (_data == null)
                {
                    CreateDefaultConfig();
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

        private static void CreateDefaultConfig()
        {
            _data = new GlobalConfigData();
        }

        public static void ResetToDefault()
        {
            CreateDefaultConfig();
            SaveConfig();
        }
    }
}
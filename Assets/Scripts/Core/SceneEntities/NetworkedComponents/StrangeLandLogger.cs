using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Core.Networking;
using Core.SceneEntities.NetworkedComponents;
using Unity.Netcode;
using UnityEngine;

namespace Core.SceneEntities
{
    public class StrangeLandLogger : MonoBehaviour
    {
        public const char sep = ';';
        public const string Fpres = "F6";
        public float UpdatePerSecond = 25f;
        private float _updatedFreqeuncy => 1f / UpdatePerSecond;
        private readonly ConcurrentQueue<string> databuffer = new ConcurrentQueue<string>();
        private bool doneSending;
        private bool isSending;
        private bool RECORDING;
        private float NextUpdate;
        private double ScenarioStartTime;
        private Thread send;
        private StreamWriter logStream;
        private string path;
        private List<LogItem> logItems;
        private HashSet<string> recordedGameObjects = new HashSet<string>();
        private EServerState lastKnownServerState;
        private bool hasLoggedRecordingStart = false;

        public static StrangeLandLogger Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            doneSending = true;
            if (ConnectionAndSpawning.Instance != null)
            {
                lastKnownServerState = ConnectionAndSpawning.Instance.ServerStateEnum.Value;
                ConnectionAndSpawning.Instance.ServerStateEnum.OnValueChanged += OnServerStateChanged;
            }
        }

        private void OnServerStateChanged(EServerState previousValue, EServerState newValue)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            lastKnownServerState = newValue;

            if (previousValue == EServerState.Interact)
            {
                if (isRecording())
                {
                    StopRecording();
                }
            }

            if (ReadyToRecord())
            {
                string scenarioName = "Unknown";
                if (ConnectionAndSpawning.Instance.GetScenarioManager() != null)
                {
                    scenarioName = ConnectionAndSpawning.Instance.GetScenarioManager().gameObject.scene.name;
                }
                StartRecording(scenarioName, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            }
        }

        private void Update()
        {
            if (!RECORDING) return;

            if (RECORDING && !hasLoggedRecordingStart && Time.time > ScenarioStartTime + 1.0f)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    LogRecordedObjects(true);
                }
                hasLoggedRecordingStart = true;
            }
        }

        private void LateUpdate()
        {
            if (!RECORDING) return;
            if (NextUpdate < Time.time)
            {
                NextUpdate = Time.time + _updatedFreqeuncy;
                string outVal = "";
                foreach (var item in logItems)
                    outVal += item.Serialize() + sep;
                EnqueueData(outVal.TrimEnd(sep) + "\n");

                if (Time.frameCount % 300 == 0)
                {
                    FlushLogStream();
                }
            }
        }

        private void FlushLogStream()
        {
            try
            {
                if (logStream != null)
                {
                    lock (logStream)
                    {
                        logStream.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error flushing log stream: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            if (ConnectionAndSpawning.Instance != null)
            {
                ConnectionAndSpawning.Instance.ServerStateEnum.OnValueChanged -= OnServerStateChanged;
            }
        }

        private void OnApplicationQuit()
        {
            if (isRecording())
            {
                StopRecording();
            }
        }

        public bool ReadyToRecord()
        {
            if (!NetworkManager.Singleton.IsServer)
                return false;

            // Don't record in WaitingRoom
            if (ConnectionAndSpawning.Instance != null &&
                ConnectionAndSpawning.Instance.ServerStateEnum.Value != EServerState.Interact)
                return false;

            if (RECORDING)
            {
                StopRecording();
                return false;
            }

            if (!doneSending) return false;

            return true;
        }

        [ContextMenu("StartRecording")]
        public void StartRecording()
        {
            if (!ReadyToRecord()) return;
            StartRecording("Unknown", "Unknown");
        }

        public void StartRecording(string ScenarioName, string sessionName)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            var basePath = GlobalConfig.GetDataStoragePath();
            var folderpath = Path.Combine(basePath, "Logs");
            Directory.CreateDirectory(folderpath);
            path = Path.Combine(folderpath, $"CSV_Scenario-{ScenarioName}_Session-{sessionName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            InitLogs();
            recordedGameObjects.Clear();
            hasLoggedRecordingStart = false;

            logItems = new List<LogItem>
            {
                new LogItem(null, (refobj) => Time.time.ToString(Fpres), "GameTime"),
                new LogItem(null, (refobj) => (Time.time - ScenarioStartTime).ToString(Fpres), "ScenarioTime"),
                new LogItem(null, (refobj) => Time.smoothDeltaTime.ToString(Fpres), "DeltaTime"),
                new LogItem(null, (refobj) => Time.frameCount.ToString(), "FrameCount")
            };
            StrangeLandTransform[] strangeLandObjects = FindObjectsByType<StrangeLandTransform>(sortMode: FindObjectsSortMode.None);
            foreach (var slt in strangeLandObjects)
            {
                ParticipantOrder PO;
                string parentName;
                // OK so the parent name is a bit wonky to implement, for now the name of gameobject should be sufficient
                FindClosestParentDisplayOrInteractable(slt.transform, out PO, out parentName);
                string finalNameForLog = !string.IsNullOrEmpty(slt.OverrideName) ? slt.OverrideName : slt.gameObject.name;
                string labelPrefix = PO.ToString() + "_" + finalNameForLog;

                // Record the GameObject for logging
                recordedGameObjects.Add(slt.gameObject.name);

                if (slt.LogPosition)
                {
                    logItems.Add(new LogItem(slt.gameObject, PositionLog, labelPrefix + "_Pos"));
                }
                if (slt.LogRotation)
                {
                    logItems.Add(new LogItem(slt.gameObject, OrientationLog, labelPrefix + "_Rot"));
                }
                if (slt.LogScale)
                {
                    logItems.Add(new LogItem(slt.gameObject, ScaleLog, labelPrefix + "_Scale"));
                }
            }
            var headerRow = "";
            foreach (var item in logItems)
                headerRow += item.GetJsonPropertyName() + sep;
            EnqueueData(headerRow.TrimEnd(sep) + "\n");
            doneSending = false;
            isSending = true;
            send = new Thread(ContinuousDataSend);
            send.Start();
            Debug.Log($"Started Recording to file: {path}");
            ScenarioStartTime = Time.time;
            RECORDING = true;
        }

        [ContextMenu("StopRecording")]
        public void StopRecording()
        {
            if (!isRecording()) return;

            // Stop the data sending thread
            isSending = false;

            // Flush any remaining data in the buffer
            string data;
            while (databuffer.TryDequeue(out data))
            {
                try
                {
                    if (logStream != null)
                    {
                        logStream.Write(data);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error writing log data: {e}");
                }
            }

            // Close the stream and log
            RECORDING = false;
            CloseLogs();

            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    LogRecordedObjects(false);
                }
            }
            doneSending = true;
        }

        private void LogRecordedObjects(bool isStart)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            StringBuilder sb = new StringBuilder();
            if (isStart)
            {
                sb.AppendLine($"Started recording to: {path}");
                sb.AppendLine($"Recording {recordedGameObjects.Count} GameObjects:");
            }
            else
            {
                sb.AppendLine($"Finished recording to: {path}");
                sb.AppendLine($"Recorded {recordedGameObjects.Count} GameObjects:");
            }

            foreach (var objName in recordedGameObjects)
            {
                sb.AppendLine($"- {objName}");
            }

            Debug.Log(sb.ToString());
        }

        public bool isRecording()
        {
            return RECORDING;
        }

        private void EnqueueData(string data)
        {
            databuffer.Enqueue(data);
        }

        private void InitLogs()
        {
            logStream = File.AppendText(path);
        }

        private void CloseLogs()
        {
            if (logStream != null)
            {
                try
                {
                    logStream.Flush();
                    logStream.Close();
                    logStream = null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error closing log stream: {e}");
                }
            }
        }

        private void ContinuousDataSend()
        {
            while (isSending)
            {
                while (databuffer.TryDequeue(out var dat))
                    DataSend(dat);
                Thread.Sleep(100);
            }

            while (databuffer.TryDequeue(out var finalDat))
                DataSend(finalDat);

            doneSending = true;
        }

        private void DataSend(string data)
        {
            try
            {
                if (logStream != null)
                {
                    lock (logStream)
                    {
                        logStream.Write(data);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error writing log data: " + e);
            }
        }

        private string PositionLog(GameObject o)
        {
            if (o == null) return string.Empty;

            Transform t = o.transform;
            Vector3 pos = t.position;
            byte[] buffer = new byte[3 * 4];
            Array.Copy(BitConverter.GetBytes(pos.x), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(pos.y), 0, buffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(pos.z), 0, buffer, 8, 4);
            return Convert.ToBase64String(buffer);
        }

        private string OrientationLog(GameObject o)
        {
            if (o == null) return string.Empty;

            Transform t = o.transform;
            Vector3 euler = t.rotation.eulerAngles;
            byte[] buffer = new byte[3 * 4];
            Array.Copy(BitConverter.GetBytes(euler.x), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(euler.y), 0, buffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(euler.z), 0, buffer, 8, 4);
            return Convert.ToBase64String(buffer);
        }

        private string ScaleLog(GameObject o)
        {
            if (o == null) return string.Empty;

            Transform t = o.transform;
            Vector3 scale = t.lossyScale;
            byte[] buffer = new byte[3 * 4];
            Array.Copy(BitConverter.GetBytes(scale.x), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(scale.y), 0, buffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(scale.z), 0, buffer, 8, 4);
            return Convert.ToBase64String(buffer);
        }

        private void FindClosestParentDisplayOrInteractable(Transform child, out ParticipantOrder pOrder, out string parentName)
        {
            pOrder = ParticipantOrder.None;
            parentName = "None";
            Transform current = child;
            while (current != null)
            {
                var cd = current.GetComponent<ClientDisplay>();
                if (cd != null)
                {
                    pOrder = cd.GetParticipantOrder();
                    parentName = cd.gameObject.name;
                    return;
                }
                var io = current.GetComponent<InteractableObject>();
                if (io != null)
                {
                    pOrder = io.GetParticipantOrder();
                    parentName = io.gameObject.name;
                    return;
                }
                current = current.parent;
            }
        }

        public class LogItem
        {
            private GameObject reference;
            private Func<GameObject, string> logProducer;
            private string jsonPropertyName;
            public LogItem(GameObject reference, Func<GameObject, string> logProducer, string jsonPropertyName)
            {
                this.reference = reference;
                this.logProducer = logProducer;
                this.jsonPropertyName = jsonPropertyName;
            }
            public string Serialize()
            {
                return logProducer(reference);
            }
            public string GetJsonPropertyName()
            {
                return jsonPropertyName;
            }
        }
    }
}
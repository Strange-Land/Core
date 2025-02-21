using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Core.Networking;
using Core.SceneEntities.NetworkedComponents.ClientInterface;
using UnityEngine;

namespace Core.SceneEntities.NetworkedComponents
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
        public static StrangeLandLogger Instance { get; private set; }
        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }
        private void Start()
        {
            doneSending = true;
        }
        private void Update()
        {
            if (!RECORDING) return;
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
            }
        }
        private void OnApplicationQuit()
        {
            if (isRecording())
                StartCoroutine(IEStopRecording());
        }
        public bool ReadyToRecord()
        {
            if (RECORDING)
            {
                StartCoroutine(IEStopRecording());
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
            var folderpath = Application.persistentDataPath + "/Logs";
            Directory.CreateDirectory(folderpath);
            path = Path.Combine(folderpath, $"CSV_Scenario-{ScenarioName}_Session-{sessionName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            InitLogs();
            logItems = new List<LogItem>();
            logItems.Add(new LogItem(null, (refobj) => Time.time.ToString(Fpres), "GameTime"));
            logItems.Add(new LogItem(null, (refobj) => (Time.time - ScenarioStartTime).ToString(Fpres), "ScenarioTime"));
            logItems.Add(new LogItem(null, (refobj) => Time.smoothDeltaTime.ToString(Fpres), "DeltaTime"));
            logItems.Add(new LogItem(null, (refobj) => Time.frameCount.ToString(), "FrameCount"));
            StrangeLandTransform[] strangeLandObjects = FindObjectsByType<StrangeLandTransform>(sortMode: FindObjectsSortMode.None);
            foreach (var slt in strangeLandObjects)
            {
                ParticipantOrder PO;
                string parentName;
                FindClosestParentDisplayOrInteractable(slt.transform, out PO, out parentName);
                string finalNameForLog = !string.IsNullOrEmpty(slt.OverrideName) ? slt.OverrideName : slt.gameObject.name;
                string labelPrefix = PO.ToString() + "_" + parentName + "_" + finalNameForLog;
                if (slt.LogPosition)
                {
                    logItems.Add(new LogItem(slt.transform, PositionLog, labelPrefix + "_Pos"));
                }            
                if (slt.LogRotation)
                {
                    logItems.Add(new LogItem(slt.transform, OrientationLog, labelPrefix + "_Rot"));
                }        
                if (slt.LogScale)
                {
                    logItems.Add(new LogItem(slt.transform, ScaleLog, labelPrefix + "_Scale"));
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
            Debug.Log("Started Recording to file: " + path);
            ScenarioStartTime = Time.time;
            RECORDING = true;
        }
        [ContextMenu("StopRecording")]
        public void StopRecording()
        {
            if (isRecording())
                StartCoroutine(IEStopRecording());
        }
        public IEnumerator IEStopRecording()
        {
            isSending = false;
            Debug.Log("Stopping Recording...");
            yield return new WaitUntil(() => doneSending);
            RECORDING = false;
            CloseLogs();
            Debug.Log("Stopped Recording. File saved to: " + path);
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
            logStream.Close();
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
                logStream.Write(data);
            }
            catch (Exception e)
            {
                Debug.LogError("Error writing log data: " + e);
            }
        }
        private string PositionLog(object o)
        {
            Transform t = (Transform)o;
            Vector3 pos = t.position;
            byte[] buffer = new byte[3 * 4];
            Array.Copy(BitConverter.GetBytes(pos.x), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(pos.y), 0, buffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(pos.z), 0, buffer, 8, 4);
            return Convert.ToBase64String(buffer);
        }
        private string OrientationLog(object o)
        {
            Transform t = (Transform)o;
            Vector3 euler = t.rotation.eulerAngles;
            byte[] buffer = new byte[3 * 4];
            Array.Copy(BitConverter.GetBytes(euler.x), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(euler.y), 0, buffer, 4, 4);
            Array.Copy(BitConverter.GetBytes(euler.z), 0, buffer, 8, 4);
            return Convert.ToBase64String(buffer);
        }
    
        // I actually DK if I should record local or lossy(global) scale here
        private string ScaleLog(object o)
        {
            Transform t = (Transform)o;
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
                var io = current.GetComponent<InteractableObject.InteractableObject>();
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
            private object reference;
            private Func<object, string> logProducer;
            private string jsonPropertyName;
            public LogItem(object reference, Func<object, string> logProducer, string jsonPropertyName)
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

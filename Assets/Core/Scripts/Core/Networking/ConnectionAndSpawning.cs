using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Scenario;
using Core.SceneEntities;
using Core.SceneEntities.NetworkedComponents;
using Newtonsoft.Json;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Utilities;

/* note, feel free to remove
Default -> Waiting Room: ServerStarted
Waiting Room -> Loading Scenario: SwitchToLoading that triggers from UI
Loading Scenario -> Loading Visuals: SceneEvent_Server (base scene load completed)
Loading Visuals -> Ready: SceneEvent_Server (visual scene load completed)
Ready -> Interact: SwitchToDriving that triggers from UI
Interact -> QN: (Optional?) SwitchToQuestionnaire that triggers from UI
AnyState -> Waiting Room: trigger from UI
*/

namespace Core.Networking
{
    public class ConnectionAndSpawning : NetworkBehaviour
    {
        private IServerState _currentState;

        public NetworkVariable<EServerState> ServerStateEnum = new NetworkVariable<EServerState>(EServerState.Default,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField] private ConnectionAndSpawningSO _config;

        public SceneField WaitingRoomScene => _config.WaitingRoomScene;
        public List<SceneField> ScenarioScenes => _config.ScenarioScenes;

        public static ConnectionAndSpawning Instance { get; private set; }

        public ParticipantOrderMapping Participants = new ParticipantOrderMapping();
        public ParticipantOrder PO { get; private set; } = ParticipantOrder.None;

        private Dictionary<ParticipantOrder, ClientDisplay> POToClientDisplay = new Dictionary<ParticipantOrder, ClientDisplay>();
        private Dictionary<ParticipantOrder, InteractableObject> POToInteractableObjects = new Dictionary<ParticipantOrder, InteractableObject>();

        private void Awake()
        {
            if (Instance != null)
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

        private void Update()
        {
            if (NetworkManager.Singleton == null)
            {
                return;
            }

            if (NetworkManager.Singleton.IsServer)
            {
                if (_currentState != null)
                {
                    _currentState.UpdateState(this);
                }
            }

        }

        public void StartAsServer()
        {
            NetworkManager.Singleton.OnServerStarted += ServerStarted;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

            _currentState = new Default();
            _currentState.EnterState(this);

            NetworkManager.Singleton.StartServer();
        }

        public void StartAsClient()
        {
            NetworkManager.Singleton.StartClient();
        }

        public void StartAsClient(string ipAddress, ParticipantOrder po)
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                ipAddress,
                (ushort)7777
            );
            JoinParameters joinParams = new JoinParameters()
            {
                PO = po
            };

            var jsonString = JsonConvert.SerializeObject(joinParams);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(jsonString);

            NetworkManager.Singleton.StartClient();
        }

        public void StartAsHost(ParticipantOrder po)
        {
            NetworkManager.Singleton.OnServerStarted += ServerStarted;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

            JoinParameters joinParams = new JoinParameters()
            {
                PO = po
            };

            var jsonString = JsonConvert.SerializeObject(joinParams);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(jsonString);

            NetworkManager.Singleton.StartHost();
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            bool approve = false;

            JoinParameters joinParams = JsonConvert.DeserializeObject<JoinParameters>(Encoding.ASCII.GetString(request.Payload));

            approve = Participants.AddParticipant(joinParams.PO, request.ClientNetworkId);

            if (approve)
            {
                Debug.Log($"Approved connection from {request.ClientNetworkId} with PO {joinParams.PO}");
            }
            else
            {
                Debug.Log($"Rejected connection from {request.ClientNetworkId} with PO {joinParams.PO}!");
            }

            response.Approved = approve;
            response.CreatePlayerObject = false;
            response.Pending = false;
        }

        private void ServerStarted()
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent_Server;

            SwitchToState(new WaitingRoom());
        }

        private void ClientDisconnected(ulong clientId)
        {
            ParticipantOrder po = Participants.GetPO(clientId);

            if (POToClientDisplay.ContainsKey(po))
            {
                POToClientDisplay.Remove(po);
            }

            if (POToInteractableObjects.ContainsKey(po))
            {
                POToInteractableObjects.Remove(po);
            }

            Participants.RemoveParticipant(clientId);
            Debug.Log($"Client {clientId} (PO: {po}) disconnected. Cleaned up associated objects.");
        }

        private void SceneEvent_Server(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadEventCompleted:
                    LoadEventCompleted(sceneEvent);
                    break;
                case SceneEventType.LoadComplete:
                    if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                    {
                        ScenarioManager sm = GetScenarioManager();
                        if (sm == null || !sm.IsInitialized)
                        {
                            // Debug.LogWarning($"ScenarioManager not ready when server completed scene load: {sceneEvent.SceneName}. Waiting for it to initialize.");
                            StartCoroutine(WaitForScenarioManagerAndThenSpawn(sceneEvent));
                            return;
                        }

                        if (sceneEvent.LoadSceneMode == LoadSceneMode.Additive && sm.HasVisualScene() ||
                           (sceneEvent.LoadSceneMode == LoadSceneMode.Single && !sm.HasVisualScene()))
                        {
                            foreach (var connectedClientId in NetworkManager.Singleton.ConnectedClientsIds)
                            {
                                if (sceneEvent.ClientId == connectedClientId)
                                {
                                    ParticipantOrder po = Participants.GetPO(connectedClientId);
                                    if (po != ParticipantOrder.Researcher && !POToInteractableObjects.ContainsKey(po))
                                    {
                                        StartCoroutine(EnsureClientDisplayAndSpawnInteractable(connectedClientId, po));
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }


        private IEnumerator EnsureClientDisplayAndSpawnInteractable(ulong clientId, ParticipantOrder po)
        {
            yield return new WaitUntil(() => POToClientDisplay.ContainsKey(po) && POToClientDisplay[po] != null);
            SpawnInteractableObject(clientId);
        }


        private IEnumerator WaitForScenarioManagerAndThenSpawn(SceneEvent sceneEvent)
        {
            ScenarioManager sm = GetScenarioManager();
            while (sm == null || !sm.IsInitialized)
            {
                Debug.Log($"Waiting for ScenarioManager to initialize for scene: {sceneEvent.SceneName}...");
                yield return null;
                sm = GetScenarioManager();
            }
            Debug.Log($"ScenarioManager is now initialized for scene: {sceneEvent.SceneName}. Proceeding with LoadComplete logic.");

            if (sceneEvent.LoadSceneMode == LoadSceneMode.Additive && sm.HasVisualScene() ||
                (sceneEvent.LoadSceneMode == LoadSceneMode.Single && !sm.HasVisualScene()))
            {
                foreach (var connectedClientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (sceneEvent.ClientId == connectedClientId)
                    {
                        ParticipantOrder po = Participants.GetPO(connectedClientId);
                        if (po != ParticipantOrder.Researcher && !POToInteractableObjects.ContainsKey(po))
                        {
                            StartCoroutine(EnsureClientDisplayAndSpawnInteractable(connectedClientId, po));
                        }
                    }
                }
            }
        }


        private void LoadEventCompleted(SceneEvent sceneEvent)
        {
            // This event signifies that ALL clients (including server) have finished a Load or Unload operation.
            Debug.Log($"SceneEvent_Server: LoadEventCompleted for scene {sceneEvent.SceneName} and type {sceneEvent.SceneEventType}. Current state: {_currentState?.GetType().Name}");

            switch (_currentState)
            {
                case WaitingRoom:
                    SpawnResearcherPrefabs();
                    break;
                case LoadingScenario:
                    SwitchToState(new LoadingVisuals());
                    break;
                case LoadingVisuals:
                    SpawnResearcherPrefabs();
                    SwitchToState(new Ready());
                    break;
            }
        }

        private void ClientConnected(ulong clientId)
        {
            StartCoroutine(IEClientConnectedInternal(clientId));
        }

        private IEnumerator IEClientConnectedInternal(ulong clientId)
        {
            // Wait a frame to ensure scene loading has progressed
            yield return null;

            ScenarioManager sm = GetScenarioManager();
            while (sm == null)
            {
                yield return null;
                sm = GetScenarioManager();
            }

            while (!sm.IsInitialized)
            {
                yield return null;
            }
            Debug.Log($"ConnectionAndSpawning.IEClientConnectedInternal: ScenarioManager found and initialized for client {clientId}.");


            ParticipantOrder po = Participants.GetPO(clientId);

            if (po == ParticipantOrder.Researcher)
            {
                SpawnResearcherCamera(clientId);
            }
            else
            {
                Pose pose = sm.GetSpawnPose(po);
                GameObject clientDisplayPrefab = GetClientDisplayPrefab(po);
                if (clientDisplayPrefab == null)
                {
                    Debug.LogError($"Client Display Prefab is null for PO: {po}. Cannot spawn ClientDisplay.");
                    yield break;
                }

                GameObject clientInterfaceInstance = Instantiate(clientDisplayPrefab, pose.position, pose.rotation);
                NetworkObject netObj = clientInterfaceInstance.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    Debug.LogError($"ClientDisplay Prefab {clientDisplayPrefab.name} for PO: {po} is missing a NetworkObject component.");
                    Destroy(clientInterfaceInstance);
                    yield break;
                }

                netObj.SpawnAsPlayerObject(clientId);

                ClientDisplay ci = clientInterfaceInstance.GetComponent<ClientDisplay>();
                if (ci == null)
                {
                    Debug.LogError($"ClientDisplay Prefab {clientDisplayPrefab.name} for PO: {po} is missing a ClientDisplay component.");
                    if (netObj.IsSpawned) netObj.Despawn(true); else Destroy(clientInterfaceInstance);
                    yield break;
                }
                POToClientDisplay[po] = ci;
                ci.SetParticipantOrder(po);
                Debug.Log($"Spawned ClientDisplay for PO: {po} with ClientId: {clientId}");
            }
        }

        private void SpawnResearcherCamera(ulong clientId)
        {
            if (_config.ResearcherCameraPrefab == null)
            {
                Debug.LogError("ResearcherCameraPrefab is not assigned!");
                return;
            }

            ScenarioManager sm = GetScenarioManager();
            if (sm == null || !sm.IsInitialized)
            {
                Debug.LogError($"Cannot spawn researcher camera: ScenarioManager not ready for client {clientId}. This should not happen.");
                return;
            }

            Pose poseA = sm.GetSpawnPose(ParticipantOrder.A);
            Vector3 spawnPosition = poseA.position;
            Quaternion spawnRotation = poseA.rotation;

            SpawnResearcherCameraClientRpc(spawnPosition, spawnRotation, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
            Debug.Log($"Requested researcher camera spawn for client {clientId} at Pose A's position");
        }

        [ClientRpc]
        private void SpawnResearcherCameraClientRpc(Vector3 spawnPosition, Quaternion spawnRotation, ClientRpcParams clientRpcParams = default)
        {
            if (_config.ResearcherCameraPrefab == null)
            {
                Debug.LogError($"ResearcherCameraPrefab is null on client {NetworkManager.Singleton.LocalClientId}. Cannot spawn.");
                return;
            }

            ParticipantOrder po = Participants.GetPO(NetworkManager.Singleton.LocalClientId);
            if (po != ParticipantOrder.Researcher)
            {
                Debug.LogWarning($"Attempted to spawn researcher camera for non-researcher client {NetworkManager.Singleton.LocalClientId} (PO: {po}). Skipping spawn.");
                return;
            }

            if (GameObject.FindFirstObjectByType<FreeCameraController>() != null && NetworkManager.Singleton.LocalClientId == clientRpcParams.Send.TargetClientIds[0])
            {
                Debug.LogWarning($"Researcher camera already exists for client {NetworkManager.Singleton.LocalClientId}. Skipping spawn.");
                return;
            }

            GameObject researcherCameraInstance = Instantiate(_config.ResearcherCameraPrefab, spawnPosition, spawnRotation);
            Debug.Log($"Spawned researcher camera locally on client {NetworkManager.Singleton.LocalClientId}");
        }

        private void SpawnInteractableObject(ulong clientId)
        {
            StartCoroutine(IESpawnInteractableObject(clientId));
        }

        private IEnumerator IESpawnInteractableObject(ulong clientId)
        {
            ParticipantOrder po = Participants.GetPO(clientId);

            yield return new WaitUntil(() => POToClientDisplay.ContainsKey(po) && POToClientDisplay[po] != null);

            ScenarioManager sm = GetScenarioManager();
            if (sm == null || !sm.IsInitialized)
            {
                Debug.LogError($"Cannot spawn interactable object for PO {po} (Client {clientId}): ScenarioManager not ready. This should ideally be caught earlier.");
                yield break;
            }

            Pose pose = sm.GetSpawnPose(po);
            GameObject interactablePrefab = GetInteractableObjectPrefab(po);
            if (interactablePrefab == null)
            {
                Debug.LogError($"Interactable Object Prefab is null for PO: {po}. Cannot spawn.");
                yield break;
            }

            GameObject interactableInstance = Instantiate(interactablePrefab, pose.position, pose.rotation);
            NetworkObject netObj = interactableInstance.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError($"InteractableObject Prefab {interactablePrefab.name} for PO: {po} is missing a NetworkObject component.");
                Destroy(interactableInstance);
                yield break;
            }


            InteractableObject io = interactableInstance.GetComponent<InteractableObject>();
            if (io == null)
            {
                Debug.LogError($"InteractableObject Prefab {interactablePrefab.name} for PO: {po} is missing an InteractableObject component.");
                Destroy(interactableInstance);
                yield break;
            }

            netObj.SpawnWithOwnership(clientId);
            io.SetParticipantOrder(po);
            POToInteractableObjects[po] = io;

            yield return null;

            ClientDisplay clientDisplay = POToClientDisplay[po];
            if (clientDisplay != null && clientDisplay.NetworkObject.IsSpawned && io.NetworkObject.IsSpawned)
            {
                bool success = clientDisplay.AssignFollowTransform(io, clientId);
                if (!success) Debug.LogError($"Failed to assign follow transform for PO {po} to {io.name}.");
                else Debug.Log($"Assigned follow transform for PO {po} to {io.name}");
            }
            else
            {
                Debug.LogError($"Cannot assign follow transform for PO {po}: ClientDisplay or InteractableObject not ready/spawned.");
            }
        }

        public void SwitchToState(IServerState newState)
        {
            if (_currentState != null)
            {
                _currentState.ExitState(this);
            }

            _currentState = newState;

            string stateName = _currentState.GetType().Name;

            ServerStateEnum.Value = _currentState.State;

            _currentState.EnterState(this);
        }

        private void SpawnResearcherPrefabs()
        {
            Debug.Log("Spawning researcher prefabs (server-side or common)");

            foreach (GameObject prefab in _config.ResearcherPrefabs)
            {
                if (prefab == null)
                {
                    Debug.LogWarning("A prefab in _researcherPrefabs list is null. Skipping.");
                    continue;
                }
                GameObject instance = Instantiate(prefab);
                NetworkObject netObj = instance.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    if (!netObj.IsSpawned) netObj.Spawn(true);
                    Debug.Log($"Spawned researcher NetworkObject prefab: {prefab.name}");
                }
                else
                {
                    Debug.Log($"Instantiated local researcher prefab: {prefab.name}");
                }
            }
        }

        private GameObject GetClientDisplayPrefab(ParticipantOrder po)
        {
            ClientOption option = ClientOptions.Instance.GetOption(po);
            if (ClientDisplaysSO.Instance == null || option.ClientDisplay < 0 || option.ClientDisplay >= ClientDisplaysSO.Instance.ClientDisplays.Count)
            {
                Debug.LogError($"Invalid ClientDisplay option for PO: {po}. Index: {option.ClientDisplay}");
                return null;
            }
            var displaySO = ClientDisplaysSO.Instance.ClientDisplays[option.ClientDisplay];
            if (displaySO == null)
            {
                Debug.LogError($"ClientDisplaySO is null for PO: {po} at index {option.ClientDisplay}");
                return null;
            }
            return displaySO.Prefab;
        }

        private GameObject GetInteractableObjectPrefab(ParticipantOrder po)
        {
            ClientOption option = ClientOptions.Instance.GetOption(po);
            if (InteractableObjectsSO.Instance == null || option.InteractableObject < 0 || option.InteractableObject >= InteractableObjectsSO.Instance.InteractableObjects.Count)
            {
                Debug.LogError($"Invalid InteractableObject option for PO: {po}. Index: {option.InteractableObject}");
                return null;
            }
            var interactableSO = InteractableObjectsSO.Instance.InteractableObjects[option.InteractableObject];
            if (interactableSO == null)
            {
                Debug.LogError($"InteractableObjectSO is null for PO: {po} at index {option.InteractableObject}");
                return null;
            }
            return interactableSO.Prefab;
        }

        public void ServerLoadScene(string sceneName, LoadSceneMode mode)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("ServerLoadScene: sceneName is null or empty!");
                return;
            }
            Debug.Log($"ServerLoadingScene: {sceneName} with mode {mode}");
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, mode);
        }

        public ScenarioManager GetScenarioManager()
        {
            return FindFirstObjectByType<ScenarioManager>();
        }


        private void DestroyAllClientsInteractables()
        {
            List<ParticipantOrder> posToProcess = new List<ParticipantOrder>(POToInteractableObjects.Keys);

            foreach (var po in posToProcess)
            {
                if (po != ParticipantOrder.Researcher)
                {
                    DestroyInteractableObjectForPO(po);
                }
            }
            POToInteractableObjects.Clear();
        }

        private void DestroyInteractableObjectForPO(ParticipantOrder po)
        {
            if (POToInteractableObjects.TryGetValue(po, out InteractableObject io) && io != null)
            {
                NetworkObject netObj = io.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                    Debug.Log($"Despawned InteractableObject for PO: {po}");
                }
                else if (netObj == null)
                {
                    Destroy(io.gameObject);
                    Debug.LogWarning($"Destroyed InteractableObject (no NetworkObject) for PO: {po}");
                }
            }
        }


        public void SwitchToLoading(string scenarioName)
        {
            LoadingPrep();
            SwitchToState(new LoadingScenario(scenarioName));
        }


        [ContextMenu("SwitchToWaitingRoom")]
        public void BackToWaitingRoom()
        {
            LoadingPrep();
            SwitchToState(new WaitingRoom());
        }

        private void LoadingPrep()
        {
            if (StrangeLandLogger.Instance != null && StrangeLandLogger.Instance.isRecording())
            {
                StrangeLandLogger.Instance.StopRecording();
            }

            foreach (ParticipantOrder po in POToClientDisplay.Keys.ToList())
            {
                if (POToClientDisplay.TryGetValue(po, out ClientDisplay cd) && cd != null)
                {
                    if (POToInteractableObjects.TryGetValue(po, out InteractableObject io) && io != null)
                    {
                        if (cd.NetworkObject != null && io.NetworkObject != null)
                        {
                            cd.De_AssignFollowTransform(io.NetworkObject);
                            Debug.Log($"De-assigned follow transform for PO: {po}");
                        }
                    }
                }
            }
            DestroyAllClientsInteractables();
        }

        public void SwitchToInteract()
        {
            SwitchToState(new Interact());
        }

        public string GetServerState()
        {
            if (_currentState == null) return "State Undefined";
            return _currentState.ToString();
        }
    }
}
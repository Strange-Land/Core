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

        [SerializeField] private List<GameObject> _researcherPrefabs;
        [SerializeField] private GameObject _researcherCameraPrefab;

        public SceneField WaitingRoomScene;
        public List<SceneField> ScenarioScenes = new List<SceneField>();

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
        }

        private void SceneEvent_Server(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.LoadEventCompleted:
                    LoadEventCompleted(sceneEvent);
                    break;
                case SceneEventType.LoadComplete:
                    if (sceneEvent.ClientId == 0)
                    {
                        return;
                    }

                    if (sceneEvent.LoadSceneMode == LoadSceneMode.Additive && GetScenarioManager().HasVisualScene() ||
                        (sceneEvent.LoadSceneMode == LoadSceneMode.Single && !GetScenarioManager().HasVisualScene()))
                    {
                        ParticipantOrder po = Participants.GetPO(sceneEvent.ClientId);
                        if (po != ParticipantOrder.Researcher)
                        {
                            SpawnInteractableObject(sceneEvent.ClientId);
                        }
                    }
                    break;
            }
        }

        private void LoadEventCompleted(SceneEvent sceneEvent)
        {
            Debug.Log($"Scene load completed: {sceneEvent.SceneName}, current state: {_currentState}");

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
            yield return new WaitForEndOfFrame();

            ParticipantOrder po = Participants.GetPO(clientId);

            if (po == ParticipantOrder.Researcher)
            {
                SpawnResearcherCamera(clientId);
            }
            else
            {
                ScenarioManager sm = GetScenarioManager();
                Pose pose = sm.GetSpawnPose(po);
                GameObject clientInterfaceInstance = Instantiate(GetClientDisplayPrefab(po), pose.position, pose.rotation);

                clientInterfaceInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

                ClientDisplay ci = clientInterfaceInstance.GetComponent<ClientDisplay>();
                POToClientDisplay.Add(po, ci);
                ci.SetParticipantOrder(po);
            }
        }

        // for now researcher camera is just a local instance on the client, not networked
        private void SpawnResearcherCamera(ulong clientId)
        {
            if (_researcherCameraPrefab == null)
            {
                Debug.LogError("ResearcherCameraPrefab is not assigned!");
                return;
            }

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            ScenarioManager sm = GetScenarioManager();
            if (sm != null)
            {
                Pose researcherPose = sm.GetSpawnPose(ParticipantOrder.Researcher);
                spawnPosition = researcherPose.position;
                spawnRotation = researcherPose.rotation;
            }

            SpawnResearcherCameraClientRpc(spawnPosition, spawnRotation, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
        }

        [ClientRpc]
        private void SpawnResearcherCameraClientRpc(Vector3 spawnPosition, Quaternion spawnRotation, ClientRpcParams clientRpcParams = default)
        {
            if (_researcherCameraPrefab == null)
            {
                Debug.LogError("ResearcherCameraPrefab is not assigned!");
                return;
            }

            GameObject researcherCameraInstance = Instantiate(_researcherCameraPrefab, spawnPosition, spawnRotation);
            DontDestroyOnLoad(researcherCameraInstance);
            Debug.Log($"Spawned researcher camera locally on client {NetworkManager.Singleton.LocalClientId}");
        }

        private void SpawnInteractableObject(ulong clientId)
        {
            StartCoroutine(IESpawnInteractableObject(clientId));
        }

        private IEnumerator IESpawnInteractableObject(ulong clientId)
        {
            ParticipantOrder po = Participants.GetPO(clientId);
            yield return new WaitUntil(() => POToClientDisplay.ContainsKey(po));

            ScenarioManager sm = GetScenarioManager();
            Pose pose = sm.GetSpawnPose(po);
            GameObject interactableInstance = Instantiate(GetInteractableObjectPrefab(po), pose.position, pose.rotation);
            InteractableObject io = interactableInstance.GetComponent<InteractableObject>();
            ClientDisplay clientDisplay = POToClientDisplay[po];
            // io.OnSpawnComplete += (spawnedIO) =>
            // {
            //     Debug.Log($"[debug] Assigning follow transform to {spawnedIO.name} for client {clientId}");
            //     bool success = clientDisplay.AssignFollowTransform(spawnedIO, clientId);
            //     if (!success)
            //     {
            //         Debug.LogError($"[debug] Failed to assign follow transform to {spawnedIO.name} for client {clientId}");
            //     }
            //     else
            //     {
            //         Debug.Log($"[debug] Successfully assigned follow transform to {spawnedIO.name} for client {clientId}");
            //     }
            // };

            interactableInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

            io.SetParticipantOrder(po);
            POToInteractableObjects[po] = io;

            yield return new WaitForSeconds(0.01f);

            clientDisplay.AssignFollowTransform(io, clientId);

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
            Debug.Log("Spawning researcher prefabs");

            foreach (GameObject prefab in _researcherPrefabs)
            {
                Instantiate(prefab);
            }
        }

        private GameObject GetClientDisplayPrefab(ParticipantOrder po)
        {
            ClientOption option = ClientOptions.Instance.GetOption(po);
            return ClientDisplaysSO.Instance.ClientDisplays[option.ClientDisplay].Prefab;
        }

        private GameObject GetInteractableObjectPrefab(ParticipantOrder po)
        {
            ClientOption option = ClientOptions.Instance.GetOption(po);
            return InteractableObjectsSO.Instance.InteractableObjects[option.InteractableObject].Prefab;
        }

        public void ServerLoadScene(string sceneName, LoadSceneMode mode)
        {
            Debug.Log($"ServerLoadingScene: {sceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, mode);
        }

        public ScenarioManager GetScenarioManager()
        {
            return FindFirstObjectByType<ScenarioManager>();
        }


        private void DestroyAllClientsInteractables()
        {
            foreach (var po in Participants.GetAllConnectedPOs())
            {
                if (po != ParticipantOrder.Researcher)
                {
                    DestroyAllInteractableObjects(po);
                }
            }
        }

        private void DestroyAllInteractableObjects(ParticipantOrder po)
        {
            if (POToInteractableObjects.ContainsKey(po))
            {
                POToInteractableObjects[po].gameObject.GetComponent<NetworkObject>().Despawn(true);
                POToInteractableObjects.Remove(po);
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

            foreach (ParticipantOrder po in POToInteractableObjects.Keys.ToList())
            {
                if (POToClientDisplay.ContainsKey(po))
                {
                    foreach (InteractableObject io in POToInteractableObjects.Values)
                    {
                        POToClientDisplay[po].De_AssignFollowTransform(io.GetComponent<NetworkObject>());
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
            return _currentState.ToString();
        }
    }
}
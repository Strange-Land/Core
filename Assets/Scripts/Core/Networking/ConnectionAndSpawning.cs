using System.Collections;
using System.Collections.Generic;
using System.Text;
using Core.Scenario;
using Core.SceneEntities.NetworkedComponents.ClientInterface;
using Core.SceneEntities.NetworkedComponents.InteractableObject;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utilities.SceneField;

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

        public SceneField WaitingRoomScene;
        public List<SceneField> ScenarioScenes = new List<SceneField>();

        public static ConnectionAndSpawning Instance { get; private set; }

        public ParticipantOrderMapping Participants = new ParticipantOrderMapping();
        public ParticipantOrder PO { get; private set; } = ParticipantOrder.None;

        private Dictionary<ParticipantOrder, ClientDisplay> POToClientDisplay = new Dictionary<ParticipantOrder, ClientDisplay>();
        // change to one to one
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

            _currentState = new Default();
            _currentState.EnterState(this);

            NetworkManager.Singleton.StartServer();
        }

        public void StartAsClient()
        {
            NetworkManager.Singleton.StartClient();
        }

        public void StartAsHost()
        {
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

        private void SceneEvent_Server(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                // Trigger once on server scene load completed
                case SceneEventType.LoadEventCompleted:
                    LoadEventCompleted(sceneEvent);
                    break;
                // Trigger once on client scene load completed
                case SceneEventType.LoadComplete:
                    if (sceneEvent.ClientId == 0)
                    {
                        return;
                    }

                    if (sceneEvent.LoadSceneMode == LoadSceneMode.Additive && GetScenarioManager().HasVisualScene() ||
                        (sceneEvent.LoadSceneMode == LoadSceneMode.Single && !GetScenarioManager().HasVisualScene()))
                    {
                        SpawnInteractableObject(sceneEvent.ClientId);
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

            ScenarioManager sm = GetScenarioManager();

            ParticipantOrder po = Participants.GetPO(clientId);
            Pose pose = sm.GetSpawnPose(po);
            GameObject clientInterfaceInstance = Instantiate(GetClientDisplayPrefab(po), pose.position, pose.rotation);
            Debug.Log($"Spawned {clientInterfaceInstance.name} for PO {po}");

            clientInterfaceInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            ClientDisplay ci = clientInterfaceInstance.GetComponent<ClientDisplay>();
            POToClientDisplay.Add(po, ci);
            ci.SetParticipantOrder(po);
        }

        private void SpawnInteractableObject(ulong clientId)
        {
            StartCoroutine(IESpawnInteractableObject(clientId));
        }

        private IEnumerator IESpawnInteractableObject(ulong clientId)
        {
            yield return new WaitUntil(() => POToClientDisplay.ContainsKey(Participants.GetPO(clientId)));

            ParticipantOrder po = Participants.GetPO(clientId);

            ScenarioManager sm = GetScenarioManager();
            Pose pose = sm.GetSpawnPose(po);
            GameObject interactableInstance = Instantiate(GetInteractableObjectPrefab(po), pose.position, pose.rotation);
            Debug.Log($"Spawned {interactableInstance.name} for PO {po}");

            // different depending on SO config
            interactableInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);

            InteractableObject io = interactableInstance.GetComponent<InteractableObject>();
            io.SetParticipantOrder(po);
            POToInteractableObjects[po] = io;

            ClientDisplay cientDisplay = POToClientDisplay[po];
            cientDisplay.AssignFollowTransform(io, clientId);
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

        // expose simple APIs for other classes to trigger
        public void SwitchToLoading(string scenarioName)
        {
            foreach (ParticipantOrder po in POToInteractableObjects.Keys)
            {
                foreach (InteractableObject io in POToInteractableObjects.Values)
                {
                    POToClientDisplay[po].De_AssignFollowTransform(io.GetComponent<NetworkObject>());
                }
            }

            DestroyAllClientsInteractables();
            SwitchToState(new LoadingScenario(scenarioName));
        }

        private void DestroyAllClientsInteractables()
        {
            foreach (var po in Participants.GetAllConnectedPOs())
            {
                DestroyAllInteractableObjects(po);
            }
        }

        private void DestroyAllInteractableObjects(ParticipantOrder po)
        {
            POToInteractableObjects[po].gameObject.GetComponent<NetworkObject>().Despawn(true);
        }

        [ContextMenu("SwitchToWaitingRoom")]
        public void BackToWaitingRoom()
        {
            DestroyAllClientsInteractables();
            SwitchToState(new WaitingRoom());
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


using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    /*
     Ahhhhhh so here is the thing. It makes a ton of sense to use Interface for the state machine
     But in networked context is it not the easiest to sync states between server and clients
     So I'm reluctantlly putting enum here, matching the server state and sync with clients
     
     Actively investigating more elegant solutions
     */
    public enum EServerState
    {
        Default,
        WaitingRoom,
        LoadingScenario,
        LoadingVisuals,
        Ready,
        Interact,
        Questions,
        PostQuestions
    }
    
    public interface IServerState
    {
        void EnterState(ConnectionAndSpawning context);
        
        void UpdateState(ConnectionAndSpawning context);
        
        void ExitState(ConnectionAndSpawning context);
    }
    
    /* I was inclining to use abstract class + virtual methods
     to provide default implementation (like logging),
     but I realized I will need to do stuff like "base.enter" in every override, 
     so it's not making code neater. So we just go with plain interface*/
    
    public class Default : IServerState
    {
        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("DefaultState: Enter state");
        }

        public void UpdateState(ConnectionAndSpawning context) { }
        
        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("DefaultState: Exit state");
        }
    }

    
    public class WaitingRoom : IServerState
    {
        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("WaitingRoomState: Enter state");
        
            context.ServerLoadScene(ConnectionAndSpawning.Instance.WaitingRoomScene.SceneName);
        }

        public void UpdateState(ConnectionAndSpawning context) { }
    
        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("WaitingRoomState: Exit state");
        }
    }
    
    public class LoadingScenario : IServerState
    {
        private readonly string _scenarioName;
    
        public LoadingScenario(string scenarioName)
        {
            _scenarioName = scenarioName;
        }

        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log($"Loading scenario: {_scenarioName}");

            context.ServerLoadScene(_scenarioName);

            Debug.Log("Destroying old interactable objects, preparing new scenario...");
        }

        public void UpdateState(ConnectionAndSpawning context) { }
        
        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("LoadingScenarioState: Exit state");
        }
    }

    public class LoadingVisuals : IServerState
    {
        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("LoadingVisualsState: Enter state");

            var scenarioManager = context.GetScenarioManager();
            if (scenarioManager == null)
            {
                Debug.LogWarning("ScenarioManager is null. Cannot load visuals scene.");
                return;
            }

            if (!scenarioManager.HasVisualScene())
            {
                Debug.LogWarning("No visual scene to load.");
                return;
            }

            var sceneName = scenarioManager.GetVisualSceneName();

            if (sceneName == "") return;
            
            Debug.Log($"Loading visuals scene: {sceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        public void UpdateState(ConnectionAndSpawning context)
        {
        }

        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("LoadingVisualsState: Exit state");
        }
    }

    public class Ready : IServerState
    {
        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("ReadyState: Enter state");
        }

        public void UpdateState(ConnectionAndSpawning context)
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.I))
            {
                context.SwitchToState(new Interact());
            }
        }
        
        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("ReadyState: Exit state");
        }
    }
    
    public class Interact : IServerState
    {
        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("InteractState: Enter state");
        }

        public void UpdateState(ConnectionAndSpawning context)
        { }
        
        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("InteractState: Exit state");
        }
    }
    
    public class Questions : IServerState
    {
        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("QuestionsState: Enter state");
        }

        public void UpdateState(ConnectionAndSpawning context)
        {
            Debug.Log("QuestionsState: checking if all participants are done...");
        
            // If all QNFinished = true => context.SwitchToState(new PostQuestionsState());
        }
        
        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("QuestionsState: Exit state");
        }
    }

}

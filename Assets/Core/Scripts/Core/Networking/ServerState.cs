using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Networking
{
    /*
     Ahhhhhh so here is the thing. It makes a ton of sense to use Interface for the state machine
     But in networked context is it not the easiest to sync states between server and clients
     So I'm reluctantly putting enum here, matching the server state and sync with clients
     
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
        public EServerState State { get; }

        void EnterState(ConnectionAndSpawning context);

        void UpdateState(ConnectionAndSpawning context);

        void ExitState(ConnectionAndSpawning context);
    }

    public class Default : IServerState
    {
        public EServerState State => EServerState.Default;

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
        public EServerState State => EServerState.WaitingRoom;

        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("WaitingRoomState: Enter state");

            context.ServerLoadScene(ConnectionAndSpawning.Instance.WaitingRoomScene.SceneName, LoadSceneMode.Single);
        }

        public void UpdateState(ConnectionAndSpawning context) { }

        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("WaitingRoomState: Exit state");
        }
    }

    public class LoadingScenario : IServerState
    {
        public EServerState State => EServerState.LoadingScenario;

        private readonly string _scenarioName;

        public LoadingScenario(string scenarioName)
        {
            _scenarioName = scenarioName;
        }

        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log($"Loading scenario: {_scenarioName}");

            context.ServerLoadScene(_scenarioName, LoadSceneMode.Single);

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
        public EServerState State => EServerState.LoadingVisuals;

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

            Debug.Log($"Loading visuals scene: {sceneName}");

            if (sceneName == "") return;

            Debug.Log($"Loading visuals  scene: {sceneName}");
            context.ServerLoadScene(sceneName, LoadSceneMode.Additive);
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
        public EServerState State => EServerState.Ready;

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
        public EServerState State => EServerState.Interact;

        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("InteractState: Enter state");
        }

        public void UpdateState(ConnectionAndSpawning context)
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Q))
            {
                context.SwitchToState(new Questions());
            }
        }

        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("InteractState: Exit state");
        }
    }

    public class Questions : IServerState
    {
        public EServerState State => EServerState.Questions;

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

    public class PostQuestions : IServerState
    {
        public EServerState State => EServerState.PostQuestions;

        public void EnterState(ConnectionAndSpawning context)
        {
            Debug.Log("PostQuestionsState: Enter state");
        }

        public void UpdateState(ConnectionAndSpawning context)
        {
        }

        public void ExitState(ConnectionAndSpawning context)
        {
            Debug.Log("PostQuestionsState: Exit state");

            // context.SwitchToState(new WaitingRoom());
        }
    }

}

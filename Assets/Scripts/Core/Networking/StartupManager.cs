using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.Playmode;
using UnityEngine;

namespace Core.Networking
{
    public class StartupManager : MonoBehaviour
    {
        [SerializeField] private GameObject _researcherStartupPrefab;
        [SerializeField] private GameObject _participantStartupPrefab;

        [SerializeField] private List<RuntimePlatform> _serverPlatforms;
        [SerializeField] private List<RuntimePlatform> _clientPlatforms;

        [Tooltip("Use Platform for builds, use PlayModeTag for editor (multiplayer center)")]
        [SerializeField] private StartupMode _startupMode;
        private string[] _playModeTags;
        
        private enum StartupMode
        {
            Platform,
            PlayModeTag
        }
        
        private void Awake()
        {
            _playModeTags = CurrentPlayer.ReadOnlyTags();
            
            switch (_startupMode)
            {
                case StartupMode.Platform:
                    PlatformStartup();
                    break;
                case StartupMode.PlayModeTag:
                    TagStartup();
                    break;
            }
        }
        
        private void TagStartup()
        {
            if (_playModeTags.Contains("Researcher"))
            {
                StartResearcherStartup();
            }
            else if (_playModeTags.Contains("Participant"))
            {
                StartParticipantStartup();
            }
            else
            {
                Debug.LogError("Play mode tag not supported");
            }
        }
        
        private void PlatformStartup()
        {
            if (_serverPlatforms.Contains(Application.platform))
            {
                StartResearcherStartup();
            }
            else if (_clientPlatforms.Contains(Application.platform))
            {
                StartParticipantStartup();
            }
            else
            {
                Debug.LogError("Platform not supported");
            }
        }
        
        private void StartResearcherStartup()
        {
            Instantiate(_researcherStartupPrefab);
            Destroy(this);
        }
        
        private void StartParticipantStartup()
        {
            Instantiate(_participantStartupPrefab);
            Destroy(this);
        }
    }
}

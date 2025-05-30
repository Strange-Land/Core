using System.Collections.Generic;
using System.Linq;
using Unity.Multiplayer.Playmode;
using UnityEngine;

namespace Core.Networking
{
    public class StartupManager : MonoBehaviour
    {
        [SerializeField] private GameObject _PCStartupPrefab;
        [SerializeField] private GameObject _VRStartupPrefab;

        [SerializeField] private List<RuntimePlatform> _PCPlatforms;
        [SerializeField] private List<RuntimePlatform> _VRPlatforms;

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
            // use tags if in editor, use platform if in build

            if (Application.isEditor)
            {
                _startupMode = StartupMode.PlayModeTag;
            }
            else
            {
                _startupMode = StartupMode.Platform;
            }

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
            _playModeTags = CurrentPlayer.ReadOnlyTags();
            if (_playModeTags.Contains("PC"))
            {
                StartPCStartup();
            }
            else if (_playModeTags.Contains("VR"))
            {
                StartVRStartup();
            }
            else
            {
                Debug.LogError("Play mode tag not supported! Please go to the multiplayer center and add the tags");
            }
        }

        private void PlatformStartup()
        {
            if (_PCPlatforms.Contains(Application.platform))
            {
                StartPCStartup();
            }
            else if (_VRPlatforms.Contains(Application.platform))
            {
                StartVRStartup();
            }
            else
            {
                Debug.LogError("Platform not supported");
            }
        }

        private void StartPCStartup()
        {
            Instantiate(_PCStartupPrefab);
            Destroy(this);
        }

        private void StartVRStartup()
        {
            Instantiate(_VRStartupPrefab);
            Destroy(this);
        }
    }
}

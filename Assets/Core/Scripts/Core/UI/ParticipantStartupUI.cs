using System.Linq;
using System.Text;
using Core.Networking;
using Newtonsoft.Json;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Core.UI
{
    public class ParticipantStartupUI : MonoBehaviour
    {
        public TMP_Dropdown PODropdown;
        public TMP_Dropdown LanguageDropdown;
        
        private JoinParameters _joinParameters;
        
        private void Start()
        {
            PopulateLanguageDropdown();
        }
        
        private void PopulateLanguageDropdown()
        {
            LanguageDropdown.ClearOptions();
    
            var languages = System.Enum.GetNames(typeof(Language));
            LanguageDropdown.AddOptions(languages.ToList());
        }
        
        private void UpdateJoinParameters()
        {
            _joinParameters = new JoinParameters
            {
                PO = (ParticipantOrder) PODropdown.value,
            };
        }
        
        [ContextMenu("StartClient")]
        public void StartClient()
        {
            UpdateJoinParameters();
            
            var jsonString = JsonConvert.SerializeObject(_joinParameters);
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(jsonString);
            
            ConnectionAndSpawning.Instance.StartAsClient();
        }
    }
}

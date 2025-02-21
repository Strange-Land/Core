using System.Collections.Generic;

namespace Core.Networking
{
    public class ClientOptions
    {
        private static ClientOptions _instance;

        public static ClientOptions Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClientOptions();
                }
                return _instance;
            }
            internal set => _instance = value;
        }

        public List<ClientOption> Options = new List<ClientOption>();

        public ClientOption GetOption(ParticipantOrder po)
        {
            return Options.Find(x => x.PO == po);
        }
    
        private ClientOptions()
        {
            for (int i = 0; i < 6; i++)
            {
                ClientOption co = new ClientOption();
                co.PO = (ParticipantOrder)i; 
                co.ClientDisplay = 0;
                co.InteractableObject = 0;
                Options.Add(co);
            }
        }
    }
}
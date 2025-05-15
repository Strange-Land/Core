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

        public List<ClientOption> Options => GlobalConfig.GetClientOptions();

        public ClientOption GetOption(ParticipantOrder po)
        {
            return GlobalConfig.GetClientOption(po);
        }
    }
}
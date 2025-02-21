using System.Collections.Generic;
using Utilities;

namespace Core.SceneEntities.NetworkedComponents.ClientInterface
{
    public class ClientDisplaysSO : SingletonSO<ClientDisplaysSO>
    {
        public List<ClientDisplaySO> ClientDisplays = new List<ClientDisplaySO>();
    }
}

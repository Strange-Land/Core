using System.Collections.Generic;
using Core.Utilities;

namespace Core.SceneEntities
{
    public class ClientDisplaysSO : SingletonSO<ClientDisplaysSO>
    {
        public List<ClientDisplaySO> ClientDisplays = new List<ClientDisplaySO>();
    }
}

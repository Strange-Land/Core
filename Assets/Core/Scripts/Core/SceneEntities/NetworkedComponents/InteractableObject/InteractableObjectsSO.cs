using System.Collections.Generic;
using Utilities;

namespace Core.SceneEntities.NetworkedComponents
{
    public class InteractableObjectsSO : SingletonSO<InteractableObjectsSO>
    {
        public List<InteractableObjectSO> InteractableObjects = new List<InteractableObjectSO>();
    }
}


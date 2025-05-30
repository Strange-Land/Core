using System.Collections.Generic;
using Core.Utilities;

namespace Core.SceneEntities
{
    public class InteractableObjectsSO : SingletonSO<InteractableObjectsSO>
    {
        public List<InteractableObjectSO> InteractableObjects = new List<InteractableObjectSO>();
    }
}


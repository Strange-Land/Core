using System;
using Unity.Netcode;
using UnityEngine;

namespace Core.SceneEntities.NetworkedComponents
{
    public class OneScreenDisplay : ClientDisplay
    {
        public override void AssignFollowTransform(InteractableObject MyInteractableObject, ulong targetClient)
        {
            NetworkObject netobj = MyInteractableObject.NetworkObject;

            transform.position = MyInteractableObject.GetCameraPositionObject().position;
            transform.rotation = MyInteractableObject.GetCameraPositionObject().rotation;

            NetworkObject.TrySetParent(netobj, true);

        }

        public override InteractableObject GetFollowTransform()
        {
            throw new NotImplementedException();
        }

        public override Transform GetMainCamera()
        {
            throw new NotImplementedException();
        }

        public override void CalibrateClient(Action<bool> calibrationFinishedCallback)
        {
            throw new NotImplementedException();
        }

        public override void GoForPostQuestion()
        {
            throw new NotImplementedException();
        }
    }
}

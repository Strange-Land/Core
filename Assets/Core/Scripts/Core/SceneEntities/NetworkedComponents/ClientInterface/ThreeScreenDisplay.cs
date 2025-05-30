using System;
using Unity.Netcode;
using UnityEngine;

namespace Core.SceneEntities.NetworkedComponents
{
    public class ThreeScreenDisplay : ClientDisplay
    {
        public override bool AssignFollowTransform(InteractableObject MyInteractableObject, ulong targetClient)
        {
            NetworkObject netobj = MyInteractableObject.NetworkObject;

            transform.position = MyInteractableObject.GetCameraPositionObject().position;
            transform.rotation = MyInteractableObject.GetCameraPositionObject().rotation;

            bool success = NetworkObject.TrySetParent(netobj, true);
            return success;
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

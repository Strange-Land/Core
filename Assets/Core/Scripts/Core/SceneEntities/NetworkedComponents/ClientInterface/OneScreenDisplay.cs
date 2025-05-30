using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;


namespace Core.SceneEntities
{
    public class OneScreenDisplay : ClientDisplay
    {
        public override bool AssignFollowTransform(InteractableObject _interactableObject, ulong targetClient)
        {
            NetworkObject netobj = _interactableObject.NetworkObject;

            transform.position = _interactableObject.GetCameraPositionObject().position;
            transform.rotation = _interactableObject.GetCameraPositionObject().rotation;

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

using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Core.SceneEntities.NetworkedComponents
{
    public class OneScreenDisplay : ClientDisplay
    {
        public override void AssignFollowTransform(InteractableObject interactableObject, ulong targetClient)
        {
            StartCoroutine(SetParentCoroutine(interactableObject));
        }

        private IEnumerator SetParentCoroutine(InteractableObject interactableObject)
        {
            yield return new WaitForSeconds(0.1f);

            NetworkObject netobj = interactableObject.NetworkObject;

            transform.position = interactableObject.GetCameraPositionObject().position;
            transform.rotation = interactableObject.GetCameraPositionObject().rotation;

            bool success = NetworkObject.TrySetParent(netobj, true);
            if (!success)
            {
                Debug.LogError("Failed to set parent for OneScreenDisplay");
            }
            else
            {
                Debug.Log("Successfully set parent for OneScreenDisplay");
            }

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

using System;
using System.Collections.Generic;
using Core.Networking;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.SceneEntities
{
    public abstract class ClientDisplay : NetworkBehaviour
    {

        private NetworkVariable<ParticipantOrder> _participantOrder = new NetworkVariable<ParticipantOrder>();

        public InteractableObject MyInteractableObject;

        private static List<ClientDisplay> instances = new List<ClientDisplay>(); // Can cause memory leakage if not kept clean...!!! 
        public static IReadOnlyList<ClientDisplay> Instances => instances.AsReadOnly();

        private void Awake()
        {
            instances.Add(this);
        }

        public override void OnDestroy()
        {
            instances.Remove(this);
        }

        public override void OnNetworkSpawn()
        {
            /* I know we originally do it in inherited classes
        but at least for camera and audio listener they should be generic 
        the inherited classes just need to remember do base.OnNetworkSpawn()
        */
            if (!IsLocalPlayer)
            {
                foreach (var c in GetComponentsInChildren<Camera>())
                {
                    c.enabled = false;
                }

                foreach (var a in GetComponentsInChildren<AudioListener>())
                {
                    a.enabled = false;
                }

                foreach (var e in GetComponentsInChildren<EventSystem>())
                {
                    e.enabled = false;
                }

            }
            else
            {
                Debug.Log("ClientInterface OnNetworkSpawn");
            }
        }

        public void SetParticipantOrder(ParticipantOrder _ParticipantOrder)
        {
            _participantOrder.Value = _ParticipantOrder;
        }
        public ParticipantOrder GetParticipantOrder()
        {
            return _participantOrder.Value;
        }

        public abstract bool AssignFollowTransform(InteractableObject _interactableObject, ulong targetClient);
        public abstract InteractableObject GetFollowTransform();

        public virtual void De_AssignFollowTransform(NetworkObject netobj)
        {
            if (IsServer)
            {
                NetworkObject.TryRemoveParent(true);
                MyInteractableObject = null;
                De_AssignFollowTransformClientRPC();
                Debug.Log("De_AssignFollowTransform");
            }
        }


        [ClientRpc]
        public virtual void De_AssignFollowTransformClientRPC()
        {
            MyInteractableObject = null;
        }

        public abstract Transform GetMainCamera();

        //David: Once finished callibration return a True to the provided function. onm the server we then know callibration was succesfful. false for callibration failed.
        public abstract void CalibrateClient(Action<bool> calibrationFinishedCallback);

        public abstract void GoForPostQuestion();
    }
}
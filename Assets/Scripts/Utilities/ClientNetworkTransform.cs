using Unity.Netcode.Components;
using UnityEngine;


    /// <summary>
    /// Used for syncing a transform with client side changes. This includes host. Pure server as owner isn't supported by this. Please use NetworkTransform
    /// for transforms that'll always be owned by the server.
    ///
    /// If you want to modify this Script please copy it into your own project and add it to your Player Prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// Used to determine who can write to this transform. Owner client only.
        /// This imposes state to the server. This is putting trust on your clients.
        /// Make sure no security-sensitive features use this transform.
        /// </summary>
        /// <returns>True if server-authoritative, False otherwise.</returns>
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }

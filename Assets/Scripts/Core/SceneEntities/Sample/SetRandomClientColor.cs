using Unity.Netcode;
using UnityEngine;

namespace Core.SceneEntities.Sample
{

/// <summary>
/// This class sets a randcom Client Color on startup and spawing to distinquish clinets.
/// Its mostly used for debugging network issues. 
/// </summary>
[RequireComponent(typeof(Renderer))]
public class SetRandomClientColor : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetColorBasedOnOwner();
    }

    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        SetColorBasedOnOwner();
    }

    /// <summary>
    /// Setting Color.
    /// </summary>
    void SetColorBasedOnOwner()
    {
        Random.InitState((int) OwnerClientId);
        GetComponent<Renderer>().material.color = Random.ColorHSV();
    }
}
    
}
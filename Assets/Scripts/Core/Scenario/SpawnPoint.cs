using Core.Networking;
using UnityEngine;

namespace Core.Scenario
{
    public class SpawnPoint : MonoBehaviour
    {
        public ParticipantOrder PO;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, .25f);
        
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
        }
    }
}

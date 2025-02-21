using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.SceneEntities.Sample
{
    public class SC_SimpleWASDControl : NetworkBehaviour
    {
        public float Speed = 5;
    
        private void Update()
        {
            if (!IsOwner || !IsSpawned) return;

            var multiplier = Speed * Time.deltaTime;

            if (Keyboard.current.aKey.isPressed)
            {
                transform.position += new Vector3(-multiplier, 0, 0);
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                transform.position += new Vector3(multiplier, 0, 0);
            }
            else if (Keyboard.current.wKey.isPressed)
            {
                transform.position += new Vector3(0, 0, multiplier);
            }
            else if (Keyboard.current.sKey.isPressed)
            {
                transform.position += new Vector3(0, 0, -multiplier);
            }
        }
    }
}

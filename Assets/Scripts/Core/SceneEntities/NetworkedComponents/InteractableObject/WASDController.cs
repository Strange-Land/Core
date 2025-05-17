using System;
using Core.Networking;
using UnityEngine;

namespace Core.SceneEntities.NetworkedComponents
{
    [RequireComponent(typeof(Rigidbody))]
    public class WASDController : InteractableObject
    {
        public Transform CameraAnchor;
        private Rigidbody rb;

        public float moveSpeed = 3f;
        public float jumpForce = 5f;
        public float rotationSpeed = 150f;
        public float maxGroundAngle = 45f;

        private bool isGrounded = false;

        public override void SetStartingPose(Pose _pose)
        {
            throw new NotImplementedException();
        }

        public override void AssignClient(ulong CLID_, ParticipantOrder _participantOrder_)
        {
            throw new NotImplementedException();
        }

        public override Transform GetCameraPositionObject()
        {
            return CameraAnchor;
        }

        public override void Stop_Action()
        {
            throw new NotImplementedException();
        }

        public override bool HasActionStopped()
        {
            throw new NotImplementedException();
        }

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (ConnectionAndSpawning.Instance.ServerStateEnum.Value != EServerState.Interact) return;

            HandleRotation();
            HandleMovement();
        }

        private void FixedUpdate()
        {
            CheckGroundStatus();
        }

        private void HandleRotation()
        {
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X");
                transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime);
            }
        }

        private void HandleMovement()
        {
            float horizontalInput = 0f;
            float verticalInput = 0f;

            if (Input.GetKey(KeyCode.W)) verticalInput = 1f;
            if (Input.GetKey(KeyCode.S)) verticalInput = -1f;
            if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
            if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;

            Vector3 moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;

            if (isGrounded)
            {
                Vector3 velocity = moveDirection * moveSpeed;
                velocity.y = rb.linearVelocity.y;
                rb.linearVelocity = velocity;

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Jump();
                }
            }
        }

        private void Jump()
        {
            if (isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        private void CheckGroundStatus()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.1f))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                isGrounded = angle <= maxGroundAngle;
            }
            else
            {
                isGrounded = false;
            }
        }
    }
}

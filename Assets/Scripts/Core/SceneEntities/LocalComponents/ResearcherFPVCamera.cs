using UnityEngine;

namespace Core.SceneEntities
{
    public class FreeCameraController : MonoBehaviour
    {
        public float moveSpeed = 10f;
        public float speedMultiplier = 2f;

        public float rotationSpeed = 5f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        public float zoomSpeed = 10f;
        public float zoomMultiplier = 2f;
        public float minFOV = 20f;
        public float maxFOV = 90f;

        float _yaw;
        float _pitch;

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        void Update()
        {
            Move();
            Look();
            Zoom();
        }

        void Move()
        {
            float currentSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift)) currentSpeed *= speedMultiplier;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 direction = (right * h + forward * v).normalized;
            Vector3 move = direction * currentSpeed * Time.deltaTime;

            float up = 0f;
            if (Input.GetKey(KeyCode.Space)) up += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) up -= 1f;
            move += Vector3.up * up * currentSpeed * Time.deltaTime;

            transform.position += move;
        }

        void Look()
        {
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

                _yaw += mouseX;
                _pitch -= mouseY;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

                transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
            }
        }

        void Zoom()
        {
            float currentZoomSpeed = zoomSpeed;
            if (Input.GetKey(KeyCode.LeftShift)) currentZoomSpeed *= zoomMultiplier;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Camera cam = GetComponent<Camera>();
                if (cam)
                {
                    float fov = cam.fieldOfView;
                    fov -= scroll * currentZoomSpeed;
                    fov = Mathf.Clamp(fov, minFOV, maxFOV);
                    cam.fieldOfView = fov;
                }
            }
        }
    }
}

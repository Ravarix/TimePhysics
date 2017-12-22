using UnityEngine;

namespace Demo
{
    public class DemoFlyer : MonoBehaviour
    {
        public float HorizontalMouseSensitivity = 90f;
        public float VerticalMouseSensitivity = 75f;
        public float MoveSpeed = 10f;

        //Lazy cache transform
        private Transform _transform;
        public Transform Transform => _transform ?? (_transform = transform);

        public struct DemoInput
        {
            public bool Forward;
            public bool Backward;
            public bool Left;
            public bool Right;
            public bool Up;
            public bool Down;
            public float Yaw;
            public float Pitch;
        }

        private void Awake()
        {
            if(Cursor.visible)
                ToggleCursor();
        }
        
        private void Update()
        {
            if(Input.GetKeyUp(KeyCode.LeftShift))
                ToggleCursor();
            
            var input = new DemoInput {
                Forward = Input.GetKey(KeyCode.W),
                Backward = Input.GetKey(KeyCode.S),
                Left = Input.GetKey(KeyCode.A),
                Right = Input.GetKey(KeyCode.D),
                Up = Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space),
                Down = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftControl),
                Yaw = Input.GetAxis("Mouse X"),
                Pitch = -Input.GetAxis("Mouse Y")
            };

            if (!Cursor.visible)
            {
                Transform.Rotate(Vector3.up, HorizontalMouseSensitivity * Time.deltaTime * input.Yaw, Space.World);
                Transform.Rotate(Vector3.right, VerticalMouseSensitivity * Time.deltaTime * input.Pitch);   
            }

            Vector3 delta = Vector3.zero;
            if (input.Forward ^ input.Backward) 
                delta += input.Forward ? Transform.forward : -Transform.forward;
            if (input.Right ^ input.Left)
                delta += input.Right ? Transform.right : -Transform.right;
            if (input.Up ^ input.Down)
                delta += input.Up ? Transform.up : -Transform.up;

            Transform.position += MoveSpeed * Time.deltaTime * delta;
        }
        
        public void ToggleCursor()
        {
            if (Cursor.visible) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            } else {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

    }
}
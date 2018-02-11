using UnityEngine;

namespace Demo
{
    public enum MovementType { Flight, CharacterController };

    public class DemoMover : MonoBehaviour
    {
        public float HorizontalMouseSensitivity = 90f;
        public float VerticalMouseSensitivity = 75f;
        public float MoveSpeed = 10f;
        public MovementType MovementType = MovementType.CharacterController;

        //Lazy cache transform
        private Transform _transform;
        public Transform Trans => _transform ?? (_transform = transform);

        private CharacterController _cc;
        public CharacterController CharacterController => _cc ?? (_cc = GetComponent<CharacterController>());

        private struct DemoInput
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
                Trans.Rotate(Vector3.up, HorizontalMouseSensitivity * Time.deltaTime * input.Yaw, Space.World);
                Trans.Rotate(Vector3.right, VerticalMouseSensitivity * Time.deltaTime * input.Pitch);   
            }

            Vector3 delta = Vector3.zero;
            if (input.Forward ^ input.Backward) 
                delta += input.Forward ? Trans.forward : -Trans.forward;
            if (input.Right ^ input.Left)
                delta += input.Right ? Trans.right : -Trans.right;
            if (input.Up ^ input.Down)
                delta += input.Up ? Trans.up : -Trans.up;
            
            if (MovementType == MovementType.CharacterController)
                CharacterController?.Move(MoveSpeed * Time.deltaTime * delta.normalized);
            else
                Trans.position += MoveSpeed * Time.deltaTime * delta.normalized;
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
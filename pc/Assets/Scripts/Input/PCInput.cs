using UnityEngine;

namespace GranivelCity
{
    public class PCInput : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool ShootHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool AimHeld { get; private set; }
        public bool ReloadPressed { get; private set; }
        public bool InventoryPressed { get; private set; }
        public bool PausePressed { get; private set; }

        [SerializeField] private float mouseSensitivity = 2.0f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Move = Vector2.ClampMagnitude(Move, 1f);
            LookDelta = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y")) * mouseSensitivity;
            ShootHeld = Input.GetMouseButton(0);
            AimHeld = Input.GetMouseButton(1);
            InteractPressed = Input.GetKeyDown(KeyCode.E);
            JumpPressed = Input.GetKeyDown(KeyCode.Space);
            SprintHeld = Input.GetKey(KeyCode.LeftShift);
            ReloadPressed = Input.GetKeyDown(KeyCode.R);
            InventoryPressed = Input.GetKeyDown(KeyCode.Tab);
            PausePressed = Input.GetKeyDown(KeyCode.Escape);

            if (PausePressed)
            {
                bool unlock = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = unlock ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = unlock;
            }
        }
    }
}

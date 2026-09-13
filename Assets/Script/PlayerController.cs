using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 10f;
    public float jumpHeight = 1.3f;
    public float gravity = -20f;

    [Header("Look Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 85f;

    [Header("State")]
    public bool canMove = true;
    public bool isSprinting { get; private set; }

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
        }
        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = new Vector3(0, 1f, 0);

        SetupCamera();
    }

    void Start()
    {
        LockCursor();
    }

    public void SetupCamera()
    {
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null && Camera.main != null)
            {
                cam = Camera.main;
            }

            if (cam != null)
            {
                cameraTransform = cam.transform;
                cameraTransform.SetParent(transform);
                cameraTransform.localPosition = new Vector3(0, 1.6f, 0); // Eye level
                cameraTransform.localRotation = Quaternion.identity;

                // Remove legacy follow scripts if attached
                var oldFollow = cam.GetComponent("CameraFollow");
                if (oldFollow != null)
                {
                    Destroy(oldFollow);
                }
            }
        }
    }

    void Update()
    {
        // Click to lock cursor when clicking in game view
        if (InputBridge.GetFire() && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }

        // Toggle cursor lock with Escape
        if (InputBridge.GetEscapeDown())
        {
            ToggleCursorLock();
        }

        if (!canMove) return;

        HandleLook();
        HandleMovement();
    }

    void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 mouseDelta = InputBridge.GetMouseDelta() * mouseSensitivity;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float moveX = InputBridge.GetHorizontal();
        float moveZ = InputBridge.GetVertical();

        isSprinting = InputBridge.GetSprint() && moveZ > 0.1f;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (InputBridge.GetJumpDown() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            UnlockCursor();
        }
        else
        {
            LockCursor();
        }
    }
}
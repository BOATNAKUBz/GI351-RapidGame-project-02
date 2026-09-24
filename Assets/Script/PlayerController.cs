using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6.5f;
    public float sprintSpeed = 10.5f;
    public float jumpHeight = 1.3f;
    public float gravity = -20f;

    [Header("Dash Settings")]
    public float dashDistance = 10f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.5f;

    [Header("Look Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 85f;

    [Header("State")]
    public bool canMove = true;
    public bool isDashing { get; private set; }
    public float dashCooldownRemaining { get; private set; }

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private float lastDashTime = -999f;

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
                cam.nearClipPlane = 0.03f; // Prevent first-person weapon model from clipping

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
        // คำนวณเวลา Cooldown สำหรับใช้งาน UI
        dashCooldownRemaining = Mathf.Max(0f, (lastDashTime + dashCooldown) - Time.time);

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

        // ระหว่าง Dash จะข้ามการเคลื่อนที่ปกติ
        if (!isDashing)
        {
            HandleDashInput();
            HandleMovement();
        }
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

        // Support both New Input System and legacy Input Manager reliably
        float moveX = InputBridge.GetHorizontal();
        float moveZ = InputBridge.GetVertical();
        if (Mathf.Approximately(moveX, 0f) && Mathf.Approximately(moveZ, 0f))
        {
            try
            {
                moveX = Input.GetAxisRaw("Horizontal");
                moveZ = Input.GetAxisRaw("Vertical");
            }
            catch { }
        }

        Vector3 moveInput = transform.right * moveX + transform.forward * moveZ;
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        // Sprinting when holding Sprint (Left Shift)
        float currentSpeed = walkSpeed;
        if (InputBridge.GetSprint() && isGrounded && moveInput.sqrMagnitude > 0.01f)
        {
            currentSpeed = Mathf.Max(sprintSpeed, walkSpeed * 1.5f);
        }

        Vector3 moveVelocity = moveInput * currentSpeed;

        if (InputBridge.GetJumpDown() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = moveVelocity + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }

    private void HandleDashInput()
    {
        // Dash with Q key or SprintDown (Shift) with cooldown
        if ((Input.GetKeyDown(KeyCode.Q) || InputBridge.GetDashDown()) && dashCooldownRemaining <= 0f)
        {
            StartCoroutine(PerformDash());
        }
    }

    private IEnumerator PerformDash()
    {
        isDashing = true;
        lastDashTime = Time.time;

        float moveX = InputBridge.GetHorizontal();
        float moveZ = InputBridge.GetVertical();
        Vector3 dashDir = (transform.right * moveX + transform.forward * moveZ).normalized;

        if (dashDir == Vector3.zero)
        {
            dashDir = transform.forward;
        }

        float dashSpeed = dashDistance / dashDuration;
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            controller.Move(dashDir * dashSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
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
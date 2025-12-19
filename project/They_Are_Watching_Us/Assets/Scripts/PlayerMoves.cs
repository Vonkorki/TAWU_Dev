using UnityEngine;

[System.Serializable]
public class CameraSettings
{
    [Header("Position")]
    [Range(5f, 50f)] public float height = 15f;
    [Range(30f, 85f)] public float angle = 60f;
    [Range(3f, 15f)] public float followSpeed = 8f;
    
    [Header("Zoom")]
    [Range(1f, 10f)] public float minHeight = 3f;
    [Range(15f, 100f)] public float maxHeight = 30f;
    [Range(5f, 30f)] public float zoomSpeed = 15f;
    [Range(1f, 10f)] public float zoomSmoothness = 5f;
    
    [Header("Input")]
    public string zoomAxis = "Mouse ScrollWheel";
    public bool invertZoom = false;
    public KeyCode resetZoomKey = KeyCode.R;

}

[System.Serializable]
public class MovementSettings
{
    [Header("Movement")]
    [Range(1f, 20f)] public float moveSpeed = 8f;
    [Range(100f, 2000f)] public float rotationSpeed = 720f;
    [Range(5f, 50f)] public float acceleration = 15f;
    [Range(5f, 50f)] public float deceleration = 10f;

    [Header("Advanced")]
    public bool usePhysics = false;
    public float gravity = -9.81f;
    public LayerMask groundMask = 1;
}

public class PlayerMoves: MonoBehaviour
{
    // Serialized fields
    [SerializeField] private CameraSettings cameraSettings = new CameraSettings();
    [SerializeField] private MovementSettings movementSettings = new MovementSettings();
    
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraPivot;
    
    // Private variables
    private CharacterController characterController;
    private Vector3 currentVelocity;
    private Vector3 moveInput;
    private float currentCameraHeight;
    private float cameraDistance;
    private Vector3 cameraOffset;
    private Vector3 cameraVelocity;
    private float verticalVelocity;
    private bool isGrounded;
    //hash
    private int idleHash;
    private int walkHash;
    
    // Properties
    public float CurrentZoomPercentage => Mathf.InverseLerp(
        cameraSettings.minHeight, 
        cameraSettings.maxHeight, 
        currentCameraHeight
    );
    void Start()
    {
        // animator = GetComponent<Animator>();
        idleHash = Animator.StringToHash("idle");
        walkHash = Animator.StringToHash("walk");
        playerCamera = Camera.main;
    }
    void Awake()
    {
        InitializeComponents();
        InitializeCamera();
    }
    
    void InitializeComponents()
    {
        // Get or add CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0, 1f, 0);
        }
        
        // Find camera if not assigned
        if (playerCamera == null)
            playerCamera = Spawn.mainCamera;
        
        // Create camera pivot if needed
        if (cameraPivot == null)
        {
            cameraPivot = new GameObject("CameraPivot").transform;
            cameraPivot.position = transform.position;
        }
    }
    
    void InitializeCamera()
    {
        currentCameraHeight = cameraSettings.height;
        CalculateCameraDistance();
        
        // Set initial camera position and rotation
        cameraOffset = new Vector3(0, currentCameraHeight, -cameraDistance);
        
        if (playerCamera != null)
        {
            playerCamera.transform.position = transform.position + cameraOffset;
            playerCamera.transform.rotation = Quaternion.Euler(cameraSettings.angle, 0f, 0f);
        }
        
        // Setup cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        HandleInput();
        HandleZoom();
        
        // Toggle cursor with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? 
                CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = !Cursor.visible;
        }
    }
    
    void FixedUpdate()
    {
        if (movementSettings.usePhysics)
        {
            HandlePhysicsMovement();
        }
    }
    
    void LateUpdate()
    {
        if (!movementSettings.usePhysics)
        {
            HandleCharacterControllerMovement();
        }
        
        UpdateCamera();
    }
    
    void HandleInput()
    {
        // Get WASD input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Create normalized input vector
        moveInput = new Vector3(horizontal, 0f, vertical);
        if (moveInput.magnitude > 1f)
            moveInput.Normalize();
    }
    
    void HandleCharacterControllerMovement()
    {
        Vector3 targetVelocity = Vector3.zero;
        
        if (moveInput.magnitude > 0.1f)
        {
            animator.SetTrigger(walkHash);
            // Get camera-relative movement
            Vector3 cameraForward = playerCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            Vector3 cameraRight = playerCamera.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();
            
            // Calculate movement direction relative to camera
            Vector3 moveDirection = cameraForward * moveInput.z + cameraRight * moveInput.x;
            moveDirection.Normalize();
            
            targetVelocity = moveDirection * movementSettings.moveSpeed;
            
            // Acceleration
            currentVelocity = Vector3.Lerp(
                currentVelocity, 
                targetVelocity, 
                movementSettings.acceleration * Time.deltaTime
            );
            
            // Rotate player towards movement
            RotatePlayer(moveDirection);
        }
        else
        {
            animator.SetTrigger(idleHash);
            // Deceleration when no input
            currentVelocity = Vector3.Lerp(
                currentVelocity, 
                Vector3.zero, 
                movementSettings.deceleration * Time.deltaTime
            );
        }
        
        // Apply gravity
        if (!characterController.isGrounded)
        {
            verticalVelocity += movementSettings.gravity * Time.deltaTime;
        }
        else
        {
            verticalVelocity = -0.5f; // Small value to keep grounded
        }
        
        // Final movement
        Vector3 finalMovement = new Vector3(
            currentVelocity.x, 
            verticalVelocity, 
            currentVelocity.z
        );

        characterController.Move(finalMovement * Time.deltaTime);
        
        // Ground check
        isGrounded = characterController.isGrounded;
    }
    
    void HandlePhysicsMovement()
    {
        // Alternative physics-based movement (if needed)
        // Note: Requires Rigidbody component
    }
    
    void RotatePlayer(Vector3 direction)
    {
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                movementSettings.rotationSpeed * Time.deltaTime
            );
        }
    }
    
    void HandleZoom()
    {
        // Get zoom input
        float scroll = Input.GetAxis(cameraSettings.zoomAxis);
        
        // Invert if needed
        if (cameraSettings.invertZoom)
            scroll = -scroll;
        
        // Additional zoom keys
        if (Input.GetKey(KeyCode.E))
            scroll = 0.1f;
        else if (Input.GetKey(KeyCode.Q))
            scroll = -0.1f;
        
        // Apply zoom
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentCameraHeight -= scroll * cameraSettings.zoomSpeed;
            currentCameraHeight = Mathf.Clamp(
                currentCameraHeight,
                cameraSettings.minHeight,
                cameraSettings.maxHeight
            );
            
            // Smooth interpolation
            cameraSettings.height = Mathf.Lerp(
                cameraSettings.height,
                currentCameraHeight,
                cameraSettings.zoomSmoothness * Time.deltaTime
            );
        }
        
        // Reset zoom
        if (Input.GetKeyDown(cameraSettings.resetZoomKey))
        {
            currentCameraHeight = 15f;
            cameraSettings.height = 15f;
        }
    }
    
    void CalculateCameraDistance()
    {
        // Calculate horizontal distance based on height and angle
        // Formula: distance = height / tan(angle)
        float angleRad = cameraSettings.angle * Mathf.Deg2Rad;
        cameraDistance = cameraSettings.height / Mathf.Tan(angleRad);
    }
    
    void UpdateCamera()
    {
        if (playerCamera == null) return;
        
        // Recalculate distance if height changed
        CalculateCameraDistance();
        
        // Update camera offset
        cameraOffset = new Vector3(0, cameraSettings.height, -cameraDistance);
        
        // Calculate target position (with optional look-ahead)
        Vector3 targetPosition = transform.position + cameraOffset;
        
        // Optional: Add look-ahead based on velocity
        if (currentVelocity.magnitude > 1f)
        {
            Vector3 lookAhead = currentVelocity.normalized * 2f;
            targetPosition += lookAhead;
        }
        
        // Smooth camera follow
        playerCamera.transform.position = Vector3.SmoothDamp(
            playerCamera.transform.position,
            targetPosition,
            ref cameraVelocity,
            0.2f, // Smooth time
            cameraSettings.followSpeed
        );
        
        // Fixed camera angle - NO ROTATION CHANGES DURING ZOOM
        playerCamera.transform.rotation = Quaternion.Euler(cameraSettings.angle, 0f, 0f);
    }
    
    #region Public Methods
    
    public void SetZoom(float percentage)
    {
        percentage = Mathf.Clamp01(percentage);
        currentCameraHeight = Mathf.Lerp(
            cameraSettings.minHeight, 
            cameraSettings.maxHeight, 
            percentage
        );
    }
    
    public void SetCameraAngle(float newAngle)
    {
        cameraSettings.angle = Mathf.Clamp(newAngle, 30f, 85f);
    }
    
    public void SetMoveSpeed(float speed)
    {
        movementSettings.moveSpeed = Mathf.Max(1f, speed);
    }
    
    public void Teleport(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
    }
    
    #endregion
    
    #region Debug & Gizmos
    
    void OnDrawGizmosSelected()
    {
        // Draw movement direction
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up, currentVelocity.normalized * 2f);
        }
        
        // Draw camera frustum preview
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCamera.transform.position, 0.5f);
            
            // Draw camera angle
            Vector3 camPos = playerCamera.transform.position;
            Vector3 playerPos = transform.position;
            Gizmos.DrawLine(camPos, playerPos);
            
            // Draw ground projection
            Vector3 groundPoint = new Vector3(playerPos.x, 0, playerPos.z);
            Gizmos.DrawLine(playerPos, groundPoint);
        }
    }
    
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        // Display debug info
        float yPos = 10f;
        float lineHeight = 25f;
        
        GUI.Label(new Rect(10, yPos, 300, 25), $"Speed: {currentVelocity.magnitude:F1}", style);
        yPos += lineHeight;
        
        GUI.Label(new Rect(10, yPos, 300, 25), $"Zoom: {CurrentZoomPercentage * 100:F0}%", style);
        yPos += lineHeight;
        
        GUI.Label(new Rect(10, yPos, 300, 25), $"Height: {cameraSettings.height:F1}", style);
        yPos += lineHeight;
        
        GUI.Label(new Rect(10, yPos, 300, 25), $"Grounded: {isGrounded}", style);
        yPos += lineHeight;
        
        // Controls help
        GUI.Label(new Rect(10, Screen.height - 120, 300, 25), "CONTROLS:", style);
        GUI.Label(new Rect(10, Screen.height - 95, 300, 25), "WASD - Move", style);
        GUI.Label(new Rect(10, Screen.height - 70, 300, 25), "Mouse Scroll - Zoom", style);
        GUI.Label(new Rect(10, Screen.height - 45, 300, 25), "R - Reset Zoom", style);
        GUI.Label(new Rect(10, Screen.height - 20, 300, 25), "ESC - Toggle Cursor", style);
    }
    
    #endregion
}
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class Player2DController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the player moves left/right")]
    [Range(1f, 20f)]
    public float moveSpeed = 5f;

    [Header("Jump Settings")]
    public bool canJump = true;

    [Tooltip("How high the player jumps")]
    [Range(1f, 30f)]
    public float jumpForce = 10f;

    [Tooltip("Grace period after leaving a ledge where you can still jump")]
    [Range(0f, 0.3f)]
    private float coyoteTime = 0.1f;

    [Tooltip("Time window to press jump before landing and it will execute")]
    [Range(0f, 0.3f)]
    private float jumpBufferTime = 0.1f;

    [Header("Ground Detection")]
    [Tooltip("How far below the player to check for ground")]
    [Range(0.01f, 1f)]
    public float groundCheckDistance = 0.1f;

    [Tooltip("Which layers count as ground")]
    public LayerMask groundLayer = 1;

    [Header("Physics Settings")]
    [Tooltip("How heavy the player is")]
    [Range(0.1f, 10f)]
    public float mass = 1f;

    [Tooltip("How fast the player falls (higher = falls faster)")]
    [Range(0f, 10f)]
    public float gravityScale = 3f;

    [Tooltip("Air resistance")]
    [Range(0f, 10f)]
    public float linearDrag = 0f;

    [Header("Visual Settings")]
    [Tooltip("Flip sprite when moving left/right")]
    public bool flipSpriteOnDirection = true;

    [Foldout("Debug")]
    public bool showGroundCheck = true;

    [Foldout("Debug")]
    public bool showColliderBounds = true;

    [Foldout("Debug")]
    public bool debugJumpInfo = false;

    [Foldout("UI References")]
    public Button leftButton;

    [Foldout("UI References")]
    public Button rightButton;

    [Foldout("UI References")]
    public Button jumpButton;


    // Private variables
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool holdingLeft;
    private bool holdingRight;
    private float horizontalInput;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    private Vector3 spawnPoint;

    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Auto-add collider if missing
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider2D>();
            AutoFitColliderToSprite();
            Debug.Log("Auto-added CapsuleCollider2D and fitted to sprite!");
        }

        lastJumpPressedTime = -1f;
        lastGroundedTime = -1f;
        spawnPoint = transform.position;

        SetupPhysics();
    }

    void Start()
    {
        SetupUI();
    }

    void Update()
    {
        HandleInput();
        CheckGrounded();
        UpdateJumpTimers();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleJump();
    }

    void SetupPhysics()
    {
        if (rb == null) return;

        rb.mass = mass;
        rb.gravityScale = gravityScale;
        rb.drag = linearDrag;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void AutoFitColliderToSprite()
    {
        if (capsuleCollider == null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        Bounds bounds = spriteRenderer.sprite.bounds;
        capsuleCollider.size = new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.95f);
        capsuleCollider.offset = new Vector2(0f, bounds.center.y);
        capsuleCollider.direction = CapsuleDirection2D.Vertical;
    }

    void SetupUI()
    {
        // Setup left button
        if (leftButton != null)
        {
            var leftTrigger = leftButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (leftTrigger == null) leftTrigger = leftButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            leftTrigger.triggers.Clear();

            var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { holdingLeft = true; });
            leftTrigger.triggers.Add(pointerDown);

            var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { holdingLeft = false; });
            leftTrigger.triggers.Add(pointerUp);
        }

        // Setup right button
        if (rightButton != null)
        {
            var rightTrigger = rightButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (rightTrigger == null) rightTrigger = rightButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            rightTrigger.triggers.Clear();

            var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { holdingRight = true; });
            rightTrigger.triggers.Add(pointerDown);

            var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { holdingRight = false; });
            rightTrigger.triggers.Add(pointerUp);
        }

        // Setup jump button
        if (jumpButton != null && canJump)
        {
            jumpButton.onClick.RemoveAllListeners();
            jumpButton.onClick.AddListener(OnJumpButtonPressed);
        }
    }

    void OnJumpButtonPressed()
    {
        lastJumpPressedTime = Time.time;
        if (debugJumpInfo) Debug.Log("Jump button pressed!");
    }

    void HandleInput()
    {
        // Keyboard input
        float keyboardInput = 0f;
        if (Input.GetKey(KeyCode.A)) keyboardInput -= 1f;
        if (Input.GetKey(KeyCode.D)) keyboardInput += 1f;

        // UI button input
        float uiInput = 0f;
        if (holdingLeft) uiInput -= 1f;
        if (holdingRight) uiInput += 1f;

        // Combine inputs
        horizontalInput = Mathf.Clamp(keyboardInput + uiInput, -1f, 1f);

        // Jump input
        if (canJump && Input.GetKeyDown(KeyCode.Space))
        {
            lastJumpPressedTime = Time.time;
            if (debugJumpInfo) Debug.Log("Space pressed!");
        }
    }

    void UpdateJumpTimers()
    {
        if (!canJump) return;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
    }

    void HandleMovement()
    {
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        if (flipSpriteOnDirection && spriteRenderer != null && horizontalInput != 0)
        {
            spriteRenderer.flipX = horizontalInput < 0;
        }
    }

    void HandleJump()
    {
        if (!canJump) return;

        float timeSinceGrounded = Time.time - lastGroundedTime;
        float timeSinceJumpPressed = Time.time - lastJumpPressedTime;

        bool withinCoyoteTime = timeSinceGrounded <= coyoteTime;
        bool withinJumpBuffer = timeSinceJumpPressed <= jumpBufferTime;
        bool jumpRequested = lastJumpPressedTime > 0 && withinJumpBuffer;

        if (debugJumpInfo && jumpRequested)
        {
            Debug.Log($"Jump Check: Coyote={withinCoyoteTime}, Buffer={withinJumpBuffer}");
        }

        if (withinCoyoteTime && jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            lastJumpPressedTime = -1f;
            lastGroundedTime = -1f;

            if (debugJumpInfo)
            {
                Debug.Log("JUMP EXECUTED!");
            }
        }
    }

    void CheckGrounded()
    {
        if (!canJump || capsuleCollider == null) return;

        wasGroundedLastFrame = isGrounded;

        Vector2 position = transform.TransformPoint(capsuleCollider.offset);
        float scaleY = Mathf.Abs(transform.localScale.y);
        float scaleX = Mathf.Abs(transform.localScale.x);

        float colliderHeight = capsuleCollider.size.y;
        float colliderWidth = capsuleCollider.size.x;
        float castOriginOffset = 0.05f;
        float distance = (colliderHeight * scaleY * 0.5f) - castOriginOffset + groundCheckDistance;

        RaycastHit2D hit = Physics2D.CapsuleCast(
            position,
            new Vector2(colliderWidth * scaleX * 0.9f, 0.1f),
            CapsuleDirection2D.Vertical,
            0f,
            Vector2.down,
            distance,
            groundLayer
        );

        isGrounded = hit.collider != null;

        if (isGrounded != wasGroundedLastFrame && debugJumpInfo)
        {
            if (isGrounded)
            {
                Debug.Log("LANDED - Player is now grounded");
            }
            else
            {
                Debug.Log("LEFT GROUND - Player is now airborne");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider2D>();
        if (capsuleCollider == null) return;

        float colliderWidth = capsuleCollider.size.x;
        float colliderHeight = capsuleCollider.size.y;

        // Draw collider bounds
        if (showColliderBounds)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector2 pos = transform.TransformPoint(capsuleCollider.offset);
            float scaleY = Mathf.Abs(transform.localScale.y);
            float scaleX = Mathf.Abs(transform.localScale.x);

            float radius = colliderWidth * scaleX * 0.5f;
            float height = colliderHeight * scaleY;

            Gizmos.DrawWireSphere(pos + Vector2.up * (height * 0.5f - radius), radius);
            Gizmos.DrawWireSphere(pos + Vector2.down * (height * 0.5f - radius), radius);

            Vector3 topLeft = pos + new Vector2(-radius, height * 0.5f - radius);
            Vector3 topRight = pos + new Vector2(radius, height * 0.5f - radius);
            Vector3 bottomLeft = pos + new Vector2(-radius, -(height * 0.5f - radius));
            Vector3 bottomRight = pos + new Vector2(radius, -(height * 0.5f - radius));

            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(topRight, bottomRight);
        }

        // Draw ground check
        if (showGroundCheck && canJump)
        {
            Gizmos.color = (Application.isPlaying && isGrounded) ? Color.green : Color.red;
            Vector2 position = transform.TransformPoint(capsuleCollider.offset);
            float scaleY = Mathf.Abs(transform.localScale.y);
            float scaleX = Mathf.Abs(transform.localScale.x);

            float distance = (colliderHeight * scaleY * 0.5f) + groundCheckDistance;

            Vector2 size = new Vector2(colliderWidth * scaleX * 0.9f, 0.1f);
            Vector2 bottomPos = position + Vector2.down * distance;

            Gizmos.DrawWireCube(bottomPos, size);
            Gizmos.DrawLine(position + Vector2.left * size.x * 0.5f, bottomPos + Vector2.left * size.x * 0.5f);
            Gizmos.DrawLine(position + Vector2.right * size.x * 0.5f, bottomPos + Vector2.right * size.x * 0.5f);
        }
    }

    // ----- RESPAWN SYSTEM -----
    public void SetSpawnPoint(Vector3 newSpawnPoint)
    {
        spawnPoint = newSpawnPoint;
        Debug.Log($"Spawn Point updated to: {spawnPoint}");
    }

    public void Respawn()
    {
        transform.position = spawnPoint;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        Debug.Log("Player Respawned!");
    }

    // Helper method to update physics at runtime
    void OnValidate()
    {
        if (Application.isPlaying && rb != null)
        {
            SetupPhysics();
        }
    }
}
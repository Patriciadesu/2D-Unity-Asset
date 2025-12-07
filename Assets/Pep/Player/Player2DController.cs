using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

[ExecuteAlways]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class Player2DController : MonoBehaviour
{
    [BoxGroup("Movement Settings")]
    [Label("Move Speed")]
    [Range(1f, 20f)]
    [Tooltip("How fast the player moves left/right")]
    public float moveSpeed = 5f;

    [BoxGroup("Jump Settings")]
    [Label("Enable Jumping")]
    public bool canJump = true;

    [BoxGroup("Jump Settings")]
    [ShowIf("canJump")]
    [Label("Jump Force")]
    [Range(1f, 30f)]
    [Tooltip("How high the player jumps (higher = jump higher)")]
    public float jumpForce = 10f;

    [BoxGroup("Jump Settings")]
    [ShowIf("canJump")]
    [InfoBox("These settings make jumping feel more responsive and forgiving!", EInfoBoxType.Normal)]
    [Label("Coyote Time (Ledge Grace)")]
    [Range(0f, 0.3f)]
    [Tooltip("COYOTE TIME: Lets you jump for a brief moment AFTER walking off a ledge. Makes platforming more forgiving!\n\n0.15s = Very forgiving (recommended for beginners)\n0.1s = Balanced (default)\n0.05s = Tight (for experienced players)\n0s = Disabled")]
    public float coyoteTime = 0.1f;

    [BoxGroup("Jump Settings")]
    [ShowIf("canJump")]
    [Label("Jump Buffer Time (Early Jump)")]
    [Range(0f, 0.3f)]
    [Tooltip("JUMP BUFFER: Lets you press jump slightly BEFORE landing and it will execute on landing. Prevents missed jumps!\n\n0.15s = Very forgiving\n0.1s = Balanced (default)\n0.05s = Tight\n0s = Disabled")]
    public float jumpBufferTime = 0.1f;

    [BoxGroup("Ground Detection")]
    [ShowIf("canJump")]
    [Label("Ground Check Distance")]
    [Range(0.01f, 1f)]
    [Tooltip("How far below the player to check for ground. Increase if player doesn't detect ground properly.")]
    public float groundCheckDistance = 0.1f;

    [BoxGroup("Ground Detection")]
    [ShowIf("canJump")]
    [Label("Ground Layer")]
    [Tooltip("Which layers count as 'ground'. Usually set to 'Default' or 'Ground' layer.")]
    public LayerMask groundLayer = 1;

    [BoxGroup("Physics Settings")]
    [InfoBox("These control how the player feels when moving and falling", EInfoBoxType.Normal)]
    [Label("Player Mass")]
    [Range(0.1f, 10f)]
    [Tooltip("How heavy the player is. Higher = harder to push around by physics.")]
    public float mass = 1f;

    [BoxGroup("Physics Settings")]
    [Label("Gravity Scale")]
    [Range(0f, 10f)]
    [Tooltip("How fast the player falls. Higher = falls faster (feels heavier). 3 is good for most games.")]
    public float gravityScale = 3f;

    [BoxGroup("Physics Settings")]
    [Label("Linear Drag")]
    [Range(0f, 10f)]
    [Tooltip("Air resistance. Higher = slows down faster when not moving. Usually keep at 0.")]
    public float linearDrag = 0f;

    [BoxGroup("Collider Settings")]
    [InfoBox("CapsuleCollider2D is BEST for characters - smooth movement, no getting stuck!", EInfoBoxType.Normal)]
    [Label("Collider Width")]
    [Range(0.1f, 5f)]
    [Tooltip("How wide the collision capsule is")]
    public float colliderWidth = 1f;

    [BoxGroup("Collider Settings")]
    [Label("Collider Height")]
    [Range(0.1f, 5f)]
    [Tooltip("How tall the collision capsule is")]
    public float colliderHeight = 2f;

    [BoxGroup("Collider Settings")]
    [Label("Collider Offset Y")]
    [Range(-2f, 2f)]
    [Tooltip("Move collider up/down relative to player position")]
    public float colliderOffsetY = 0f;

    [BoxGroup("UI References")]
    [Label("Left Button")]
    [Required]
    public Button leftButton;

    [BoxGroup("UI References")]
    [Label("Right Button")]
    [Required]
    public Button rightButton;

    [BoxGroup("UI References")]
    [ShowIf("canJump")]
    [Label("Jump Button")]
    public Button jumpButton;

    [BoxGroup("Visual Settings")]
    [Label("Flip Sprite Direction")]
    [Tooltip("Should the sprite flip when moving left/right?")]
    public bool flipSpriteOnDirection = true;

    [BoxGroup("Debug")]
    [Label("Show Ground Check")]
    [Tooltip("Visualize ground detection in Scene view (green = grounded, red = in air)")]
    public bool showGroundCheck = true;

    [BoxGroup("Debug")]
    [Label("Show Collider Bounds")]
    [Tooltip("Visualize the collision capsule in Scene view")]
    public bool showColliderBounds = true;

    [BoxGroup("Debug")]
    [ShowIf("canJump")]
    [Label("Show Jump Info")]
    [Tooltip("Print jump details to console")]
    public bool debugJumpInfo = false;

    // Private variables
    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool holdingLeft;
    private bool holdingRight;
    private float horizontalInput;

    // Jump control variables
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    // Respawn variables
    private Vector3 spawnPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Initialize to prevent first frame issues
        lastJumpPressedTime = -1f;
        lastGroundedTime = -1f;
        spawnPoint = transform.position;
    }

    void OnEnable()
    {
        SetupPhysics();
        SetupCollider();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            SetupUI();
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            HandleInput();
            CheckGrounded();
            UpdateJumpTimers();
        }
        else
        {
            // Update in Edit Mode for instant visual feedback
            SetupPhysics();
            SetupCollider();
        }
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying) return;

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

    void SetupCollider()
    {
        if (capsuleCollider == null) return;

        capsuleCollider.size = new Vector2(colliderWidth, colliderHeight);
        capsuleCollider.offset = new Vector2(0f, colliderOffsetY);
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
        if (debugJumpInfo) Debug.Log("🎮 Jump button pressed!");
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

        // Jump input - record the time when jump was pressed
        if (canJump && Input.GetKeyDown(KeyCode.Space))
        {
            lastJumpPressedTime = Time.time;
            if (debugJumpInfo) Debug.Log("⌨️ Space pressed!");
        }
    }

    void UpdateJumpTimers()
    {
        if (!canJump) return;

        // Update grounded time
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
    }

    void HandleMovement()
    {
        // Apply horizontal movement
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        // Flip sprite based on direction
        if (flipSpriteOnDirection && spriteRenderer != null && horizontalInput != 0)
        {
            spriteRenderer.flipX = horizontalInput < 0;
        }
    }

    void HandleJump()
    {
        if (!canJump) return;

        // Check if we can jump using coyote time and jump buffer
        float timeSinceGrounded = Time.time - lastGroundedTime;
        float timeSinceJumpPressed = Time.time - lastJumpPressedTime;

        bool withinCoyoteTime = timeSinceGrounded <= coyoteTime;
        bool withinJumpBuffer = timeSinceJumpPressed <= jumpBufferTime;
        bool jumpRequested = lastJumpPressedTime > 0 && withinJumpBuffer;

        if (debugJumpInfo && jumpRequested)
        {
            Debug.Log($"📊 Jump Check: Coyote={withinCoyoteTime}({timeSinceGrounded:F3}s), Buffer={withinJumpBuffer}({timeSinceJumpPressed:F3}s)");
        }

        // Jump if within coyote time and jump was pressed recently
        if (withinCoyoteTime && jumpRequested)
        {
            // Perform jump
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Consume the jump press to prevent multiple jumps
            lastJumpPressedTime = -1f;
            lastGroundedTime = -1f;

            if (debugJumpInfo)
            {
                Debug.Log("✨ JUMP EXECUTED!");
            }
        }
    }

    void CheckGrounded()
    {
        if (!canJump) return;

        wasGroundedLastFrame = isGrounded;

        // Account for scale
        Vector2 position = transform.TransformPoint(capsuleCollider.offset);
        float scaleY = Mathf.Abs(transform.localScale.y);
        float scaleX = Mathf.Abs(transform.localScale.x);
        
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

        // Log state changes
        if (isGrounded != wasGroundedLastFrame && debugJumpInfo)
        {
            if (isGrounded)
            {
                Debug.Log("⬇️ LANDED - Player is now grounded");
            }
            else
            {
                Debug.Log("⬆️ LEFT GROUND - Player is now airborne");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider2D>();

        // Draw collider bounds
        if (showColliderBounds && capsuleCollider != null)
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
        if (showGroundCheck && canJump && capsuleCollider != null)
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

    [Button("🔄 Reset to Default Settings")]
    private void ResetSettings()
    {
        moveSpeed = 5f;
        canJump = true;
        jumpForce = 10f;
        coyoteTime = 0.1f;
        jumpBufferTime = 0.1f;
        groundCheckDistance = 0.1f;
        mass = 1f;
        gravityScale = 3f;
        linearDrag = 0f;
        colliderWidth = 1f;
        colliderHeight = 2f;
        colliderOffsetY = 0f;
        flipSpriteOnDirection = true;
        showGroundCheck = true;
        showColliderBounds = true;

        SetupPhysics();
        SetupCollider();
        Debug.Log("✓ All settings reset to defaults!");
    }

    [Button("Auto-Fit Collider to Sprite", EButtonEnableMode.Editor)]
    private void AutoFitCollider()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Bounds bounds = spriteRenderer.sprite.bounds;
            colliderWidth = bounds.size.x * 0.9f; // Slightly smaller for better feel
            colliderHeight = bounds.size.y * 0.95f;
            colliderOffsetY = bounds.center.y;
            SetupCollider();
            Debug.Log("✓ Collider fitted to sprite! Adjusted slightly for smoother gameplay.");
        }
        else
        {
            Debug.LogWarning("⚠ No SpriteRenderer or Sprite found!");
        }
    }

    [Button("🎮 Test Jump (Play Mode Only)", EButtonEnableMode.Playmode)]
    private void TestJump()
    {
        if (Application.isPlaying && canJump && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.velocity.x, jumpForce);
            Debug.Log("✨ Test jump executed!");
        }
        else if (!isGrounded)
        {
            Debug.Log("⚠️ Can't test jump - not grounded!");
        }
    }

    [Button("Use Preset: Forgiving (Beginner)")]
    private void UseForgivingPreset()
    {
        coyoteTime = 0.15f;
        jumpBufferTime = 0.15f;
        Debug.Log("Applied FORGIVING preset - great for casual/mobile games!");
    }

    [Button("Use Preset: Balanced (Default)")]
    private void UseBalancedPreset()
    {
        coyoteTime = 0.1f;
        jumpBufferTime = 0.1f;
        Debug.Log("Applied BALANCED preset - good for most games!");
    }

    [Button("Use Preset: Tight (Advanced)")]
    private void UseTightPreset()
    {
        coyoteTime = 0.05f;
        jumpBufferTime = 0.05f;
        Debug.Log("Applied TIGHT preset - for challenging platformers!");
    }

    [Button("Use Preset: Disabled (Realistic)")]
    private void UseRealisticPreset()
    {
        coyoteTime = 0f;
        jumpBufferTime = 0f;
        Debug.Log("Applied REALISTIC preset - no assistance, pure physics!");
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
        if (rb != null) rb.linearVelocity = Vector2.zero; // Stop movement
        Debug.Log("Player Respawned!");
    }
}
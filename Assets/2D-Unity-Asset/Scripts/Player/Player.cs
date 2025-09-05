using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

[ExecuteAlways]
public partial class Player : Singleton<Player>
{
    #region Player Properties
    public bool showPlayerMouse = true;

    #region Camera Settings (2D)
    public enum Camera2DType
    {
        SideScroll,
        TopDown
    }

    [Foldout("Camera", true)] public Camera2DType camera2DType = Camera2DType.SideScroll;
    [Foldout("Camera", true), SerializeField, Range(2f, 30f)] float cameraOrthoSize = 6f;
    [Foldout("Camera", true)] public float cameraZOffset = -10f;
    [Foldout("DO NOT TOUCH")] public Camera camera;
    [Foldout("DO NOT TOUCH")] public CinemachineCamera vcam2D;
    [Foldout("DO NOT TOUCH")] public CinemachineConfiner2D confiner2D;
    [Foldout("DO NOT TOUCH"), ShowIf(nameof(useConfiner))] public Collider2D confinerShape2D;
    [Foldout("Camera", true)] public bool useConfiner = false;
    [Foldout("Camera", true)] public Vector2 followOffset = new Vector2(0f, 0.5f);
    #endregion

    #region Movement Settings (2D)
    public float Speed => (speed + additionalSpeed) * speedMultiplier * axis;

    [HideInInspector] public int axis = 1;

    [Foldout("Movement Settings", true), SerializeField, Range(0, 30)] private float speed = 6f;

    [Foldout("Movement Settings", true), ShowIf(nameof(IsSideScroll)), Range(0, 25)]
    public float jumpForce = 12f;

    [Foldout("Movement Settings", true), ShowIf(nameof(IsSideScroll)), Range(0, 10)]
    public float fallMultiplier = 2.5f;

    [Foldout("Movement Settings", true), ShowIf(nameof(IsSideScroll)), Range(0, 20)]
    public float gravityMultiplier = 2.5f;

    [Foldout("Movement Settings", true)] public bool autoFlipSpriteX = true;

    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float additionalSpeed = 0f;


    public bool canHit = true;
    #endregion

    #region Movement Buffer
    private float coyoteTime = 0.1f;
    private float jumpBufferTime = 0.1f;
    private float lastGroundedTime;
    private float lastJumpPressedTime;
    #endregion

    #region Ground Check (2D)
    private float groundCheckDistance = 0.1f;
    private CapsuleCollider2D capsule2D => _capsule2D;
    #endregion

    #region Components
    [HideInInspector] public Rigidbody2D rigidbody;
    [HideInInspector] public Animator animator;
    [HideInInspector] public CapsuleCollider2D _capsule2D;
    private SpriteRenderer _spriteRenderer;
    #endregion

    [HideInInspector] public Vector3 lastCheckpoint;
    [HideInInspector] public Vector3 spawnPoint;

    public bool isGrounded = true;
    [HideInInspector] public List<ICancleGravity> cancleGravityComponents = new List<ICancleGravity>();
    public bool canApplyGravity => cancleGravityComponents.TrueForAll(x => x.canApplyGravity);
    [HideInInspector] public List<IInteruptPlayerMovement> interuptPlayerMovementComponents = new List<IInteruptPlayerMovement>();
    [HideInInspector] public bool canMove => !interuptPlayerMovementComponents.Any(x => x.isPerforming);
        [HideInInspector] public List<IUseStamina> staminaComponentStates = new List<IUseStamina>();
    public bool canGenerateStamina => staminaComponentStates.TrueForAll(x => !x.isUsingStamina);
    [HideInInspector] public bool canRotateCamera = true; // kept for API parity; unused in 2D

    #region Player Delegates
    public delegate void FixedUpdateDelegate();
    public FixedUpdateDelegate OnFixedUpdate;
    public delegate void UpdateDelegate();
    public UpdateDelegate OnUpdate;
    public delegate void CollisionEnterDelegate(Collision2D collision);
    public CollisionEnterDelegate OnCollisionEnterEvent;
    public delegate void CollisionStayDelegate(Collision2D collision);
    public CollisionStayDelegate OnCollisionStayEvent;
    public delegate void CollisionExitDelegate(Collision2D collision);
    public CollisionExitDelegate OnCollisionExitEvent;
    public delegate void TriggerEnterDelegate(Collider2D other);
    public TriggerEnterDelegate OnTriggerEnterEvent;
    public delegate void TriggerStayDelegate(Collider2D other);
    public TriggerStayDelegate OnTriggerStayEvent;
    public delegate void TriggerExitDelegate(Collider2D other);
    public TriggerExitDelegate OnTriggerExitEvent;
    #endregion

    // Extensions
    private PlayerExtension[] extensions;

    #endregion

    #region Player Stats
    [Foldout("Player Stats", true), SerializeField, Range(0, 1000)] public float maxhealth = 100f;
    [Foldout("Player Stats", true), SerializeField, Range(0, 1000)] public float maxstamina = 100f;
    [Foldout("Player Stats", true), SerializeField, Range(0, 50)] public float staminaRegenRate = 20f;
    [HideInInspector] public float currenthealth;
    [HideInInspector] public float currentstamina;
    #endregion

    // --- Unified Animator Driving ---
    private Vector2 _lastNonZeroDir = Vector2.down;              // default face "front" (0,-1)
    [SerializeField, Range(0f, 1f)] private float moveSpeedParamSmoothing = 0.08f;
    private float _animMoveSpeed = 0f;                           // smoothed 0..1 for Idle<->Walk

    #region Unity Methods
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        _capsule2D = GetComponent<CapsuleCollider2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        currenthealth = maxhealth;
        currentstamina = maxstamina;

        // Initialize animator-facing defaults
        _lastNonZeroDir = Vector2.down;
        _animMoveSpeed = 0f;

        // Ensure camera ref in editor
        if (!Application.isPlaying)
            if (camera == null) camera = Camera.main;
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            SetExtensions();
            SetUpdate();
            SetFixedUpdate();
            SetSpawnPoint(transform.position);

            if (!showPlayerMouse)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            staminaComponentStates.Clear();
            staminaComponentStates.AddRange(GetComponents<IUseStamina>());
            interuptPlayerMovementComponents.Clear();
            interuptPlayerMovementComponents.AddRange(GetComponents<IInteruptPlayerMovement>());
            cancleGravityComponents.Clear();
            cancleGravityComponents.AddRange(GetComponents<ICancleGravity>());

            if (!IsSideScroll) rigidbody.gravityScale = 0;
            else rigidbody.gravityScale = 1;

            if (camera == null) camera = Camera.main;
            SetUpCamera2D();
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            OnUpdate?.Invoke();

            // Grounded = always true in TopDown; SideScroll uses real ground check
            if (IsTopDown) isGrounded = true;

            if (isGrounded) lastGroundedTime = Time.time;

            // Regen stamina when grounded (SideScroll) or always (TopDown)
            if (IsTopDown || isGrounded) RegenerateStamina();
        }
        else
        {
            // Editor-time camera setup to avoid duplicate MainCameras
            SetUpCamera2D();
            foreach (GameObject cam in GameObject.FindGameObjectsWithTag("MainCamera"))
                if (camera != null && cam != camera.gameObject)
                    DestroyImmediate(cam);
        }
    }

    void FixedUpdate()
    {
        if (Application.isPlaying)
            OnFixedUpdate?.Invoke();
    }
    #endregion

    #region Player Methods

    #region Helpers for Mode
    private bool IsSideScroll => camera2DType == Camera2DType.SideScroll;
    private bool IsTopDown => camera2DType == Camera2DType.TopDown;
    #endregion

    #region Setup Methods
    public void SetExtensions()
    {
        extensions = GetComponents<PlayerExtension>();
        foreach (var extension in extensions) extension.OnStart(this);
    }

    public void SetUpdate()
    {
        OnUpdate = null;
        OnUpdate += CheckGrounded2D;

        if (IsSideScroll) OnUpdate += JumpHandler2D;
        // No mouse-look in 2D; keep slot for parity if you later add aim
        // OnUpdate += HandleAim2D;
    }

    public void SetFixedUpdate()
    {
        OnFixedUpdate = null;

        if (IsSideScroll)
        {
            OnFixedUpdate += ApplyGravity2D;
            OnFixedUpdate += MoveSideScroll2D;
        }
        else
        {
            OnFixedUpdate += MoveTopDown2D;
        }
    }

    public void SetSpawnPoint(Vector3 spawnPoint) => this.spawnPoint = spawnPoint;

    void SetUpCamera2D()
    {
        if (camera == null) camera = Camera.main;
        if (camera != null)
        {
            camera.orthographic = true;
            camera.transform.position = new Vector3(transform.position.x, transform.position.y, cameraZOffset);
        }

        if (vcam2D != null)
        {
            vcam2D.Follow = transform;
            var cmCamera = vcam2D;
            cmCamera.Lens.OrthographicSize = cameraOrthoSize;

            // Tweak body component offset if present
            var posComposer = cmCamera.GetComponent<Unity.Cinemachine.CinemachinePositionComposer>();
            if (posComposer != null)
            {
                // followOffset is Vector2 in your script – PositionComposer expects Vector3
                posComposer.TargetOffset = new Vector3(followOffset.x, followOffset.y, 0f);
            }

            // Confiner2D: assign shape and refresh caches (CM3)
            if (useConfiner && confiner2D != null)
            {
                confiner2D.BoundingShape2D = confinerShape2D;

                // Rebuild the polygon cache if points/scale/rotation changed
                confiner2D.InvalidateBoundingShapeCache();

                // If you change orthographic size or FOV at runtime, refresh lens cache too
                confiner2D.InvalidateLensCache();
            }
        }
    }
    #endregion

    #region Movement – SideScroll
    void MoveSideScroll2D()
    {
        if (!canMove || rigidbody == null) return;

        float horizontal = Input.GetAxis("Horizontal");

        // Physics
        Vector2 vel = rigidbody.linearVelocity;
        vel.x = horizontal * Speed;
        rigidbody.linearVelocity = vel;

        // Animator (unified)
        UpdateAnimatorFromInput(horizontal, 0f);
    }

    public void JumpHandler2D()
    {
        if (!IsSideScroll) return;
        if (Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;

        if ((Time.time - lastJumpPressedTime) <= jumpBufferTime &&
            (Time.time - lastGroundedTime) <= coyoteTime &&
            isGrounded)
        {
            Jump2D();
            lastJumpPressedTime = -999f; // prevent double fire
        }
    }

    public void Jump2D()
    {
        if (!IsSideScroll) return;
        animator?.SetTrigger("jump"); // optional; safe if no clip is wired
        var v = rigidbody.linearVelocity;
        v.y = 0f;
        rigidbody.linearVelocity = v;
        rigidbody.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;
    }
    #endregion

    #region Movement – TopDown
    void MoveTopDown2D()
    {
        if (!canMove || rigidbody == null) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector2 dir = new Vector2(horizontal, vertical);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        rigidbody.linearVelocity = dir * Speed;

        // Animator (unified)
        UpdateAnimatorFromInput(horizontal, vertical);
    }
    #endregion

    #region Gravity & Ground Check (2D)
    void ApplyGravity2D()
    {
        if (!IsSideScroll) return;

        float _fallMult = canApplyGravity ? fallMultiplier : 1f;

        if (rigidbody.linearVelocity.y < 0)
        {
            // Falling
            rigidbody.linearVelocity += Vector2.up * Physics2D.gravity.y * (_fallMult - 1f) * Time.fixedDeltaTime;
        }
        else if (rigidbody.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            // Early jump release
            rigidbody.linearVelocity += Vector2.up * Physics2D.gravity.y * (gravityMultiplier - 1f) * Time.fixedDeltaTime;
        }

        // Small downward stick when grounded
        if (isGrounded && rigidbody.linearVelocity.y < 0)
            rigidbody.linearVelocity = new Vector2(rigidbody.linearVelocity.x, -2f);
    }

    void CheckGrounded2D()
    {
        if (!IsSideScroll) { isGrounded = true; return; }
        if (capsule2D == null) { isGrounded = true; return; }

        // Cast a small box beneath the collider
        Bounds b = capsule2D.bounds;
        float skin = 0.02f;
        Vector2 size = new Vector2(b.size.x * 0.95f, skin);
        Vector2 origin = new Vector2(b.center.x, b.min.y - skin * 0.5f);

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, ~0);

        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;

        if (isGrounded) lastGroundedTime = Time.time;

        // Land event (optional hook)
        if (!wasGrounded && isGrounded)
        {
            // e.g., play land SFX
        }
    }
    #endregion

    #region Animator – Unified Driver
    /// <summary>
    /// Feed animator with direction + speed in a unified way for both modes.
    /// Uses only three parameters: MoveX, MoveY, MoveSpeed (0..1).
    /// Keeps the last facing direction so Idle shows front/back/side correctly.
    /// </summary>
    private void UpdateAnimatorFromInput(float horizontal, float vertical)
{
    if (animator == null) return;

    // 1. Input as vector
    Vector2 inputDir = new Vector2(horizontal, vertical);

    if (IsSideScroll)
    {
        // SideScroll = only X matters
        if (Mathf.Abs(inputDir.x) > 0.01f)
        {
            _lastNonZeroDir = new Vector2(Mathf.Sign(inputDir.x), 0f);
        }

        // Flip sprite visually
        if (autoFlipSpriteX && _spriteRenderer != null && Mathf.Abs(_lastNonZeroDir.x) > 0.01f)
            _spriteRenderer.flipX = _lastNonZeroDir.x < 0f;

        inputDir = new Vector2(inputDir.x, 0f);
    }
    else
    {
        // TopDown = both axes
        if (inputDir.sqrMagnitude > 0.01f)
            _lastNonZeroDir = inputDir.normalized;
    }

    // 2. Is the player moving right now?
    bool isMoving = inputDir.sqrMagnitude > 0.0001f;

    // 3. If not moving, lock to last direction
    Vector2 facing = isMoving ? inputDir.normalized : _lastNonZeroDir;

    // 4. Smooth MoveSpeed for Idle <-> Walk blending
    float targetSpeed = Mathf.Clamp01(isMoving ? inputDir.magnitude : 0f);
    _animMoveSpeed = Mathf.MoveTowards(
        _animMoveSpeed, targetSpeed,
        Time.deltaTime / Mathf.Max(0.01f, moveSpeedParamSmoothing)
    );

    // 5. Send to Animator
    animator.SetFloat("MoveX", Mathf.Clamp(facing.x, -1f, 1f));
    animator.SetFloat("MoveY", Mathf.Clamp(facing.y, -1f, 1f));
    animator.SetFloat("MoveSpeed", _animMoveSpeed);
}

    #endregion

    #region Stamina / Damage / Respawn
    private void RegenerateStamina()
    {
        if (currentstamina < maxstamina && canGenerateStamina)
        {
            currentstamina += staminaRegenRate * Time.deltaTime;
            if (currentstamina > maxstamina) currentstamina = maxstamina;
        }
    }

    public void TakeDamage(float amount)
    {
        if (canHit)
        {
            currenthealth -= Mathf.Max(amount, 0);
            animator?.SetTrigger("GetHit");
            if (currenthealth <= 0)
            {
                currenthealth = 0;
                Respawn();
            }
        }
        else
        {
            canHit = true;
            animator?.SetBool("isBlocking", false);
        }
    }

    public void Respawn()
    {
        rigidbody.linearVelocity = Vector2.zero;
        Vector3 target = lastCheckpoint == Vector3.zero ? spawnPoint : lastCheckpoint;
        transform.position = target;
        currenthealth = maxhealth;
    }

    public float GetAnimationLength(string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == animationName) return clip.length;
        return 0f;
    }
    #endregion

    #region 2D Physics Events
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Application.isPlaying) OnCollisionEnterEvent?.Invoke(collision);
    }
    void OnCollisionStay2D(Collision2D collision)
    {
        if (Application.isPlaying) OnCollisionStayEvent?.Invoke(collision);
    }
    void OnCollisionExit2D(Collision2D collision)
    {
        if (Application.isPlaying) OnCollisionExitEvent?.Invoke(collision);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Application.isPlaying) OnTriggerEnterEvent?.Invoke(other);
    }
    void OnTriggerStay2D(Collider2D other)
    {
        if (Application.isPlaying) OnTriggerStayEvent?.Invoke(other);
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (Application.isPlaying) OnTriggerExitEvent?.Invoke(other);
    }
    #endregion

    #region Gizmos
    void OnDrawGizmosSelected()
    {
        if (capsule2D == null) return;

        Bounds b = capsule2D.bounds;
        float skin = 0.02f;
        Vector2 size = new Vector2(b.size.x * 0.95f, skin);
        Vector3 origin = new Vector3(b.center.x, b.min.y - skin * 0.5f, 0f);
        Vector3 p1 = origin + new Vector3(-size.x * 0.5f, 0f, 0f);
        Vector3 p2 = origin + new Vector3(size.x * 0.5f, 0f, 0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawWireSphere(p1, 0.02f);
        Gizmos.DrawWireSphere(p2, 0.02f);
    }
    #endregion

    #endregion
}

/*
============================ ANIMATOR WIRING ============================

Create EXACTLY these parameters:
  - Float MoveX      [-1..+1]  // facing X  (left/right). In SideScroll becomes ±1
  - Float MoveY      [-1..+1]  // facing Y  (front/back). In SideScroll stays 0
  - Float MoveSpeed  [0..1]    // smoothed movement amount (drives Idle <-> Walk)

States:
  1) Idle  ->  Blend Tree (2D Freeform Directional) with parameters (MoveX, MoveY)
       Motions:
         Idle_Front at (0, -1)
         Idle_Back  at (0, +1)
         Idle_Side  at (+1, 0)
         Idle_Side  at (-1, 0)   // reuse same side clip

  2) Walk  ->  Blend Tree (2D Freeform Directional) with parameters (MoveX, MoveY)
       Motions:
         Walk_Front at (0, -1)
         Walk_Back  at (0, +1)
         Walk_Side  at (+1, 0)
         Walk_Side  at (-1, 0)   // reuse same side clip

Transitions:
  Idle -> Walk : MoveSpeed > 0.01
  Walk -> Idle : MoveSpeed < 0.01
  (Optional hysteresis: 0.02 one way to avoid flicker)

SideScroll mirroring:
  - Keep autoFlipSpriteX = true to mirror left/right using SpriteRenderer.flipX.

========================================================================
*/

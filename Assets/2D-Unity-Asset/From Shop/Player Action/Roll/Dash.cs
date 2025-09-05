using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Dash (2D) — press activateKey to dash in the current move direction (or last non-zero).
/// Optional stamina cost + cooldown UI. Adds a TrailRenderer while dashing.
/// </summary>
public class Dash : PlayerExtension, IUseStamina,IInteruptPlayerMovement
{
    [Header("UI")]
    public bool enableDashUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Q;
    [Tooltip("Dash speed in world units/second (2D).")]
    public float dashSpeed = 12f;
    [Tooltip("How long the dash lasts, in seconds.")]
    public float dashDuration = 0.15f;
    [Tooltip("Cooldown between dashes, in seconds.")]
    public float cooldownTime = 1f;

    [Header("Stamina")]
    public bool useStamina = true;
    [ShowIf(nameof(useStamina))] public float staminaCost = 15f;
    public bool isUsingStamina => useStamina && isDashing;
    public bool canDrainStamina => useStamina && _player.currentstamina >= staminaCost;

    private float heightScaleWhileDash = 0.5f;

    
    
    [Foldout("FX")]public TrailRenderer dashTrail;
    
    [Foldout("FX")]public float trailTime = 0.18f;
    
    [Foldout("FX")]public float trailStartWidth = 0.25f;
    
    [Foldout("FX")]public float trailEndWidth = 0.05f;
    [Foldout("FX")]public Gradient trailColor = DefaultTrailGradient();

    // --- State ---
    private float lastDashTime = 0f;
    public bool isPerforming => isDashing;
    private bool isDashing = false;
    private Vector2 dashDir = Vector2.right;
    private Vector2 lastNonZeroMoveDir = Vector2.right;

    // Collider cache (avoid cumulative error)
    private CapsuleCollider2D col2D;
    private Vector2 originalSize;
    private Vector2 originalOffset;
    private bool cachedCollider = false;

    // Convenience
    private bool IsReadyToDash => Time.time >= lastDashTime + cooldownTime;
    private bool CanDash =>
        _player.canMove &&
        _player.isGrounded &&
        _player.canApplyGravity &&
        IsReadyToDash &&
        (!useStamina || _player.currentstamina >= staminaCost);

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        lastDashTime = -cooldownTime; // allow immediate use

        if (enableDashUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();

        col2D = _player._capsule2D;
        if (col2D != null)
        {
            originalSize = col2D.size;
            originalOffset = col2D.offset;
            cachedCollider = true;
        }

        EnsureTrail();
        EnableTrail(false);
    }

    private void Update()
    {
        // UI cooldown bar
        if (enableDashUI && uiManager != null)
            uiManager.UpdateDashCooldown(Time.time - lastDashTime, cooldownTime);

        // Track last non-zero movement dir for dash facing
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector2 move = new Vector2(h, v);
        if (move.sqrMagnitude > 0.0001f)
            lastNonZeroMoveDir = move.normalized;

        if (!isDashing && Input.GetKeyDown(activateKey) && CanDash)
            StartDash(move);
    }

    private void FixedUpdate()
    {
        if (!isDashing) return;

        // While dashing, enforce constant velocity.
        if (IsSideScroll())
        {
            // Keep current vertical velocity in sidescroller; dash along X only.
            var currentVY = GetRBVelocity().y;
            SetRBVelocity(new Vector2(dashDir.normalized.x * dashSpeed, currentVY));
        }
        else
        {
            // Top-down: dash in 2D plane
            SetRBVelocity(dashDir.normalized * dashSpeed);
        }
    }

    private void StartDash(Vector2 currentMoveInput)
    {
        isDashing = true;
        _player.canRotateCamera = false;

        if (canDrainStamina) DrainStamina(staminaCost);

        // Determine dash direction
        if (IsSideScroll())
        {
            float dirX = Mathf.Abs(currentMoveInput.x) > 0.0001f
                ? Mathf.Sign(currentMoveInput.x)
                : Mathf.Sign(lastNonZeroMoveDir.x == 0 ? 1f : lastNonZeroMoveDir.x);
            dashDir = new Vector2(dirX, 0f);
        }
        else
        {
            dashDir = currentMoveInput.sqrMagnitude > 0.0001f
                ? currentMoveInput.normalized
                : lastNonZeroMoveDir;
        }

        // Animation (optional)
        if (_player.animator != null)
        {
            // Use your existing "Slide" trigger if you like the motion. Rename to "Dash" if you have one.
            _player.animator.SetTrigger("Slide");
        }

        // Visual trail
        EnableTrail(true);
        dashTrail?.Clear();

        // Schedule stop
        this.Invoke(nameof(StopDash), dashDuration);
    }

    private void StopDash()
    {

        // Stop forcing velocity; gently keep current motion
        isDashing = false;
        _player.canRotateCamera = true;
        lastDashTime = Time.time;

        EnableTrail(false);
    }

    private bool IsSideScroll() =>
        _player.camera2DType == Player.Camera2DType.SideScroll;

    // --- Stamina ---
    public void DrainStamina(float amount)
    {
        if (!useStamina) return;
        _player.currentstamina = Mathf.Max(0f, _player.currentstamina - amount);
    }
    void IUseStamina.DrainStamina(float amount) => DrainStamina(amount);

    // --- Rigidbody helpers (safe across custom wrappers) ---
    private Vector2 GetRBVelocity()
    {
        // Prefer a standard Rigidbody2D if present
        var rb2d = _player.GetComponent<Rigidbody2D>();
        if (rb2d != null) return rb2d.linearVelocity;

        // Fallback: try a 'linearVelocity' property via reflection (your custom wrapper)
        var rb = _player.rigidbody;
        if (rb != null)
        {
            var p = rb.GetType().GetProperty("linearVelocity");
            if (p != null)
            {
                object val = p.GetValue(rb, null);
                if (val is Vector2 v2) return v2;
                if (val is Vector3 v3) return (Vector2)v3;
            }
        }
        return Vector2.zero;
    }

    private void SetRBVelocity(Vector2 v)
    {
        var rb2d = _player.GetComponent<Rigidbody2D>();
        if (rb2d != null) { rb2d.linearVelocity = v; return; }

        var rb = _player.rigidbody;
        if (rb != null)
        {
            var p = rb.GetType().GetProperty("linearVelocity");
            if (p != null) { p.SetValue(rb, v, null); return; }
        }
    }

    // --- Trail helpers ---
    private void EnsureTrail()
    {
        if (dashTrail != null) return;

        // Attach to player's visual root (or this GameObject if unknown)
        var host = _player != null ? _player.gameObject : this.gameObject;
        dashTrail = host.GetComponent<TrailRenderer>();
        if (dashTrail == null) dashTrail = host.AddComponent<TrailRenderer>();

        // Basic material (Sprites/Default to avoid magenta)
        if (dashTrail.sharedMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            dashTrail.sharedMaterial = mat;
        }

        // Configure
        dashTrail.time = trailTime;
        dashTrail.widthCurve = AnimationCurve.Linear(0, trailStartWidth, 1, trailEndWidth);
        dashTrail.colorGradient = trailColor;
        dashTrail.minVertexDistance = 0.05f;
        dashTrail.autodestruct = false;
        dashTrail.emitting = false;
        dashTrail.enabled = false;
    }

    private void EnableTrail(bool on)
    {
        if (dashTrail == null) return;
        dashTrail.time = trailTime;
        dashTrail.emitting = on;
        dashTrail.enabled = on;
    }

    // A nice default white->transparent gradient if you don’t provide one.
    private static Gradient DefaultTrailGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f),
            }
        );
        return g;
    }
}

/// <summary>
/// Optional: add this to your PlayerUIManager (partial) to support a dedicated dash cooldown UI,
/// without colliding with your existing roll UI methods.
/// Guarded so it’s safe if the bar isn’t present.
/// </summary>
public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    public bool enableDashCooldownUI = true;

    public void UpdateDashCooldown(float timeSinceLastDash, float cooldownTime)
    {
        if (!enableDashCooldownUI || dashCooldownUI == null) return;
        bool isOnCooldown = timeSinceLastDash < cooldownTime;
        dashCooldownUI.gameObject.SetActive(isOnCooldown);
        if (isOnCooldown)
            dashCooldownUI.value = Mathf.Clamp01(timeSinceLastDash / cooldownTime);
    }
}

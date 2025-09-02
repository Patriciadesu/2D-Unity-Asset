using NaughtyAttributes;
using UnityEngine;

public class Roll : PlayerExtension, IUseStamina
{
    [Header("UI")]
    public bool enableRollUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Q;
    public float rollSpeed = 1f;          // Multiplier applied to _player.Speed
    public float rollDuration = 0.15f;
    public float cooldownTime = 1f;

    [Header("Stamina")]
    public bool useStamina = true;
    [ShowIf("useStamina")] public float staminaCost = 15f;
    public bool isUsingStamina => useStamina && isRolling;
    public bool canDrainStamina => useStamina && _player.currentstamina >= staminaCost;

    // State
    private float lastRollTime = 0f;
    private bool isRolling = false;
    private Vector2 rollDirection2D = Vector2.zero;
    private Vector2 lastNonZeroMoveDir = Vector2.right; // fallback if no input

    // Collider restore cache (avoid cumulative error)
    private CapsuleCollider2D col2D;
    private Vector2 originalSize;
    private Vector2 originalOffset;
    private bool cachedCollider = false;

    // Convenience
    private bool IsReadyToRoll => Time.time >= lastRollTime + cooldownTime;
    private float rollAnimSpeed => rollSpeed / Mathf.Max(0.01f, _player.GetAnimationLength("Slide"));
    private bool CanRoll =>
        _player.canMove &&
        _player.isGrounded &&
        _player.canApplyGravity &&
        IsReadyToRoll &&
        (!useStamina || _player.currentstamina >= staminaCost);

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        lastRollTime = -cooldownTime; // allow immediate use
        if (enableRollUI) uiManager = Object.FindAnyObjectByType<PlayerUIManager>();

        col2D = _player._capsule2D;
        if (col2D != null)
        {
            originalSize = col2D.size;
            originalOffset = col2D.offset;
            cachedCollider = true;
        }
    }

    private void Update()
    {
        Debug.Log(CanRoll);
        // UI cooldown bar
        if (enableRollUI && uiManager != null)
            uiManager.UpdateRollCooldown(Time.time - lastRollTime, cooldownTime);

        // Track last non-zero movement dir for roll facing
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector2 move = new Vector2(h, v);
        if (move.sqrMagnitude > 0.0001f)
            lastNonZeroMoveDir = move.normalized;

        if (isRolling)
        {
            ApplyRollingVelocity();
        }
        else if (Input.GetKeyDown(activateKey) && CanRoll)
        {
            StartRoll(move);
        }
    }

    private void ApplyRollingVelocity()
    {
        // 2D Player used linearVelocity in your stack; keep consistent.
        var vel = _player.rigidbody.linearVelocity;

        if (IsSideScroll())
        {
            // Preserve current vertical velocity; roll only along X
            vel = new Vector2(rollDirection2D.normalized.x * rollSpeed, vel.y);
        }
        else // TopDown
        {
            vel = rollDirection2D.normalized * rollSpeed;
        }

        _player.rigidbody.AddForce(vel,ForceMode2D.Impulse);
    }

    private void StartRoll(Vector2 currentMoveInput)
    {
        isRolling = true;
        _player.canMove = false;
        _player.canRotateCamera = false;

        if (canDrainStamina) DrainStamina(staminaCost);

        // Determine roll direction
        if (IsSideScroll())
        {
            // Prefer horizontal input; fallback to last facing dir
            float dirX = Mathf.Abs(currentMoveInput.x) > 0.0001f
                ? Mathf.Sign(currentMoveInput.x)
                : Mathf.Sign(lastNonZeroMoveDir.x == 0 ? 1f : lastNonZeroMoveDir.x);
            rollDirection2D = new Vector2(dirX, 0f);
        }
        else
        {
            // Top-down: use current input or last stored direction
            rollDirection2D = currentMoveInput.sqrMagnitude > 0.0001f
                ? currentMoveInput.normalized
                : lastNonZeroMoveDir;
        }

        // Temporarily shrink collider height for low-profile slide
        if (col2D != null && cachedCollider)
        {
            col2D.size = new Vector2(originalSize.x, originalSize.y * 0.5f);
            col2D.offset = new Vector2(originalOffset.x, originalOffset.y * 0.5f);
        }

        // Play animation faster if needed
        if (_player.animator != null)
        {
            _player.animator.speed = rollAnimSpeed;
            _player.animator.SetTrigger("Slide");
        }

        // Schedule stop
        this.Invoke("StopRoll", rollDuration);
    }

    private bool IsSideScroll() =>
        _player.camera2DType == Player.Camera2DType.SideScroll;

    private void StopRoll()
    {
        // Restore collider
        if (col2D != null && cachedCollider)
        {
            col2D.size = originalSize;
            col2D.offset = originalOffset;
        }
        _player.canMove = true;
        _player.canRotateCamera = true;
        isRolling = false;
        if (_player.animator != null) _player.animator.speed = 1f;
        lastRollTime = Time.time;
    }

    public void DrainStamina(float amount)
    {
        if (!useStamina) return;
        _player.currentstamina = Mathf.Max(0f, _player.currentstamina - amount);
    }

    // Implement interface explicitly as well, routing to same logic
    void IUseStamina.DrainStamina(float amount) => DrainStamina(amount);
}

public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    public bool enableRollCooldownUI = true;

    public void UpdateRollCooldown(float timeSinceLastRoll, float cooldownTime)
    {
        if (!enableRollCooldownUI || rollCooldownUI == null) return;
        bool isOnCooldown = timeSinceLastRoll < cooldownTime;
        rollCooldownUI.gameObject.SetActive(isOnCooldown);
        if (isOnCooldown)
            rollCooldownUI.value = Mathf.Clamp01(timeSinceLastRoll / cooldownTime);
    }
}

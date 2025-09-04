using System.Collections;
using UnityEngine;

public class ShadowDash : GhostPhaseRobust
{
    [Header("Dash Settings")]
    public float dashSpeedMultiplier = 3f;   // how much faster while dashing
    public float dashDuration = 0.5f;        // how long the dash lasts
    public KeyCode dashKey = KeyCode.LeftShift;

    private Rigidbody2D rb;                  // for movement
    private Vector2 lastMoveDir;             // store last input direction
    private bool isDashing = false;

    public override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        if (!rb) Debug.LogError("[GhostDash] Rigidbody2D required for dashing.");
    }

    void Update()
    {
        // Only allow dash if not ghosting, not on cooldown, and key pressed
        if (Input.GetKeyDown(dashKey) && !isGhosting && !isOnCooldown && !isDashing)
        {
            if (rb != null)
            {
                // Get direction from input (WASD / Arrow keys)
                Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (inputDir.sqrMagnitude > 0.01f)
                {
                    lastMoveDir = inputDir.normalized;
                }
                else if (lastMoveDir == Vector2.zero)
                {
                    // If no movement input yet, dash forward in X
                    lastMoveDir = Vector2.right;
                }
            }

            activeCoroutine = StartCoroutine(DoDash());
        }
    }

    protected IEnumerator DoDash()
    {
        isDashing = true;
        isGhosting = true;
        isOnCooldown = true;

        // Enter Ghost state
        SetAllLayers(ghostLayerName);
        Debug.Log("[GhostDash] Dash start (Ghost state).");

        // Transparency effect
        var sr = GetComponent<SpriteRenderer>();
        if (sr) { Color c = sr.color; c.a = 0.5f; sr.color = c; }

        // Perform dash movement
        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            if (rb != null)
                rb.linearVelocity = lastMoveDir * (dashSpeedMultiplier * 5f); // base speed × multiplier
            dashTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // Stop dash
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Restore
        RestoreOriginalLayers();
        if (sr) { Color c = sr.color; c.a = 1f; sr.color = c; }

        isDashing = false;
        isGhosting = false;
        Debug.Log("[GhostDash] Dash ended.");

        // Cooldown before next dash
        yield return new WaitForSecondsRealtime(cooldown);
        isOnCooldown = false;
        activeCoroutine = null;
        Debug.Log("[GhostDash] Cooldown finished.");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ghost Dash using AddForce (Impulse).
/// Temporarily sets player to Ghost layer, fades sprite, applies force impulse,
/// then restores everything. Works in TopDown & SideScroll.
/// </summary>
public class GhostDash : PlayerExtension, IInteruptPlayerMovement
{
    [Header("Dash Settings")]
    public KeyCode dashKey = KeyCode.LeftShift;

    [Tooltip("Layer to apply while ghosting (configure Physics2D matrix).")]
    public string ghostLayerName => "Ghost";

    [Tooltip("Seconds between dashes.")]
    public float cooldown = 5f;

    [Tooltip("Impulse force multiplier.")]
    public float dashForce = 5f;

    [Tooltip("How long the ghost state lasts after dash (seconds).")]
    [Range(0.01f,0.3f)]public float dashDuration = 0.2f;

    private float ghostAlpha = 0.5f;

    private Rigidbody2D rb;
    public bool isPerforming => isDashing;
    private bool isDashing = false;
    protected bool isGhosting = false;
    protected bool isOnCooldown = false;
    protected Coroutine activeCoroutine = null;

    // Layers
    private readonly List<Transform> allTransforms = new List<Transform>();
    private readonly Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();

    // Sprite colors
    private readonly List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    private readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();

    private Vector2 lastMoveDir = Vector2.right;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = _player ? _player.rigidbody : null;
        if (!rb) Debug.LogError("[GhostDash] Rigidbody2D required for dashing.");

        RebuildCaches();
    }

    void Update()
    {
        if (rb == null || _player == null) return;

        // Update last movement direction
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (_player.camera2DType == Player.Camera2DType.SideScroll)
        {
            if (Mathf.Abs(h) > 0.01f) lastMoveDir = new Vector2(Mathf.Sign(h), 0f);
        }
        else
        {
            Vector2 inDir = new Vector2(h, v);
            if (inDir.sqrMagnitude > 0.01f) lastMoveDir = inDir.normalized;
        }
        if (lastMoveDir == Vector2.zero) lastMoveDir = Vector2.right;

        // Dash input
        if (Input.GetKeyDown(dashKey) && !isGhosting && !isOnCooldown && !isDashing)
            activeCoroutine = StartCoroutine(DoDash());
    }

    IEnumerator DoDash()
    {
        isDashing = true;
        isGhosting = true;
        isOnCooldown = true;

        RebuildCaches();

        // Apply ghost layer + fade
        if (!SetAllLayers(ghostLayerName))
            Debug.LogWarning($"[GhostDash] Layer '{ghostLayerName}' not found.");
        FadeSprites(ghostAlpha);

        // Disable movement so AddForce isn’t overwritten

        // Apply impulse force once
        rb.AddForce(lastMoveDir.normalized * dashForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(dashDuration);

        // Restore layers + visuals
        RestoreOriginalLayers();
        RestoreSpriteColors();


        isDashing = false;
        isGhosting = false;

        // Cooldown
        yield return new WaitForSecondsRealtime(cooldown);
        isOnCooldown = false;
        activeCoroutine = null;
    }

    // --- Helpers ---
    void RebuildCaches()
    {
        allTransforms.Clear();
        originalLayers.Clear();
        spriteRenderers.Clear();
        originalColors.Clear();

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            allTransforms.Add(t);
            originalLayers[t] = t.gameObject.layer;
        }
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            spriteRenderers.Add(sr);
            originalColors[sr] = sr.color;
        }
    }

    bool SetAllLayers(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1) return false;

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (!originalLayers.ContainsKey(t))
                originalLayers[t] = t.gameObject.layer;
            t.gameObject.layer = layer;
        }
        return true;
    }

    void RestoreOriginalLayers()
    {
        foreach (var kv in originalLayers)
            if (kv.Key) kv.Key.gameObject.layer = kv.Value;
    }

    void FadeSprites(float a)
    {
        foreach (var sr in spriteRenderers)
        {
            if (!sr) continue;
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }
    }

    void RestoreSpriteColors()
    {
        foreach (var kv in originalColors)
            if (kv.Key) kv.Key.color = kv.Value;
    }
}

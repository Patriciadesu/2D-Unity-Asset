using NaughtyAttributes;
using UnityEngine;

public class BowShot : PlayerExtension
{
    [Header("Input")]
    public KeyCode activateKey = KeyCode.Mouse0;

    [Header("Animation Timing")]
    [Range(0f, 1f)] public float pauseTime = 0.4f;   // normalized time in "BowShot" where we pause
    public float drawTime = 3f;                      // seconds to reach pauseTime

    [Header("Arrow")]
    public GameObject projectilePrefab;              // optional; if null we build one
    public Transform spawnPoint;                     // optional; if null we spawn in front of player (2 units)
    public float speed = 10f;
    public float damage = 10f;
    public bool hasDestroyTime = true;
    [ShowIf(nameof(hasDestroyTime))] public float destroyTime = 2f;

    [Header("UX")]
    public bool forceShowMouse = true;

    // internal state
    private bool canShot = false;    // we reached pause point and can shoot on release
    private bool isHolding = false;  // currently drawing
    private bool isPaused = false;   // animator paused at full draw
    private float clipLength = 0f;
    private bool hasAnimClip = false;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        CacheClipLength();

        if (forceShowMouse)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnEnable()
    {
        if (forceShowMouse)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void Update()
    {
        if (forceShowMouse)
        {
            if (!Cursor.visible) Cursor.visible = true;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        }

        // BYPASS: no Animator or no "BowShot" clip → fire instantly on press
        if (!hasAnimClip || _player?.animator == null)
        {
            if (Input.GetKeyDown(activateKey))
                ShootInstant();
            return;
        }

        // Animated flow
        CheckHolding();

        if (Input.GetKeyDown(activateKey))
            DrawBow();

        if (Input.GetKeyUp(activateKey))
        {
            ShotBow();
            if (!canShot)
            {
                // released early: cancel back to Idle
                _player.animator.speed = 1f;
                _player.animator.Play("Idle", 0, 0);
            }
        }
    }

    // ---------- Animation helpers ----------
    void CacheClipLength()
    {
        hasAnimClip = false;
        clipLength = 0f;

        if (_player != null && _player.animator != null && _player.animator.runtimeAnimatorController != null)
        {
            foreach (var c in _player.animator.runtimeAnimatorController.animationClips)
            {
                if (c && c.name == "BowShot")
                {
                    clipLength = c.length;
                    hasAnimClip = clipLength > 0f;
                    break;
                }
            }
        }
    }

    void DrawBow()
    {
        if (canShot) return;

        isHolding = true;
        isPaused = false;

        // Set animator speed so we hit pauseTime at drawTime seconds
        float p = Mathf.Clamp01(pauseTime);
        float L = Mathf.Max(clipLength, 0.0001f);
        float T = Mathf.Max(drawTime, 0.0001f);
        float speedForDraw = (p * L) / T;

        _player.animator.speed = Mathf.Max(speedForDraw, 0.001f);
        _player.animator.Play("BowShot", 0, 0f);
    }

    void CheckHolding()
    {
        if (!isHolding || isPaused || _player.animator == null) return;

        var info = _player.animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("BowShot")) return;

        float t = info.normalizedTime % 1f;
        if (t + Time.deltaTime >= pauseTime)
        {
            _player.animator.speed = 0f; // pause at full draw
            isPaused = true;
            canShot = true;
        }
    }

    void ShotBow()
    {
        if (!canShot) return;

        canShot = false;
        isHolding = false;
        isPaused = false;

        _player.animator.speed = 1f;
        ShootCommon();
    }

    // ---------- Bypass (no anim) ----------
    void ShootInstant() => ShootCommon();

    // ---------- Common shooter ----------
    void ShootCommon()
    {
        // Compute direction first (used by both spawn and velocity)
        Vector2 dir = ComputeShotDirection2D();
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir = dir.normalized;

        // Spawn position: use spawnPoint if provided, otherwise 2 units in front of player along dir
        Vector3 spawnPos = (spawnPoint != null)
            ? spawnPoint.position
            : _player.transform.position + (Vector3)(dir * 1.2f);

        // Build or use prefab
        GameObject projectile = (projectilePrefab != null) ? Instantiate(projectilePrefab) : CreateDefaultArrow2D();

        // Place & orient
        projectile.transform.position = spawnPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Ensure physics
        var rb2d = projectile.GetComponent<Rigidbody2D>() ?? projectile.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb2d.linearVelocity = dir * speed; // <-- actual launch

        if (projectile.GetComponent<Collider2D>() == null)
        {
            var box = projectile.AddComponent<BoxCollider2D>();
            box.isTrigger = false;
        }

        if (hasDestroyTime && destroyTime > 0f)
            Destroy(projectile, destroyTime);

        // Damage handoff
        var arrow = projectile.GetComponent<Arrow>();
        if (arrow == null) arrow = projectile.AddComponent<Arrow>();
        arrow.SetUp(damage);

        // Debug to verify
        // Debug.Log($"[BowShot2D] Shot dir: {dir}, pos: {spawnPos}, speed: {speed}");
    }

    Vector2 ComputeShotDirection2D()
    {
        // Top-Down: aim at mouse
        if (_player.camera2DType == Player.Camera2DType.TopDown)
        {
            if (_player.camera != null)
            {
                Vector3 m = Input.mousePosition;
                Vector3 world = _player.camera.ScreenToWorldPoint(m);
                world.z = _player.transform.position.z;
                Vector2 aim = (world - (spawnPoint ? spawnPoint.position : _player.transform.position));
                if (aim.sqrMagnitude > 0.0001f) return aim.normalized;
            }
            // fallback: movement vector
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector2 mv = new Vector2(h, v);
            if (mv.sqrMagnitude > 0.0001f) return mv.normalized;
            return Vector2.right;
        }
        // Side-Scroll: ±X (input or facing)
        else
        {
            float h = Input.GetAxis("Horizontal");
            if (Mathf.Abs(h) > 0.0001f) return new Vector2(Mathf.Sign(h), 0f);

            float sx = Mathf.Sign(_player.transform.localScale.x == 0 ? 1f : _player.transform.localScale.x);
            return new Vector2(sx, 0f);
        }
    }

    // ---------- Default arrow factory ----------
    GameObject CreateDefaultArrow2D()
    {
        var go = new GameObject("Arrow2D_Default");

        // Visible sprite
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSquareSprite(0.15f);
        sr.sortingOrder = 10;

        // Physics
        var rb2d = go.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        // Simple trail
        var tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.15f;
        tr.startWidth = 0.06f;
        tr.endWidth = 0f;
        tr.minVertexDistance = 0.01f;
        tr.emitting = true;

        Arrow arrow = go.AddComponent<Arrow>();
        arrow.SetUp(damage);

        return go;
    }

    Sprite CreateDefaultSquareSprite(float sizeWorld)
    {
        const int px = 8;
        var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
        var cols = new Color[px * px];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();

        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, px, px),
            new Vector2(0.5f, 0.5f),
            px / sizeWorld // pixels-per-unit: world size ≈ sizeWorld
        );
        sprite.name = "Arrow2D_DefaultSprite";
        return sprite;
    }
}

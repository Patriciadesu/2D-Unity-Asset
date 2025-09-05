using NaughtyAttributes;
using UnityEngine;

public class BowShot : PlayerExtension,IInteruptPlayerMovement
{
    public enum AimMode { MovementOrFacing, Mouse }

    [Header("Input")]
    public KeyCode activateKey = KeyCode.Mouse0;

    [Header("Aim")]
    [Tooltip("TopDown aim source (Mouse recommended).")]
    public AimMode topDownAim = AimMode.Mouse;
    [Tooltip("SideScroll aim source (set to Mouse to shoot toward cursor).")]
    public AimMode sideScrollAim = AimMode.MovementOrFacing;
    [Tooltip("When SideScroll uses Mouse, limit vertical aim to help 2D side animations (0 = free, 1 = flat).")]
    [Range(0f, 1f)] public float sideScrollVerticalDamp = 0.0f;

    [Header("Animation")]
    [Tooltip("Animator state name for the bow drawing blend-tree (2D directional).")]
    public string bowStateName = "Bow_Draw";
    [Tooltip("Normalized time to HOLD at full draw (1 = clip end).")]
    [Range(0f, 1f)] public float pauseTime = 1f;
    [Tooltip("Seconds it takes to reach 'pauseTime' (full draw).")]
    public float drawTime = 0.6f;

    [Header("Arrow")]
    public GameObject projectilePrefab;              // if null → auto-build default
    public Transform spawnPoint;                     // if null → auto-create child
    [Tooltip("Distance in front of player for default spawn point.")]
    public float defaultSpawnDistance = 1.2f;
    public float speed = 12f;
    public float damage = 10f;
    public bool hasDestroyTime = true;
    [ShowIf(nameof(hasDestroyTime))] public float destroyTime = 3f;

    [Header("UX")]
    public bool forceShowMouse = true;

    // internal state
    public bool isPerforming => isHolding;
    private bool canShot = false;    // reached pause and can shoot on release
    private bool isHolding = false;  // currently drawing (button held)
    private bool isPaused = false;   // animator paused at full draw
    private float clipLength = 0f;
    private bool hasAnimState = false;

    // cached
    private SpriteRenderer _sr;      // for side-scroll left/right flip while aiming

    public override void OnStart(Player player)
    {
        base.OnStart(player);

        if (projectilePrefab == null) projectilePrefab = Resources.Load<GameObject>("Arrow");

        _sr = _player ? _player.GetComponentInChildren<SpriteRenderer>() : null;

        EnsureSpawnPoint();
        CacheBowClipLength();

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

    private void OnDisable()
    {
        // safety: restore animator speed & movement if disabled mid-hold
        if (_player != null)
        {
            if (_player.animator != null) _player.animator.speed = 1f;
        }
        isHolding = isPaused = canShot = false;
    }

    void Update()
    {
        if (forceShowMouse)
        {
            if (!Cursor.visible) Cursor.visible = true;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        }

        if (_player?.animator == null || !hasAnimState)
        {
            // No animator/clip → simple fallback: instant fire on press
            if (Input.GetKeyDown(activateKey)) ShootInstant();
            return;
        }

        if (Input.GetKeyDown(activateKey) && !isHolding)
            BeginDraw();

        if (isHolding) WhileHoldingUpdate();

        if (Input.GetKeyUp(activateKey))
        {
            if (canShot) ReleaseAndShoot();
            else        CancelDraw();
        }
    }

    // ---------- Core flow ----------
    void BeginDraw()
    {
        isHolding = true;
        isPaused  = false;
        canShot   = false;


        float p = Mathf.Clamp01(pauseTime);
        float L = Mathf.Max(clipLength, 0.0001f);
        float T = Mathf.Max(drawTime, 0.0001f);
        float speedForDraw = (p * L) / T;
        _player.animator.speed = Mathf.Max(speedForDraw, 0.001f);

        _player.animator.Play(bowStateName, 0, 0f);

        // make sure the first frame picks the correct front/back/side
        ApplyAimFacingToAnimator();
    }

    void WhileHoldingUpdate()
    {
        ApplyAimFacingToAnimator();
        MaybeFlipSpriteForSideScroll();

        var info = _player.animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(bowStateName)) return;

        float t = info.normalizedTime;
        if (!isPaused && t >= pauseTime - 0.0005f)
        {
            _player.animator.speed = 0f; // HOLD pose, do not loop
            isPaused = true;
            canShot  = true;
        }
    }

    void ReleaseAndShoot()
    {
        _player.animator.speed = 1f;

        ShootCommon();

        _player.animator.CrossFade("Idle", 0.05f, 0, 0f);

        canShot = false;
        isHolding = false;
        isPaused = false;
    }

    void CancelDraw()
    {
        _player.animator.speed = 1f;
        _player.animator.CrossFade("Idle", 0.05f, 0, 0f);

        canShot = false;
        isHolding = false;
        isPaused = false;
    }

    // ---------- Animation helpers ----------
    void CacheBowClipLength()
    {
        hasAnimState = false;
        clipLength = 0f;

        if (_player == null || _player.animator == null) return;
        var ctrl = _player.animator.runtimeAnimatorController;
        if (ctrl == null) return;

        string[] candidates = { "Bow_Draw_Side", "Bow_Draw_Front", "Bow_Draw_Back", bowStateName, "Bow_Draw" };
        float found = 0f;

        foreach (var c in ctrl.animationClips)
        {
            if (c == null) continue;
            if (System.Array.IndexOf(candidates, c.name) >= 0)
                found = Mathf.Max(found, c.length);
        }
        if (found <= 0f)
        {
            foreach (var c in ctrl.animationClips)
                found = Mathf.Max(found, c.length);
        }

        clipLength = found;
        hasAnimState = clipLength > 0f;
    }

    void ApplyAimFacingToAnimator()
    {
        if (_player?.animator == null) return;
        Vector2 aim = ComputeShotDirection2D(true); // true = for animation (may apply vertical damp in SS)
        if (aim.sqrMagnitude < 0.0001f) aim = Vector2.right;
        aim.Normalize();

        _player.animator.SetFloat("MoveX", aim.x);
        _player.animator.SetFloat("MoveY", aim.y);
        _player.animator.SetFloat("MoveSpeed", 0f);

        UpdateSpawnPointPosition(aim);
    }

    void MaybeFlipSpriteForSideScroll()
    {
        if (_player.camera2DType != Player.Camera2DType.SideScroll) return;
        if (_sr == null || !_player.autoFlipSpriteX) return;

        Vector2 dir = ComputeShotDirection2D(false); // raw
        if (Mathf.Abs(dir.x) > 0.0001f)
            _sr.flipX = dir.x < 0f;
    }

    // ---------- Defaults ----------
    void EnsureSpawnPoint()
    {
        if (spawnPoint != null) return;
        var go = new GameObject("BowSpawnPoint");
        go.transform.SetParent(_player.transform, worldPositionStays: false);
        go.transform.localPosition = new Vector3(defaultSpawnDistance, 0f, 0f);
        spawnPoint = go.transform;
    }

    void UpdateSpawnPointPosition(Vector2 aimDir)
    {
        if (spawnPoint == null) return;
        var p = _player.transform.position;
        var offset = (Vector3)(aimDir.normalized * defaultSpawnDistance);
        spawnPoint.position = p + offset;
    }

    // ---------- Shooting ----------
    void ShootInstant() => ShootCommon();

    void ShootCommon()
    {
        Vector2 dir = ComputeShotDirection2D(false); // use raw (no damp) for physics trajectory
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir = dir.normalized;

        Vector3 spawnPos = (spawnPoint != null)
            ? spawnPoint.position
            : _player.transform.position + (Vector3)(dir * defaultSpawnDistance);

        GameObject projectile = (projectilePrefab != null) ? Instantiate(projectilePrefab)
                                                           : CreateDefaultArrow2D();

        projectile.transform.position = spawnPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var rb2d = projectile.GetComponent<Rigidbody2D>() ?? projectile.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb2d.linearVelocity = dir * speed;

        if (projectile.GetComponent<Collider2D>() == null)
        {
            var box = projectile.AddComponent<BoxCollider2D>();
            box.isTrigger = false;
        }

        if (hasDestroyTime && destroyTime > 0f)
            Object.Destroy(projectile, destroyTime);

        var arrow = projectile.GetComponent<Arrow>();
        if (arrow == null) arrow = projectile.AddComponent<Arrow>();
        arrow.SetUp(damage);
    }

    // ---------- Aiming Core ----------
    /// <summary>
    /// Compute aim direction based on camera mode and selected AimMode.
    /// When forAnimator=true in SideScroll, we optionally damp vertical to help side-only animations.
    /// </summary>
    Vector2 ComputeShotDirection2D(bool forAnimator)
    {
        bool isTopDown = _player.camera2DType == Player.Camera2DType.TopDown;
        bool useMouse =
            (isTopDown  && topDownAim   == AimMode.Mouse) ||
            (!isTopDown && sideScrollAim == AimMode.Mouse);

        // If using mouse and we have a camera, aim to cursor world point
        if (useMouse && _player.camera != null)
        {
            Vector3 m = Input.mousePosition;
            Vector3 world = _player.camera.ScreenToWorldPoint(m);
            world.z = _player.transform.position.z;
            Vector2 origin = (spawnPoint ? (Vector2)spawnPoint.position : (Vector2)_player.transform.position);
            Vector2 aim = (Vector2)world - origin;
            if (aim.sqrMagnitude > 0.0001f)
            {
                if (!isTopDown && forAnimator && sideScrollVerticalDamp > 0f)
                {
                    // damp vertical for animation selection in sidescroll to avoid odd front/back picks
                    float y = Mathf.Lerp(aim.y, 0f, sideScrollVerticalDamp);
                    aim = new Vector2(aim.x, y);
                }
                return aim.normalized;
            }
        }

        // Fallback: Movement / Facing
        if (isTopDown)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector2 mv = new Vector2(h, v);
            if (mv.sqrMagnitude > 0.0001f) return mv.normalized;
            return Vector2.right;
        }
        else
        {
            float h = Input.GetAxis("Horizontal");
            if (Mathf.Abs(h) > 0.0001f) return new Vector2(Mathf.Sign(h), 0f);

            if (_sr != null) return new Vector2(_sr.flipX ? -1f : 1f, 0f);

            float sx = Mathf.Sign(_player.transform.localScale.x == 0 ? 1f : _player.transform.localScale.x);
            return new Vector2(sx, 0f);
        }
    }

    // ---------- Default arrow ----------
    GameObject CreateDefaultArrow2D()
    {
        var go = new GameObject("Arrow2D_Default");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDefaultSquareSprite(0.15f);
        sr.sortingOrder = 10;
        if (sr.sharedMaterial == null)
            sr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        var rb2d = go.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        var tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.15f;
        tr.startWidth = 0.06f;
        tr.endWidth = 0f;
        tr.minVertexDistance = 0.01f;
        tr.emitting = true;
        if (tr.sharedMaterial == null)
            tr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        var arrow = go.AddComponent<Arrow>();
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
            px / sizeWorld
        );
        sprite.name = "Arrow2D_DefaultSprite";
        return sprite;
    }
}


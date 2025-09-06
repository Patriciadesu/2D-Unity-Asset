using System.Collections;
using UnityEngine;

/// Enemy that detects Player.Instance, chases (SideScroll or TopDown),
/// and fires 3-shot predicted bursts. If no prefab is set, it builds
/// a default Arrow and attaches EnemyProjectile.
public class ProjectileEnemy : Entity
{

    [Header("Refs (auto if null)")]
    public GameObject projectilePrefab;           // If null -> will build a default arrow template
    public Transform firePoint;                   // If null -> auto-create as a child

    [Header("Detection & Movement")]
    public float detectionRadius = 6f;            // Follow when inside this
    public float shootRadius = 4f;                // Shoot when inside this
    public float moveSpeed = 2.5f;                // Chase speed

    [Header("Shooting")]
    public int shotsPerBurst = 3;                 // 3 shots each burst
    public float timeBetweenShots = 0.18f;        // gap between shots
    public float attackCooldown = 1.5f;           // cooldown after a burst
    public float projectileSpeedForLead = 12f;    // used for lead calc (match your bullet speed)
    public float bulletLifetime = 6f;             // seconds
    public float projectileDamage = 10f;

    // internals
    Rigidbody2D rb;
    float cooldownTimer;
    bool isBursting;
    Vector2 lastPlayerPos;
    Vector2 fallbackPlayerVel;

    public override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        EnsureFirePoint();
    }

    void Update()
    {
        var pInst = Player.Instance;
        var player = pInst ? pInst.transform : null;
        cooldownTimer -= Time.deltaTime;

        if (player == null)
        {
            lastPlayerPos = Vector2.zero;
            fallbackPlayerVel = Vector2.zero;
            return;
        }

        // Prefer player's rigidbody velocity; else estimate
        Vector2 playerVel = Vector2.zero;
        if (pInst.rigidbody != null)
        {
            playerVel = pInst.rigidbody.linearVelocity;
        }
        else
        {
            Vector2 now = player.position;
            if (lastPlayerPos != Vector2.zero)
            {
                var inst = (now - lastPlayerPos) / Mathf.Max(Time.deltaTime, 0.0001f);
                fallbackPlayerVel = Vector2.Lerp(fallbackPlayerVel, inst, 0.25f);
            }
            lastPlayerPos = now;
            playerVel = fallbackPlayerVel;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // Chase while detected but not yet in shoot range
        if (dist <= detectionRadius && dist > shootRadius)
        {
            if (pInst.camera2DType == Player.Camera2DType.SideScroll)
                ChaseSideScroll(player.position);
            else
                ChaseTopDown(player.position);
        }

        // Shoot when close enough
        if (dist <= shootRadius && cooldownTimer <= 0f && !isBursting)
        {
            StartCoroutine(FireBurst(player, playerVel));
        }
    }

    // -------- Movement --------
    void ChaseSideScroll(Vector3 targetPos)
    {
        Vector2 dir = new Vector2(Mathf.Sign(targetPos.x - transform.position.x), 0f);
        if (Mathf.Abs(targetPos.x - transform.position.x) < 0.05f) dir.x = 0f;

        Vector2 step = dir * moveSpeed * Time.deltaTime;
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic) rb.MovePosition(rb.position + step);
        else transform.position += (Vector3)step;
    }

    void ChaseTopDown(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector2 step = dir * moveSpeed * Time.deltaTime;
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic) rb.MovePosition(rb.position + step);
        else transform.position += (Vector3)step;
    }

    // -------- Shooting --------
    IEnumerator FireBurst(Transform player, Vector2 playerVel)
    {
        isBursting = true;
        for (int i = 0; i < shotsPerBurst; i++)
        {
            if (player == null) break;
            ShootPredicted(player, playerVel);
            if (i < shotsPerBurst - 1)
                yield return new WaitForSeconds(timeBetweenShots);
        }
        cooldownTimer = attackCooldown;
        isBursting = false;
    }

    void ShootPredicted(Transform player, Vector2 playerVel)
    {
        if (!firePoint) return;

        Vector2 shooterPos = firePoint.position;
        Vector2 toPlayer = (Vector2)player.position - shooterPos;
        float distance = toPlayer.magnitude;

        // simple lead: t ≈ distance / bulletSpeed
        float tLead = projectileSpeedForLead > 0.01f ? distance / projectileSpeedForLead : 0f;
        Vector2 predicted = (Vector2)player.position + playerVel * tLead;
        Vector2 dir = (predicted - shooterPos).sqrMagnitude > 0.0001f
            ? (predicted - shooterPos).normalized
            : toPlayer.normalized;

        // Spawn projectile (clone the template/prefab)
        GameObject proj;
        if (projectilePrefab == null)
        {
            proj = CreateDefaultArrow2D();
            proj.transform.position = firePoint.position;
        }
        else
            proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        // Ensure it has EnemyProjectile and a Rigidbody2D/Collider (if user prefab forgot them)
        var ep = proj.GetComponent<EnemyProjectile>();
        if (!ep) ep = proj.AddComponent<EnemyProjectile>();

        var rb2d = proj.GetComponent<Rigidbody2D>();
        if (!rb2d) rb2d = proj.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col2d = proj.GetComponent<Collider2D>();
        if (!col2d) col2d = proj.AddComponent<BoxCollider2D>();

        // Fire!
        ep.Setup(dir, projectileSpeedForLead, projectileDamage, bulletLifetime, this);
    }

    // -------- Utilities --------
    void EnsureFirePoint()
    {
        if (firePoint) return;
        var go = new GameObject("FirePoint");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        firePoint = go.transform;
    }

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
        col.isTrigger = true;

        var tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.15f;
        tr.startWidth = 0.06f;
        tr.endWidth = 0f;
        tr.minVertexDistance = 0.01f;
        tr.emitting = true;
        if (tr.sharedMaterial == null)
            tr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        var enemyProj = go.AddComponent<EnemyProjectile>();
        enemyProj.defaultDamage = projectileDamage;
        enemyProj.defaultSpeed = projectileSpeedForLead;
        enemyProj.defaultLifeTime = bulletLifetime;

        return go;
    }

    Sprite CreateDefaultSquareSprite(float worldSize)
    {
        int px = Mathf.Max(2, Mathf.CeilToInt(worldSize * 100f)); // assumes 100 px/unit
        var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
        var cols = new Color[px * px];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
        tex.SetPixels(cols);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRadius);
    }

}

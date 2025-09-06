using System.Collections;
using UnityEngine;

/// Enemy -> detect/chase Player.Instance and fire 3-shot predicted bursts.
/// Movement auto-switches based on Player.Instance.camera2DType.
public class EnemyProjectiile : MonoBehaviour
{
    [Header("Refs (auto if null)")]
    public GameObject projectilePrefab;             // If null, a simple runtime projectile is created per shot
    public Transform firePoint;                     // If null, auto-created as a child

    [Header("Detection & Movement")]
    public float detectionRadius = 6f;              // Follow when inside this
    public float shootRadius = 4f;                  // Shoot when inside this
    public float moveSpeed = 2.5f;                  // Chase speed

    [Header("Shooting")]
    public int shotsPerBurst = 3;                   // 3 shots each time
    public float timeBetweenShots = 0.18f;          // gap between shots in a burst
    public float attackCooldown = 1.5f;             // cooldown after a burst
    public float projectileSpeedForLead = 10f;      // used only for lead calc (match your projectile’s real speed)
    public float bulletLifetime = 6f;               // bullet auto-destroys after this time

    // internals
    Rigidbody2D rb;
    float cooldownTimer;
    bool isBursting;
    Vector2 lastPlayerPos;
    Vector2 fallbackPlayerVel;                      // for when Player.rigidbody is missing

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        EnsureFirePoint();
    }

    void Update()
    {
        var player = Player.Instance ? Player.Instance.transform : null;
        if (player == null)
        {
            cooldownTimer -= Time.deltaTime;
            lastPlayerPos = Vector2.zero;
            fallbackPlayerVel = Vector2.zero;
            return;
        }

        cooldownTimer -= Time.deltaTime;

        // Prefer the player’s rigidbody velocity; otherwise estimate
        Vector2 playerVel = Vector2.zero;
        if (Player.Instance.rigidbody != null)
        {
            playerVel = Player.Instance.rigidbody.linearVelocity;
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

        // Follow while in detection and not yet in shoot radius
        if (dist <= detectionRadius && dist > shootRadius)
        {
            if (Player.Instance.camera2DType == Player.Camera2DType.SideScroll)
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

    // ========== Movement ==========
    void ChaseSideScroll(Vector3 targetPos)
    {
        Vector2 dir = new Vector2(Mathf.Sign(targetPos.x - transform.position.x), 0f);
        if (Mathf.Abs(targetPos.x - transform.position.x) < 0.05f) dir.x = 0f;

        Vector2 step = dir * moveSpeed * Time.deltaTime;

        if (rb != null && !rb.isKinematic) rb.MovePosition(rb.position + step);
        else transform.position += (Vector3)step;
    }

    void ChaseTopDown(Vector3 targetPos)
    {
        Vector2 dir = (targetPos - transform.position);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        Vector2 step = dir * moveSpeed * Time.deltaTime;

        if (rb != null && !rb.isKinematic) rb.MovePosition(rb.position + step);
        else transform.position += (Vector3)step;
    }

    // ========== Shooting ==========
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
        if (firePoint == null) return;

        Vector2 shooterPos = firePoint.position;
        Vector2 toPlayer = (Vector2)player.position - shooterPos;
        float distance = toPlayer.magnitude;

        // Lead time ≈ time projectile travels to current player pos
        float tLead = projectileSpeedForLead > 0.01f ? distance / projectileSpeedForLead : 0f;
        Vector2 predicted = (Vector2)player.position + playerVel * tLead;
        Vector2 dir = (predicted - shooterPos).sqrMagnitude > 0.0001f
            ? (predicted - shooterPos).normalized
            : toPlayer.normalized;

        // Spawn projectile
        GameObject proj = projectilePrefab
            ? Instantiate(projectilePrefab, firePoint.position, Quaternion.identity)
            : BuildDefaultProjectile(firePoint.position, projectileSpeedForLead);

        // Ensure bullet self-destructs after time (even if prefab didn’t have it)
        var sd = proj.GetComponent<SelfDestruct2D>();
        if (!sd) sd = proj.AddComponent<SelfDestruct2D>();
        sd.lifeTime = bulletLifetime;

        // 1) If your Projectile script exists and expects direction:
        var custom = proj.GetComponent<Projectile>();
        if (custom != null)
        {
            custom.Setup(dir);
            return;
        }

        // 2) If it has Rigidbody2D, push it
        var prb = proj.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.linearVelocity = dir * projectileSpeedForLead;
            return;
        }

        // 3) Fallback default mover
        var def = proj.GetComponent<DefaultProjectile2D>();
        if (!def) def = proj.AddComponent<DefaultProjectile2D>();
        def.direction = dir;
        def.speed = projectileSpeedForLead;
        def.lifeTime = bulletLifetime; // redundancy OK
    }

    // ========== Utilities ==========
    void EnsureFirePoint()
    {
        if (firePoint) return;
        var go = new GameObject("FirePoint");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        firePoint = go.transform;
    }

    GameObject BuildDefaultProjectile(Vector3 at, float speed)
    {
        var g = new GameObject("RuntimeProjectile");
        g.transform.position = at;

        // Visual
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = MakeUnitCircleSprite();

        // Physics
        var rb2d = g.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = g.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.08f;

        // Mover
        var mover = g.AddComponent<DefaultProjectile2D>();
        mover.speed = speed;
        mover.lifeTime = bulletLifetime;

        return g;
    }

    Sprite MakeUnitCircleSprite()
    {
        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var dx = (x + 0.5f) / size * 2f - 1f;
            var dy = (y + 0.5f) / size * 2f - 1f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            tex.SetPixel(x, y, r <= 1f ? Color.white : Color.clear);
        }
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRadius);
    }
}

/// Straight mover (used only if your prefab lacks its own projectile logic)
public class DefaultProjectile2D : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 6f;
    [HideInInspector] public Vector2 direction = Vector2.right;
    float life;

    void Update()
    {
        transform.position += (Vector3)(direction.normalized * speed * Time.deltaTime);
        life += Time.deltaTime;
        if (life >= lifeTime) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Add collision filtering/damage here if desired
        Destroy(gameObject);
    }
}

/// Adds guaranteed lifetime destruction to ANY projectile prefab.
public class SelfDestruct2D : MonoBehaviour
{
    public float lifeTime = 6f;
    float t;
    void Update()
    {
        t += Time.deltaTime;
        if (t >= lifeTime) Destroy(gameObject);
    }
}

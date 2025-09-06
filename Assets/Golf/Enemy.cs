using UnityEngine;

public class Enemy : Entity
{
    [Header("Detect & Move")]
    public float detectionRadius = 15f;
    public float moveSpeed = 3f;
    public LayerMask playerLayer;              // optional; we also fallback to Player.Instance
    public bool stopWhenColliding = true;      // stop chasing while touching player

    [Header("Damage")]
    public int damage = 10;
    [Range(0f, 10f)] public float attackCooldown = 1.0f; // seconds between hits while touching
    public bool damageOnEnter = true;                    // deal one hit immediately on enter

    // runtime
    private Transform player;
    private Rigidbody2D rb;
    private bool collidingWithPlayer = false;
    private float attackTimer = 0f; // counts down to next allowed hit

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // cooldown countdown
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        // Player singleton
        player = Player.Instance ? Player.Instance.transform : player;
        if (player == null) return;

        // Layer-checked detection (if set), else distance check
        if (playerLayer.value != 0)
        {
            var hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
            if (hit == null) return;
            player = hit.transform;
        }
        else
        {
            if (Vector2.Distance(transform.position, player.position) > detectionRadius) return;
        }

        // Movement (pause if touching and requested)
        if (stopWhenColliding && collidingWithPlayer) return;

        // Chase: X-only for SideScroll; XY for TopDown
        Vector2 target;
        if (Player.Instance != null && Player.Instance.camera2DType == Player.Camera2DType.SideScroll)
            target = new Vector2(player.position.x, transform.position.y);
        else
            target = player.position;

        Vector2 next = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (rb != null && !rb.isKinematic) rb.MovePosition(next);
        else transform.position = next;
    }

    // ---------- Collision (non-trigger) ----------
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsPlayer(collision.collider)) return;
        collidingWithPlayer = true;
        TryHitOnEnter();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsPlayer(collision.collider)) return;
        TryHitDuringContact();
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (!IsPlayer(collision.collider)) return;
        collidingWithPlayer = false;
    }

    // ---------- Trigger colliders ----------
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        collidingWithPlayer = true;
        TryHitOnEnter();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        TryHitDuringContact();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        collidingWithPlayer = false;
    }

    // ---------- Helpers ----------
    bool IsPlayer(Component hit)
    {
        if (Player.Instance == null) return false;
        return hit.transform == Player.Instance.transform || hit.GetComponentInParent<Player>() != null;
    }

    void TryHitOnEnter()
    {
        if (!damageOnEnter) return;
        if (attackTimer > 0f) return; // still cooling
        DealDamageToPlayer();
        attackTimer = attackCooldown;
    }

    void TryHitDuringContact()
    {
        if (attackTimer > 0f) return;
        DealDamageToPlayer();
        attackTimer = attackCooldown;
    }

    void DealDamageToPlayer()
    {
        if (Player.Instance == null) return;
        Player.Instance.TakeDamage(damage);
        // If you want knockback direction to come from us, you can use:
        // Player.Instance.TakeDamage(damage, transform.position, Vector2.zero, false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

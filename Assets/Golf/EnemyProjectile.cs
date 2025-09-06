using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Defaults (used if Setup not called)")]
    public float defaultSpeed = 10f;
    public float defaultDamage = 10f;
    public float defaultLifeTime = 6f;

    Rigidbody2D rb;
    float speed;
    float damage;
    float lifeTime;
    float timer;
    public Component owner; // enemy that fired this

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        // Defaults (overridden by Setup)
        speed = defaultSpeed;
        damage = defaultDamage;
        lifeTime = defaultLifeTime;
    }

    void OnEnable()
    {
        timer = 0f;
    }

    // EXACTLY as you requested
    public void Setup(Vector2 dir, float speed, float damage, float lifeTime, Component owner = null)
    {
        this.speed = speed;
        this.damage = damage;
        this.lifeTime = lifeTime;
        this.owner = owner;

        // orient sprite to direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // propel
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = dir.normalized * speed;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeTime) Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Ignore hitting the owner (if same hierarchy)
        if (owner != null && col.collider.transform.IsChildOf((owner as Component).transform))
            return;

        // Damage Player (supports collider on player or children)
        var p = Player.Instance;
        if (p != null && (col.collider.transform == p.transform || col.collider.GetComponentInParent<Player>() != null))
        {
            p.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    // If any projectile prefabs use trigger colliders instead:
    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore owner
        if (owner != null && other.transform.IsChildOf((owner as Component).transform))
            return;

        var p = Player.Instance;
        if (p != null && (other.transform == p.transform || other.GetComponentInParent<Player>() != null))
        {
            p.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    

}

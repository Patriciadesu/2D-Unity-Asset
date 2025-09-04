using UnityEngine;

public class EnemyProjectiile : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float detectionRadius = 6f;
    public float attackCooldown = 1.5f;

    private Transform player;
    private float cooldownTimer = 0f;
    private bool shootAtPlayer = true;
    public float currentplayerposx;
    public float lastplayerposx;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        currentplayerposx = player.position.x;
        cooldownTimer -= Time.deltaTime;

        // ตรวจจับ Player
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, LayerMask.GetMask("Player"));
        if (hit != null)
        {
            player = hit.transform;

            if (cooldownTimer <= 0f)
            {
                Shoot();
                cooldownTimer = attackCooldown;
            }
        }
        else
        {
            player = null;
        }
        lastplayerposx = player.position.x;
    }

    void Shoot()
    {
        currentplayerposx -= lastplayerposx;
        GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile projScript = proj.GetComponent<Projectile>();

        if (shootAtPlayer)
        {
            // ลูกแรก: ยิงตรงตำแหน่ง Player
            projScript.Setup(player.position - firePoint.position);
        }
        else
        {
            if (currentplayerposx < 0)
            {
                // Player กำลังเคลื่อนที่ไปทางซ้าย
                Vector2 targetPos = new Vector2(player.position.x - 1f, player.position.y);
                projScript.Setup(targetPos - (Vector2)firePoint.position);
            }
            else if (currentplayerposx > 0)
            {
                // Player กำลังเคลื่อนที่ไปทางขวา
                Vector2 targetPos = new Vector2(player.position.x + 1f, player.position.y);
                projScript.Setup(targetPos - (Vector2)firePoint.position);
            }
            else
            {
                // Player หยุดนิ่ง: ยิงไปด้านหลังของ Player -1 X
                Vector2 targetPos = new Vector2(player.position.x - 1f, player.position.y);
                projScript.Setup(targetPos - (Vector2)firePoint.position);
            }
        }

        shootAtPlayer = !shootAtPlayer; // สลับยิง
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

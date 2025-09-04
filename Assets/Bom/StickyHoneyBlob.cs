using UnityEngine;
using System.Collections;

public class StickyHoneyBlob : MonoBehaviour
{
    [Tooltip("เวลาที่ Slow (วินาที)")]
    public float slowDuration = 2f;

    [Tooltip("คูณความเร็วตอนโดน (0.5 = ครึ่งหนึ่ง, 0.2 = ช้ามาก, 1 = ปกติ)")]
    public float slowMultiplier = 0.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ===== Player =====
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            StartCoroutine(SlowPlayer(player));
        }

        // ===== Enemy =====
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            StartCoroutine(SlowEnemy(enemy));
        }
    }

    // ---- Player ----
    private IEnumerator SlowPlayer(Player player)
    {
        float originalMultiplier = player.speedMultiplier;

        // ลดความเร็วลง
        player.speedMultiplier *= slowMultiplier;

        yield return new WaitForSeconds(slowDuration);

        // คืนค่าความเร็ว
        player.speedMultiplier = originalMultiplier;
    }

    // ---- Enemy ----
    private IEnumerator SlowEnemy(Enemy enemy)
    {
        float originalSpeed = enemy.moveSpeed;

        // ลดความเร็วลง
        enemy.moveSpeed *= slowMultiplier;

        yield return new WaitForSeconds(slowDuration);

        // คืนค่าความเร็ว
        enemy.moveSpeed = originalSpeed;
    }
}

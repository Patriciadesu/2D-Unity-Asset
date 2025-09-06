using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)] 
public class StickyHoneyBlob : ObjectEffect
{
    [Tooltip("เวลาที่ Slow (วินาที)")]
    public float slowDuration = 2f;

    [Tooltip("คูณความเร็วตอนโดน (0.5 = ครึ่งหนึ่ง, 0.2 = ช้ามาก, 1 = ปกติ)")]
    [Range(0.05f, 1f)]
    public float slowMultiplier = 0.7f;

    [Header("Trigger Child")]
    private bool ensureChildTrigger = true;
    private Vector2 triggerSize = new Vector2(1f, 1f);
    private Vector2 triggerOffset = Vector2.zero;

    private Collider2D triggerChild; 

    void OnEnable()
    {
        if (ensureChildTrigger) StartCoroutine(AddChildTriggerAfterIO());
    }


    private IEnumerator AddChildTriggerAfterIO()
    {
        yield return null;

        var t = transform.Find("StickyHoneyBlob_Trigger");
        if (t != null)
        {
            triggerChild = t.GetComponent<Collider2D>();
            if (triggerChild != null) { triggerChild.isTrigger = true; yield break; }
        }

        GameObject child = new GameObject("StickyHoneyBlob_Trigger");
        child.transform.SetParent(transform, false);


        var box = child.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = triggerSize;
        box.offset = triggerOffset;


        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null && triggerSize == Vector2.zero)
        {
            var size = sr.bounds.size;
            if (size.x > 0f && size.y > 0f) box.size = size;
        }

        triggerChild = box;
 
    }


    public override void ApplyEffect(Player player)
    {
        if (player != null) StartCoroutine(SlowPlayer(player));
    }

    public override void ApplyEffect(Collider2D playerCollider, Player player)
    {
        if (player != null) StartCoroutine(SlowPlayer(player));
        var enemy = playerCollider != null ? playerCollider.GetComponent<Enemy>() : null;
        if (enemy != null) StartCoroutine(SlowEnemy(enemy));
    }

    public override void ApplyEffect(Collision2D playerCollision, Player player)
    {
        if (player != null) StartCoroutine(SlowPlayer(player));
        var enemy = playerCollision != null ? playerCollision.gameObject.GetComponent<Enemy>() : null;
        if (enemy != null) StartCoroutine(SlowEnemy(enemy));
    }

    // เพลเย้อกากๆ
    private IEnumerator SlowPlayer(Player player)
    {
        player.speedMultiplier *= slowMultiplier;
        yield return new WaitForSeconds(slowDuration);
        if (player != null && slowMultiplier > 0f)
            player.speedMultiplier *= 1f / slowMultiplier;
    }

    // เอเนมี้โง่ๆ
    private IEnumerator SlowEnemy(Enemy enemy)
    {
        enemy.moveSpeed *= slowMultiplier;
        yield return new WaitForSeconds(slowDuration);
        if (enemy != null && slowMultiplier > 0f)
            enemy.moveSpeed *= 1f / slowMultiplier;
    }
}

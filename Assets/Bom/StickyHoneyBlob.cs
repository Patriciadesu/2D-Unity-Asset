using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class StickyHoneyBlob : ObjectEffect
{
    [Header("Slow Settings (while inside)")]
    [Range(0.05f, 1f)] public float slowMultiplier = 0.5f;
    [Tooltip("เพิ่มค่า drag ของ Rigidbody2D ผู้เล่น (0 = ไม่เพิ่ม)")]
    [Min(0f)] public float Slower = 100f;

    [Header("Trigger Child (ไม่ยุ่งคอลลิเดอร์หลัก)")]
    private bool ensureChildTrigger = true;
    private Vector2 triggerSize = new Vector2(1f, 1f);
    private Vector2 triggerOffset = Vector2.zero;

    [Header("Physics Override (กันชน/กันตก โดยไม่แตะ InteractableObject)")]
    private bool overridePhysics = true;
    private bool passThrough = true;
    private bool usePhysic = false;
    private bool useGravity = false;

    private readonly HashSet<Player> slowedPlayers = new HashSet<Player>();
    private readonly Dictionary<Player, float> originalDrags = new Dictionary<Player, float>(); 

    void OnEnable()
    {
        StartCoroutine(SetupAfterInteractable());
    }

    IEnumerator SetupAfterInteractable()
    {
        yield return null;

        var rb = GetComponent<Rigidbody2D>();
        if (overridePhysics && rb != null)
        {
            rb.bodyType = usePhysic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            rb.gravityScale = useGravity ? 1f : 0f;
        }

        if (passThrough)
        {
            foreach (var col in GetComponents<Collider2D>())
                col.isTrigger = true;
        }

        if (ensureChildTrigger) EnsureChildTrigger();
    }

    private void EnsureChildTrigger()
    {
        var t = transform.Find("StickyHoneyBlob_Trigger");
        if (t != null)
        {
            var c = t.GetComponent<Collider2D>();
            if (c != null) { c.isTrigger = true; return; }
        }

        var child = new GameObject("StickyHoneyBlob_Trigger");
        child.transform.SetParent(transform, false);

        var box = child.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = triggerSize;
        box.offset = triggerOffset;

        if (triggerSize == Vector2.zero)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr && sr.sprite) box.size = sr.bounds.size;
        }
    }

 
    public override void ApplyEffect(Player player)                              => TryApply(player);
    public override void ApplyEffect(Collider2D playerCollider, Player player)   => TryApply(player);
    public override void ApplyEffect(Collision2D playerCollision, Player player) => TryApply(player);


    void OnTriggerExit2D(Collider2D other)
    {
        var p = FindPlayerOn(other.gameObject);
        if (p != null) TryRemove(p);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        var p = FindPlayerOn(collision.gameObject);
        if (p != null) TryRemove(p);
    }

    void OnDisable()
    {
        foreach (var p in slowedPlayers)
            ResetPlayerDrag(p);
        slowedPlayers.Clear();
        originalDrags.Clear();
    }


    private void TryApply(Player player)
    {
        if (player == null) return;
        if (slowedPlayers.Add(player))
        {
            player.speedMultiplier *= slowMultiplier;


            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                if (!originalDrags.ContainsKey(player))
                    originalDrags[player] = rb.linearDamping;
                rb.linearDamping = rb.linearDamping + Slower;
            }
        }
    }

    private void TryRemove(Player player)
    {
        if (player == null) return;
        if (slowedPlayers.Remove(player))
        {
            if (slowMultiplier > 0f)
                player.speedMultiplier *= 1f / slowMultiplier;

            ResetPlayerDrag(player);
        }
    }

    private void ResetPlayerDrag(Player player)
    {
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && originalDrags.TryGetValue(player, out float original))
        {
            rb.linearDamping = original;
            originalDrags.Remove(player);
        }
    }

    private Player FindPlayerOn(GameObject go)
    {
        var p = go.GetComponent<Player>();
        if (p != null) return p;

        if (go.CompareTag("Player"))
        {
            p = go.GetComponentInParent<Player>();
            if (p != null) return p;
            p = go.GetComponentInChildren<Player>();
            if (p != null) return p;
        }
        return null;
    }
}

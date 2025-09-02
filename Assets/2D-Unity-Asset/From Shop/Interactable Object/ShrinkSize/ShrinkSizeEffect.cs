using System.Collections;
using UnityEngine;

public class ShrinkSizeEffect : ObjectEffect
{
    [Header("Shrink Size Settings")]
    [SerializeField] private float sizeMultiplier = 0.5f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private bool isPermanent = false;

    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 2f;
    private float lastActivationTime = -999f;

    // Optional safety: if true we also scale CapsuleCollider2D (matches your 3D intent)
    // If you see “double shrink” due to transform scale, set this to false.
    [SerializeField] private bool adjustCollider = true;

    public override void ApplyEffect(Player player)
    {
        if (player == null) return;

        // Cooldown gate
        float sinceLast = Time.time - lastActivationTime;
        if (sinceLast < cooldownTime)
        {
            Debug.Log($"{name} cooldown: {cooldownTime - sinceLast:F1}s remaining");
            return;
        }

        Transform t = player.transform;
        Vector3 originalScale = t.localScale;
        Vector3 targetScale = originalScale * sizeMultiplier;

        // Capture collider (2D)
        var capsule = player._capsule2D; // from the 2D Player we created
        Vector2 originalSize = Vector2.zero;
        Vector2 originalOffset = Vector2.zero;

        if (capsule != null && adjustCollider)
        {
            originalSize = capsule.size;
            originalOffset = capsule.offset;
        }

        // Apply scaling
        t.localScale = targetScale;

        if (capsule != null && adjustCollider)
        {
            capsule.size = originalSize * sizeMultiplier;
            capsule.offset = originalOffset * sizeMultiplier;
        }

        lastActivationTime = Time.time;
        Debug.Log($"{name}: shrank {player.gameObject.name} (x{sizeMultiplier})");

        // Schedule revert if temporary
        if (!isPermanent && duration > 0f)
        {
            StartCoroutine(RevertSizeAfterDelay(player, originalScale, duration,
                capsule, originalSize, originalOffset));
        }
    }

    private IEnumerator RevertSizeAfterDelay(
        Player player,
        Vector3 originalScale,
        float delay,
        CapsuleCollider2D capsule,
        Vector2 originalSize,
        Vector2 originalOffset)
    {
        yield return new WaitForSeconds(delay);

        if (player == null) yield break;

        // Revert transform
        var t = player.transform;
        if (t != null) t.localScale = originalScale;

        // Revert collider (if still exists)
        if (capsule != null && adjustCollider)
        {
            capsule.size = originalSize;
            capsule.offset = originalOffset;
        }

        Debug.Log($"Player size{(adjustCollider ? " and collider" : "")} reverted after {delay} seconds");
    }
}

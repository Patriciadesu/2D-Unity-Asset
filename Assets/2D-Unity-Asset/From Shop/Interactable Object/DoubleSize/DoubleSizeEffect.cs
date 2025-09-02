using System.Collections;
using UnityEngine;

public class DoubleSizeEffect : ObjectEffect
{
    [Header("Double Size Settings")]
    [SerializeField] private float sizeMultiplier = 2f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private bool isPermanent = false;

    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 2f;
    private float lastActivationTime = -999f;

    // If your project already relies on Transform scale only, set this false to avoid double-scaling the collider
    [SerializeField] private bool adjustCollider = true;

    public override void ApplyEffect(Player player)
    {
        if (player == null) return;

        // Cooldown gate
        float since = Time.time - lastActivationTime;
        if (since < cooldownTime)
        {
            Debug.Log($"{name} is on cooldown for {cooldownTime - since:F1}s");
            return;
        }

        Transform t = player.transform;
        Vector3 originalScale = t.localScale;
        Vector3 targetScale = originalScale * sizeMultiplier;

        // Capture CapsuleCollider2D values (2D)
        var capsule = player._capsule2D;
        Vector2 originalSize = Vector2.zero;
        Vector2 originalOffset = Vector2.zero;

        if (capsule != null && adjustCollider)
        {
            originalSize = capsule.size;
            originalOffset = capsule.offset;
        }

        // Apply scale
        t.localScale = targetScale;

        // Scale collider to match visuals
        if (capsule != null && adjustCollider)
        {
            capsule.size = originalSize * sizeMultiplier;
            capsule.offset = originalOffset * sizeMultiplier;
        }

        lastActivationTime = Time.time;
        Debug.Log($"{name} doubled {player.gameObject.name}'s size (x{sizeMultiplier})");

        // Revert if temporary
        if (!isPermanent && duration > 0f)
        {
            StartCoroutine(RevertAfterDelay(player, originalScale, duration,
                capsule, originalSize, originalOffset));
        }
    }

    private IEnumerator RevertAfterDelay(
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

        Debug.Log($"Reverted size{(adjustCollider ? " and collider" : "")} after {delay} seconds");
    }
}

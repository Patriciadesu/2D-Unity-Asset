using System.Collections;
using UnityEngine;

public class DoubleSizeEffect : ObjectEffect
{
    [Header("Double Size Settings")]
    [SerializeField] private float sizeMultiplier = 2f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private bool isPermanent = false;

    [Header("Cooldown Settings")]
    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 2f;
    private float lastActivationTime = -999f;
    private bool isActive = false;

    // If your project already relies on Transform scale only, set this false to avoid double-scaling the collider
    [SerializeField] private bool adjustCollider = true;

    [Header("Camera Zoom Settings")]
    [SerializeField] private bool zoomCamera = true;
    [SerializeField] private float zoomMultiplier = 1.5f; // Zoom OUT
    [SerializeField] private float zoomDuration = 0.5f;

    public override void ApplyEffect(Player player)
    {
        if (player == null || isActive) return;

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
            isActive = true;
            StartCoroutine(RevertAfterDelay(player, originalScale, duration,
                capsule, originalSize, originalOffset));
        }
    }

    public override void ApplyEffect(Player2DController player)
    {
        if (player == null || isActive) return;

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
        var capsule = player.GetComponent<CapsuleCollider2D>();
        Vector2 originalSize = Vector2.zero;
        Vector2 originalOffset = Vector2.zero;

        if (capsule != null && adjustCollider)
        {
            originalSize = capsule.size;
            originalOffset = capsule.offset;
        }

        // Apply scale
        t.localScale = targetScale;

        // For Player2DController, we rely on Transform scaling.
        /*
        if (capsule != null && adjustCollider)
        {
            capsule.size = originalSize * sizeMultiplier;
            capsule.offset = originalOffset * sizeMultiplier;
        }
        */

        // Handle Camera Zoom
        float originalCamSize = 0f;
        Camera cam = Camera.main;
        if (zoomCamera && cam != null)
        {
            originalCamSize = cam.orthographicSize;
            float targetCamSize = originalCamSize * zoomMultiplier;
            player.StartCoroutine(SmoothZoom(cam, targetCamSize, zoomDuration));
        }

        lastActivationTime = Time.time;
        Debug.Log($"{name} doubled {player.gameObject.name}'s size (x{sizeMultiplier})");

        // Revert if temporary
        if (!isPermanent && duration > 0f)
        {
            isActive = true;
            player.StartCoroutine(RevertAfterDelay(player, originalScale, duration,
                capsule, originalSize, originalOffset, cam, originalCamSize));
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
        isActive = false;
    }

    private IEnumerator RevertAfterDelay(
        Player2DController player,
        Vector3 originalScale,
        float delay,
        CapsuleCollider2D capsule,
        Vector2 originalSize,
        Vector2 originalOffset,
        Camera cam,
        float originalCamSize)
    {
        yield return new WaitForSeconds(delay);

        if (player == null) yield break;

        // Revert transform
        var t = player.transform;
        if (t != null) t.localScale = originalScale;

        // Revert collider (if still exists)
        if (capsule != null && adjustCollider)
        {
            // Logic commented out for Player2D as per fix
            // capsule.size = originalSize;
            // capsule.offset = originalOffset;
        }

        // Revert Camera
        if (zoomCamera && cam != null && originalCamSize > 0)
        {
            player.StartCoroutine(SmoothZoom(cam, originalCamSize, zoomDuration));
        }

        Debug.Log($"Reverted size{(adjustCollider ? " and collider" : "")} after {delay} seconds (Player2DController)");
        isActive = false;
    }

    private IEnumerator SmoothZoom(Camera cam, float targetSize, float duration)
    {
        if (cam == null) yield break;

        float startSize = cam.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (cam == null) yield break;
            elapsed += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
            yield return null;
        }
        if (cam != null) cam.orthographicSize = targetSize;
    }
}

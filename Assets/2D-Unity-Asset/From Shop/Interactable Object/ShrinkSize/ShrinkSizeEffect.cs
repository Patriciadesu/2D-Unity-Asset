using System.Collections;
using UnityEngine;

public class ShrinkSizeEffect : ObjectEffect
{
    [Header("Shrink Size Settings")]
    [SerializeField] private float sizeMultiplier = 0.5f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private bool isPermanent = false;

    [Header("Cooldown Settings")]
    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 2f;
    private float lastActivationTime = -999f;
    private bool isActive = false;

    // Optional safety: if true we also scale CapsuleCollider2D (matches your 3D intent)
    // If you see “double shrink” due to transform scale, set this to false.
    [SerializeField] private bool adjustCollider = true;

    [Header("Camera Zoom Settings")]
    [SerializeField] private bool zoomCamera = true;
    [SerializeField] private float zoomMultiplier = 0.5f; // Zoom IN
    [SerializeField] private float zoomDuration = 0.5f;

    public override void ApplyEffect(Player player)
    {
        if (player == null || isActive) return;

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

    public override void ApplyEffect(Player2DController player)
    {
        if (player == null || isActive) return;

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
        var capsule = player.GetComponent<CapsuleCollider2D>();
        Vector2 originalSize = Vector2.zero;
        Vector2 originalOffset = Vector2.zero;

        if (capsule != null && adjustCollider)
        {
            originalSize = capsule.size;
            originalOffset = capsule.offset;
        }

        // Apply scaling
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
        Debug.Log($"{name}: shrank {player.gameObject.name} (x{sizeMultiplier})");

        // Schedule revert if temporary
        // Schedule revert if temporary
        if (!isPermanent && duration > 0f)
        {
            isActive = true;
            player.StartCoroutine(RevertSizeAfterDelay(player, originalScale, duration,
                capsule, originalSize, originalOffset, cam, originalCamSize));
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
        isActive = false;
    }

    private IEnumerator RevertSizeAfterDelay(
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
            // For Player2D we previously commented this out, but if we need to revert specialized logic:
            // capsule.size = originalSize;
            // capsule.offset = originalOffset;
        }

        // Revert Camera
        if (zoomCamera && cam != null && originalCamSize > 0)
        {
            player.StartCoroutine(SmoothZoom(cam, originalCamSize, zoomDuration));
        }

        Debug.Log($"Player2DController size{(adjustCollider ? " and collider" : "")} reverted after {delay} seconds");
        isActive = false;
    }

    private IEnumerator SmoothZoom(Camera cam, float targetSize, float duration)
    {
        if (cam == null) yield break;
        
        float startSize = cam.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Check if camera still exists
            if (cam == null) yield break;

            elapsed += Time.deltaTime;
            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
            yield return null;
        }
        if (cam != null) cam.orthographicSize = targetSize;
    }
}

using System.Collections;
using UnityEngine;

public class CameraShakeEffect : ObjectEffect
{
    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 1f;
    [SerializeField] private AnimationCurve shakeCurve = null;

    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 1f;
    private float lastActivationTime = -999f;

    // Optional: jitter on Z as well (usually keep false for 2D)
    [SerializeField] private bool allowZJitter = false;

    private const string PivotName = "__CamShakePivot__";

    private void Reset()
    {
        if (shakeCurve == null)
            shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    }

    public override void ApplyEffect(Player player)
    {
        if (player == null || player.camera == null) return;

        // Cooldown
        float since = Time.time - lastActivationTime;
        if (since < cooldownTime)
        {
            Debug.Log($"{name} is on cooldown for {cooldownTime - since:F1}s");
            return;
        }
        lastActivationTime = Time.time;

        // We’ll shake a pivot parent above the *actual* Camera so Cinemachine won’t fight us
        var cam = player.camera;
        var pivot = GetOrCreatePivot(cam.transform);

        player.StartCoroutine(ShakeRoutine(pivot));
    }

    private Transform GetOrCreatePivot(Transform cameraTransform)
    {
        // If camera is already under our named pivot, reuse it
        if (cameraTransform.parent != null && cameraTransform.parent.name == PivotName)
            return cameraTransform.parent;

        // Otherwise create a pivot and reparent the camera under it (keeping world pose)
        var pivotGO = new GameObject(PivotName);
        var pivot = pivotGO.transform;

        // Match camera world transform
        pivot.position = cameraTransform.position;
        pivot.rotation = cameraTransform.rotation;

        // Preserve current parent chain
        var originalParent = cameraTransform.parent;
        pivot.SetParent(originalParent, worldPositionStays: true);

        // Reparent camera to pivot
        cameraTransform.SetParent(pivot, worldPositionStays: true);

        return pivot;
    }

    private IEnumerator ShakeRoutine(Transform pivot)
    {
        if (shakeCurve == null)
            shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        Vector3 originalLocalPos = pivot.localPosition;
        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / shakeDuration);
            float strength = shakeCurve.Evaluate(norm) * shakeIntensity;

            // Random offset in XY (and Z if allowed)
            float ox = Random.Range(-1f, 1f) * strength;
            float oy = Random.Range(-1f, 1f) * strength;
            float oz = allowZJitter ? Random.Range(-1f, 1f) * strength * 0.25f : 0f;

            pivot.localPosition = originalLocalPos + new Vector3(ox, oy, oz);
            yield return null;
        }

        // Reset
        pivot.localPosition = originalLocalPos;
    }
}

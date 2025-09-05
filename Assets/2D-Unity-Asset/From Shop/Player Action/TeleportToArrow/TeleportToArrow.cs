using NaughtyAttributes;
using UnityEngine;

public class TeleportToArrow : PlayerExtension
{
    [Header("Teleport Settings")]
    public KeyCode teleportKey = KeyCode.Q;   // key to trigger teleport
    public float yOffset = 0.35f;             // adjust to avoid sinking into ground

    void Update()
    {
        if (Input.GetKeyDown(teleportKey))
        {
            Teleport();
        }
    }

    void Teleport()
    {
        // Find first active Arrow in scene
        Arrow arrow = GameObject.FindFirstObjectByType<Arrow>();
        if (arrow == null)
        {
            Debug.LogWarning("[TeleportToArrow] No Arrow found in the scene.");
            return;
        }

        // Teleport player
        Vector3 pos = arrow.transform.position;
        pos.y += yOffset;
        _player.transform.position = pos;

        Debug.Log($"[TeleportToArrow] Teleported player to {arrow.gameObject.name}");
    }
}

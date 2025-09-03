using UnityEngine;

public class ShadowFollow : MonoBehaviour
{
    public Transform target;       // Player to follow
    public float followSpeed = 5f; // How fast the shadow moves
    public float followDistance = 2f; // Distance to maintain behind the player

    void Update()
    {
        if (target == null) return;

        // Position to follow (behind the player)
        Vector3 followPos = target.position - target.forward * followDistance;

        // Smooth movement towards the follow position
        transform.position = Vector3.Lerp(transform.position, followPos, followSpeed * Time.deltaTime);

        // Match rotation with player
        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, followSpeed * Time.deltaTime);
    }
}
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 0.5f;
    Transform player; // Assign in Inspector
    public float followThreshold = 1.0f;

    private void Start()
    {
        player = FindAnyObjectByType<Player>().transform;

    }

    void Update()
    {
        Vector3 displacement = player.position - transform.position;

        if (displacement.magnitude > followThreshold)
        {
            Vector3 direction = displacement.normalized;
            transform.position += direction * speed * Time.deltaTime;
        }
    }
}

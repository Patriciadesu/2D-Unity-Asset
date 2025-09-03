using UnityEngine;

public class JellyPlatform : ObjectEffect
{
    public float minForce = 5f;
    public float maxForce = 10f;
    public float minAngleDeg = 45f;
    public float maxAngleDeg = 135f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        Rigidbody2D rb = collision.rigidbody;
        if (rb == null) return;

        // Generate random angle between min and max
        float angleDeg = Random.Range(minAngleDeg, maxAngleDeg);
        float angleRad = angleDeg * Mathf.Deg2Rad;

        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        // Randomize force magnitude
        float force = Random.Range(minForce, maxForce);

        rb.linearVelocity = Vector2.zero; // Optional: reset velocity
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }
}

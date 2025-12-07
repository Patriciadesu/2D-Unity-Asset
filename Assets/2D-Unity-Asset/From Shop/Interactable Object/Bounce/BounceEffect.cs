using UnityEngine;

public class BounceEffect : ObjectEffect
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private Vector3 bounceDirection = Vector3.up;
    [SerializeField] private bool useRandomDirection = false;
    [SerializeField] private float randomBounceStrength = 5f;
    
    
    public override void ApplyEffect(Player player)
    {
        if (player != null)
        {
            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                applyForce(playerRigidbody);
            }
        }
    }

    public override void ApplyEffect(Player2DController player)
    {
        if (player != null)
        {
            Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();
            if (playerRigidbody != null)
            {
                // Calculate direction
                Vector2 finalBounceDirection = bounceDirection.normalized;

                if (useRandomDirection)
                {
                    // Random 2D direction (X and Y components usually, or just angle)
                    // Assuming X/Y plane for 2D
                    Vector2 randomDir = new Vector2(
                        Random.Range(-1f, 1f),
                        Random.Range(0.5f, 1f) // Usually bounce up-ish? Or full random? 
                                                // Existing 3D code did (Range, 0, Range) which is X/Z flat random. 
                                                // For 2D side scroll, random usually means slight angle variation.
                    ).normalized;
                    
                    // Let's stick to the inspector values. 
                    // If 2D side scroller, Z is ignored. X/Y are used.
                    // The 3D code was weird (randomBounceStrength was used to multiply random vector then added).
                     Vector2 randomHorizontal = new Vector2(
                        Random.Range(-1f, 1f),
                        Random.Range(-1f, 1f)
                    ).normalized * randomBounceStrength;
                     
                     finalBounceDirection = ((Vector2)bounceDirection + randomHorizontal).normalized;
                }

                playerRigidbody.linearVelocity = Vector2.zero; // Reset velocity
                playerRigidbody.AddForce(finalBounceDirection * bounceForce, ForceMode2D.Impulse);
                
                Debug.Log($"{gameObject.name} triggered bounce effect (2D) - {player.gameObject.name} bounced!");
            }
        }
    }

    private void applyForce(Rigidbody rb)
    {
        Vector3 finalBounceDirection = bounceDirection.normalized;
        if (useRandomDirection)
        {
             Vector3 randomHorizontal = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)
            ).normalized * randomBounceStrength;
            finalBounceDirection = (bounceDirection + randomHorizontal).normalized;
        }
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(finalBounceDirection * bounceForce, ForceMode.Impulse);
        Debug.Log($"{gameObject.name} triggered bounce effect - {rb.gameObject.name} bounced!");
    }
}

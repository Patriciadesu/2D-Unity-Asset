using UnityEngine;

public class BounceEffect : ObjectEffect
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private Vector3 bounceDirection = Vector3.up;
    [SerializeField] private bool useRandomDirection = false;
    [SerializeField] private float randomBounceStrength = 5f;
    
    
    public override void ApplyEffect( Player player)
    {
        if (player != null)
        {
            Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                Vector3 finalBounceDirection = bounceDirection.normalized;
                
                if (useRandomDirection)
                {
                    // Add random horizontal direction
                    Vector3 randomHorizontal = new Vector3(
                        Random.Range(-1f, 1f),
                        0f,
                        Random.Range(-1f, 1f)
                    ).normalized * randomBounceStrength;
                    
                    finalBounceDirection = (bounceDirection + randomHorizontal).normalized;
                }
                
                // Apply bounce force
                playerRigidbody.linearVelocity = Vector3.zero; // Reset current velocity
                playerRigidbody.AddForce(finalBounceDirection * bounceForce, ForceMode.Impulse);
                
                Debug.Log($"{gameObject.name} triggered bounce effect - {player.gameObject.name} bounced with force {bounceForce}!");
            }
        }
    }
}

using UnityEngine;

/// <summary>
/// This component creates a "wind tunnel" effect that applies a continuous force
/// to any Rigidbody2D that enters its trigger collider.
/// </summary>
[RequireComponent(typeof(Collider2D))] // For 2D physics
public class WindTunnel : MonoBehaviour
{
    [Tooltip("The direction the wind is blowing. This will be normalized automatically.")]
    public Vector2 windDirection = Vector2.right;

    [Tooltip("The strength of the wind force applied to objects.")]
    public float windStrength = 10.0f;

    [Tooltip("For 2D, affects how the force is applied (e.g., as a constant force or an impulse).")]
    public ForceMode2D forceMode2D = ForceMode2D.Force;


    void Start()
    {
        // Ensure the wind direction is a unit vector for consistent force application
        windDirection.Normalize();

        // Make sure the attached collider is set to be a trigger
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            col2D.isTrigger = true;
        }
    }

    /// <summary>
    /// This method is called continuously for every Collider2D that is touching the trigger.
    /// It's used for applying force in 2D.
    /// </summary>
    /// <param name="other">The other Collider2D involved in this collision.</param>
    void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D rb2d = other.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            // Apply the wind force to the Rigidbody2D
            rb2d.AddForce(windDirection * windStrength, forceMode2D);
        }
    }
}


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnhancedFragmentBounce : MonoBehaviour
{
    [HideInInspector] public float bounceForce = 0.5f;
    [HideInInspector] public Rigidbody fragmentRb;
    private int bounceCount = 0;
    private const int maxBounces = 5;

    private void OnCollisionEnter(Collision collision)
    {
        if (fragmentRb != null && collision.contactCount > 0 && bounceCount < maxBounces)
        {
            bounceCount++;
            ContactPoint contact = collision.contacts[0];
            Vector3 bounceDirection = Vector3.Reflect(fragmentRb.linearVelocity.normalized, contact.normal);
            float currentSpeed = fragmentRb.linearVelocity.magnitude;
            float dampening = Mathf.Pow(0.8f, bounceCount);
            float bounceSpeed = currentSpeed * bounceForce * dampening;
            fragmentRb.linearVelocity = bounceDirection * bounceSpeed;

            if (bounceSpeed > 0.5f)
            {
                Vector3 randomSpin = Random.insideUnitSphere * bounceSpeed * 0.3f;
                fragmentRb.AddTorque(randomSpin, ForceMode.Impulse);
            }
        }
    }
}
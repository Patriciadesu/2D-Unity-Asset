using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomGravity : MonoBehaviour
{
    [HideInInspector] public float gravityMultiplier = 1f;
    private Rigidbody rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }
}
using UnityEngine;

public class BalloonInflate : MonoBehaviour
{
    private Rigidbody2D rb;
    public KeyCode inflateKey; 
    public float floatForce = 5f;           
    public float gravityScaleNormal = 1f;   
    public float gravityScaleFloat = 0.2f;    
    private bool isFloating = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScaleNormal;
    }

    void Update()
    {
        if (Input.GetKeyDown(inflateKey))
        {
            StartFloat();
        }

        if (Input.GetKeyUp(inflateKey))
        {
            StopFloat();
        }

        if (isFloating)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, floatForce);
        }
    }

    void StartFloat()
    {
        isFloating = true;
        rb.gravityScale = gravityScaleFloat;
    }

    void StopFloat()
    {
        isFloating = false;
        rb.gravityScale = gravityScaleNormal;
    }
}

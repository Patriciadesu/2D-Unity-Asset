using UnityEngine;

public class BalloonInflate : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Input")]
    public KeyCode inflateKey = KeyCode.Space;

    [Header("Float Settings")]
    public float floatForce = 8f;        
    public float gravityScaleNormal = 0.15f;
    public float gravityScaleFloat = 0f; 

    private float originalFallMult = 2.5f;
    private float originalGravMult = 2.5f;

    private bool isFloating = false;
    private Player player; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = Player.Instance; 
    }

    void Start()
    {
        if (rb != null) rb.gravityScale = gravityScaleNormal;

        if (player != null)
        {
            originalFallMult = player.fallMultiplier;
            originalGravMult = player.gravityMultiplier;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(inflateKey)) StartFloat();
        if (Input.GetKeyUp(inflateKey))   StopFloat();
    }

    void FixedUpdate()
    {
        if (isFloating && rb != null)
        {
            rb.AddForce(Vector2.up * floatForce, ForceMode2D.Force);
        }
    }

    void StartFloat()
    {
        isFloating = true;

        if (rb != null)
        {
            rb.gravityScale = gravityScaleFloat;

            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        if (player != null)
        {
            originalFallMult = player.fallMultiplier;
            originalGravMult = player.gravityMultiplier;

            player.fallMultiplier = 1f;
            player.gravityMultiplier = 1f;
        }
    }

    void StopFloat()
    {
        isFloating = false;

        if (rb != null)
            rb.gravityScale = gravityScaleNormal;

        if (player != null)
        {
            player.fallMultiplier = originalFallMult;
            player.gravityMultiplier = originalGravMult;
        }
    }
}

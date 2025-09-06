using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class BalloonFloatRuntime : MonoBehaviour
{
    private Rigidbody2D rb;
    private Player player;

    private KeyCode key = KeyCode.Space;
    private float force = 8f;
    private float gravNormal = 0.15f;
    private float gravFloat  = 0.1f;   


    [Header("Float Limits")]
    [SerializeField] private float maxUpSpeed = 6f; 
    [SerializeField] private float floatDrag  = 1f; 


    private bool isFloating = false;
    private float originalFallMult = 2.5f;
    private float originalGravMult = 2.5f;

    public void Configure(KeyCode inflateKey, float floatForce, float gravityScaleNormal, float gravityScaleFloat)
    {
        key        = inflateKey;
        force      = Mathf.Max(0f, floatForce);
        gravNormal = Mathf.Max(0f, gravityScaleNormal);
        gravFloat  = Mathf.Max(0f, gravityScaleFloat);

        if (rb != null && !isFloating)
            rb.gravityScale = gravNormal;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;
        rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void Start()
    {
        if (rb) rb.gravityScale = gravNormal;

        if (player)
        {
            originalFallMult = player.fallMultiplier;
            originalGravMult = player.gravityMultiplier;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(key)) StartFloat();
        if (Input.GetKeyUp(key))   StopFloat();
    }

    void FixedUpdate()
    {
        if (!isFloating || rb == null) return;


        if (rb.linearVelocity.y < maxUpSpeed)
            rb.AddForce(Vector2.up * force, ForceMode2D.Force);


        if (rb.linearVelocity.y > maxUpSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxUpSpeed);
    }

    void StartFloat()
    {
        isFloating = true;

        if (rb)
        {
            rb.gravityScale = gravFloat;  
            rb.drag = floatDrag;           

            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }

        if (player)
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

        if (rb)
        {
            rb.gravityScale = gravNormal;
            rb.drag = 0f;           
        }

        if (player)
        {
            player.fallMultiplier = originalFallMult;
            player.gravityMultiplier = originalGravMult;
        }
    }
}

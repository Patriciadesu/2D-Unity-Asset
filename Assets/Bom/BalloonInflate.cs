using UnityEngine;

public class BalloonInflate : PlayerExtension
{
    [Header("Input")]
    public KeyCode inflateKey = KeyCode.Space;

    [Header("Float Settings")]
    [Min(0f)] public float floatForce = 15f;  
    [Min(0f)] public float gravityScaleNormal = 2f;
    [Min(0f)] public float gravityScaleFloat  = 0f; 

    [Header("Float Limits")]
    [Min(0f)] public float maxUpSpeed = 100f;   
    [Min(0f)] public float floatDrag  = 0f; 

    [Header("Activation")]
    public bool activeAlways = true;           

    private Rigidbody2D rb;
    private bool isFloating = false;
    private float originalFallMult = 2.5f;
    private float originalGravMult = 2.5f;


    public override void OnStart(Player player)
    {
        base.OnStart(player);

        rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[BalloonInflate] Player ต้องมี Rigidbody2D");
            enabled = false;
            return;
        }

        rb.freezeRotation = true;
        rb.interpolation  = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;


        rb.gravityScale = gravityScaleNormal;


        originalFallMult = _player.fallMultiplier;
        originalGravMult = _player.gravityMultiplier;
    }

    void Update()
    {
        if (!activeAlways) return;
        if (_player == null || rb == null) return;

        if (Input.GetKeyDown(inflateKey)) StartFloat();
        if (Input.GetKeyUp(inflateKey))   StopFloat();
    }

    void FixedUpdate()
    {
        if (!activeAlways) return;
        if (!isFloating || rb == null) return;

        if (rb.linearVelocity.y < maxUpSpeed)
            rb.AddForce(Vector2.up * floatForce, ForceMode2D.Force);

        if (rb.linearVelocity.y > maxUpSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxUpSpeed);
    }

    private void StartFloat()
    {
        isFloating = true;


        rb.gravityScale = gravityScaleFloat;
        rb.drag = floatDrag;

        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

 
        originalFallMult = _player.fallMultiplier;
        originalGravMult = _player.gravityMultiplier;
        _player.fallMultiplier   = 1f;
        _player.gravityMultiplier = 1f;
    }

    private void StopFloat()
    {
        isFloating = false;


        rb.gravityScale = gravityScaleNormal;
        rb.drag = 0f;

 
        _player.fallMultiplier   = originalFallMult;
        _player.gravityMultiplier = originalGravMult;
    }


    public void SetActiveByZone(bool active)
    {
        activeAlways = active;
        if (!active) StopFloat();
    }
}

using UnityEngine;

public class BalloonInflate : ObjectEffect
{
    [Header("Input")]
    public KeyCode inflateKey = KeyCode.Space;

    [Header("Float Settings")]
    [Min(0f)] public float floatForce = 10f;  
    [Min(0f)] public float gravityScaleNormal = 1f;
    [Min(0f)] public float gravityScaleFloat = 0.1f;
    

    public override void ApplyEffect(Player player)
    {
        EnsureRuntimeOn(player);
    }

    public override void ApplyEffect(Collider2D playerCollider, Player player)
    {
        EnsureRuntimeOn(player);
    }

    public override void ApplyEffect(Collision2D playerCollision, Player player)
    {
        EnsureRuntimeOn(player);
    }

    private void EnsureRuntimeOn(Player player)
    {
        if (player == null) return;
    
        var runtime = player.GetComponent<BalloonFloatRuntime>();
        if (runtime == null) runtime = player.gameObject.AddComponent<BalloonFloatRuntime>();

        runtime.Configure(inflateKey, floatForce, gravityScaleNormal, gravityScaleFloat);
    }
}
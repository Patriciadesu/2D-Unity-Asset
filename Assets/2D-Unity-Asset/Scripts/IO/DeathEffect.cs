using UnityEngine;

public class DeathEffect : ObjectEffect
{

    
    public override void ApplyEffect( Player player)
    {
        if (player != null)
        {
            player.Respawn();
            Debug.Log($"{gameObject.name} triggered death effect - {player.gameObject.name} respawned!");
        }
    }
}
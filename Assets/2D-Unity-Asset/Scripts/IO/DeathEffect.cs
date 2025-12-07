using UnityEngine;

public class DeathEffect : ObjectEffect
{
    public override void ApplyEffect(Player player)
    {
        if (player != null)
        {
            player.Respawn();
            Debug.Log($"{gameObject.name} triggered death effect - {player.gameObject.name} respawned!");
            //ตอนชน หรือ เข้าไปใน Trigger จะทำ Effect นี้นะจ๊ะ
        }
    }

    public override void ApplyEffect(Player2DController player)
    {
        if (player != null)
        {
            player.Respawn();
            Debug.Log($"{gameObject.name} triggered death effect - {player.gameObject.name} respawned (Player2DController)!");
        }
    }
}
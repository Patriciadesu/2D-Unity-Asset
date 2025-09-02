using UnityEngine;

[System.Serializable]
public class IdleState : BossState
{
    public IdleState(Boss bossInstance) : base("Idle", bossInstance) { }
    public override void Enter()
    {
        if (animator != null) animator.SetTrigger("Idle");
    }
}

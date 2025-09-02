using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Timeline;

/// <summary>
/// Base class for all graph conditions. Inherit this and override StartTrackCondition / StopTrackCondition.
/// </summary>
[System.Serializable]
public class Condition
{
    [HideInInspector]public UnityEvent onConditionMet = new UnityEvent();
    protected Boss boss;
    public bool allowStateInterruption = true; // if false, the condition will not be triggered if the current state is not in the Exit stage
    public virtual void Bind(Boss bossRef) { boss = bossRef; }
    public virtual void StartTrackCondition() { }
    public virtual void StopTrackCondition()  { }

    protected void Raise() { onConditionMet?.Invoke(); }
}

public class PlayerInSightCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerInSight.AddListener( Raise);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerInSight.RemoveListener( Raise);
    }

     
}

public class PlayerOutOfSightCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerOutOfSight.AddListener( Raise);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerOutOfSight.RemoveListener( Raise);
    }

     
}

public class PlayerInAttackRangeCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerInAttackRange.AddListener( Raise);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerInAttackRange.RemoveListener( Raise);
    }

     
}
public class PlayerOutAttackRangeCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerOutOfAttackRange.AddListener( Raise);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onPlayerOutOfAttackRange.RemoveListener( Raise);
    }

     
}
public class HealthBelowCondition : Condition
{
    [Tooltip("Trigger condition when health <= this value.")]
    public float threshold = 0.3f;

    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onHealthChanged.AddListener(OnHealth);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onHealthChanged.RemoveListener(OnHealth);
    }

    private void OnHealth(float health)
    {
        if (health <= threshold)
            Raise();
    }
}

public class StateTimeAtLeastCondition : Condition
{
    [Tooltip("Trigger when state time >= this value (seconds).")]
    public float timeThreshold = 1f;

    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onStateTimeChanged.AddListener(OnTime);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onStateTimeChanged.RemoveListener(OnTime);
    }

    private void OnTime(float t)
    {
        if (t >= timeThreshold)
            Raise();
    }
}

public class OnStateChangedCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onStateChanged.AddListener( Raise);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onStateChanged.RemoveListener( Raise);
    }

     
}

public class OnStateEndCondition : Condition
{
    public override void StartTrackCondition()
    {
        if (boss == null) return;
        boss.onAttackEnd.AddListener( Raise);
    }

    public override void StopTrackCondition()
    {
        if (boss == null) return;
        boss.onAttackEnd.RemoveListener( Raise);
    }

     
}
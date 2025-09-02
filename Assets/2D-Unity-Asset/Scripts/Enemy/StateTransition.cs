using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "BossStateTransition", menuName = "BossGraph/Boss State Transition")]
public class StateTransition : ScriptableObject
{
    [SerializeReference] public Condition condition;   // polymorphic class (not SO)
    public BossStateNode[] nextStates;
    public bool allowStateInterruption => condition.allowStateInterruption;
    [HideInInspector] public UnityEvent<BossStateNode> onConditionMet;
    [HideInInspector]public Vector2 position;
    private Boss _bossBound;

    public void Bind(Boss boss)
    {
        _bossBound = boss;
        if (condition != null) condition.Bind(boss);
    }

    public void OnConditionMet()
    {
        var arr = nextStates;
        if (arr == null || arr.Length == 0) return;

        int i = Random.Range(0, arr.Length);
        var next = arr[i];
        if (!allowStateInterruption)
        {
            if (!_bossBound.stateGraph.currentState.state.IsFinished)
            {
                return;
            }
        }
        if (next != null) onConditionMet?.Invoke(next);
    }

    public void StartTrackCondition()
    {
        if (condition == null) return;

        condition.onConditionMet.RemoveListener(OnConditionMet);
        condition.onConditionMet.AddListener(OnConditionMet);

        if (_bossBound == null)
        {
            var boss = Object.FindFirstObjectByType<Boss>();
            if (boss != null) Bind(boss);
        }

        condition.StartTrackCondition();
    }

    public void StopTrackCondition()
    {
        if (condition == null) return;

        condition.onConditionMet.RemoveListener(OnConditionMet);
        condition.StopTrackCondition();
    }
}

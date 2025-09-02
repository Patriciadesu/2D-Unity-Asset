using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "BossStateGraph", menuName = "BossGraph/Boss State Graph")]
public class BossStateGraph : ScriptableObject
{
    public BossStateNode[] stateNodes;
    public StateTransition[] transitionNodes;
    public BossStateNode currentState;

    public void Awake()
    {
        currentState = stateNodes.FirstOrDefault(n => n != null && n.isInitialState);
        if (stateNodes == null) return;

        for (int i = 0; i < stateNodes.Length; i++)
        {
            var n = stateNodes[i];
            if (n != null && n.isInitialState)
            {
                currentState = n;
                break;
            }
        }
    }

    public void StartState()
    {
        currentState = stateNodes.FirstOrDefault(n => n != null && n.isInitialState);
        if (currentState != null)
        {
            currentState.state.stage = StateStage.Enter;
            currentState.StartTrackingConditions();
            currentState.onStateChange.AddListener(ChangeState);
        }
    }

    public void ChangeState(BossStateNode nextState)
    {
        if (currentState != null)
        {
            //currentState.state.stage = StateStage.Exit;
            currentState.StopTrackingConditions();
            currentState.onStateChange.RemoveListener(ChangeState);
        }

        currentState = nextState;

        if (currentState != null)
        {
            currentState.state.stage = StateStage.Enter;
            currentState.StartTrackingConditions();
            currentState.onStateChange.AddListener(ChangeState);
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.VisualScripting;
using UnityEngine.AI;
using System;
using static NodeHelper.NodeUIHelpers;
using UnityEngine.UIElements;
public enum StateStage { Enter, Update, Exit }

public class BossState : INodeInspectorContributor
{
    public string stateName;
    public StateStage stage { get; set; } = StateStage.Enter;
    protected Boss boss;
    protected Animator animator;
    protected bool isFinished = false;
    public bool IsFinished => isFinished;

    public BossState(string name, Boss bossInstance)
    {
        stateName = name;
        boss = bossInstance;
        if (boss != null) animator = boss.GetComponent<Animator>();
    }

    public virtual void BindRuntime(Boss bossInstance)
    {
        boss = bossInstance;
        if (boss != null) animator = boss.GetComponent<Animator>();
    }

    public virtual void Enter()
    {
        Debug.Log($"Entering state: {stateName}");
        stage = StateStage.Update;
    }
    public virtual void Update() { Debug.Log($"Updating state: {stateName}"); }
    public virtual void FixedUpdate() { Debug.Log($"Fixed updating state: {stateName}"); }

    // Traditional no-arg Exit (cleanup only)
    public virtual void Exit()
    {
        Debug.Log($"Exiting state: {stateName}");
    }

    // NEW: state-driven transition API
    public virtual void Exit(BossStateNode nextState)
    {
        // Do this state's cleanup once
        Exit();

        // Ask the graph (via Boss) to switch to the requested next state
        if (boss != null && boss.stateGraph != null && nextState != null)
        {
            boss.stateGraph.ChangeState(nextState);
        }
    }
    public virtual void BuildInspectorUI(VisualElement container)
    {
        RefreshInspectorUI();
    }

    public virtual void RefreshInspectorUI()
    {
    }
}

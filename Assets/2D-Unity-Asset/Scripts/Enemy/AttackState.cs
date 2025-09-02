using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.VisualScripting;
using UnityEngine.AI;
using System;

[System.Serializable]
public class AttackState : BossState
{
    public TimelineAsset timelinePlayable;
    public float damage;

    private PlayableDirector director;
    private bool subscribed;
    private bool endedOnce;

    // Safety fallback if timeline has 0 duration or director misbehaves
    private const double MinDurationEpsilon = 1e-3;
    private float safetyTimer;
    private const float SafetyTimeout = 0.25f; // seconds, end quickly when no valid timeline

    public AttackState(Boss bossInstance) : base("Attack", bossInstance) { }

    public override void Enter()
    {
        base.Enter();

        endedOnce = false;
        safetyTimer = 0f;

        // 1) Toggle attack colliders ON
        if (boss.attackCollider != null)
            Array.ForEach(boss.attackCollider, c => { if (c) c.enabled = true; });

        // 2) Ensure we have a director
        director = boss.GetComponent<PlayableDirector>();
        if (director == null) director = boss.gameObject.AddComponent<PlayableDirector>();

        // 3) Prepare & play timeline (if any)
        if (timelinePlayable != null)
        {
            // Always rebuild to avoid stale graphs on re-entry
            director.time = 0;
            director.extrapolationMode = DirectorWrapMode.None; // end cleanly, don’t loop
            director.playOnAwake = false;
            director.playableAsset = null; // clear old asset to avoid stale state
            director.playableAsset = timelinePlayable;

            // Unsubscribe old (defensive) then subscribe
            if (subscribed)
            {
                director.stopped -= OnDirectorStopped;
                subscribed = false;
            }
            director.stopped += OnDirectorStopped;
            subscribed = true;

            // Rebuild graph to get correct duration immediately
            director.RebuildGraph();
            director.Evaluate(); // evaluate bindings and initial state
            director.playableGraph.GetRootPlayable(0).SetSpeed(boss.attackAnimationSpeedMultiplier);
            director.Play();

            // If somehow duration is invalid/zero, we’ll end via safety timer below
        }
        else
        {
            // No timeline → exit shortly (single frame might be too abrupt in some loops)
            stage = StateStage.Update; // allow one update
            safetyTimer = 0f;
        }
    }

    public override void Update()
    {
        base.Update();

        // If we have a valid director + graph, check for completion ourselves too
        if (director != null && director.playableGraph.IsValid())
        {
            // Handle zero/near-zero duration timelines via safety timer
            var duration = director.duration;
            if (duration <= MinDurationEpsilon)
            {
                safetyTimer += Time.deltaTime;
                if (safetyTimer >= SafetyTimeout)
                {
                    OnAttackEnd();
                    return;
                }
            }
            else
            {
                // Normal case: if time >= duration, end (covers cases where stopped event is missed)
                if (director.state != PlayState.Playing || director.time + MinDurationEpsilon >= duration)
                {
                    OnAttackEnd();
                    return;
                }
            }
        }
        else
        {
            // No director / invalid graph / no timeline → end via safety timer
            safetyTimer += Time.deltaTime;
            if (safetyTimer >= SafetyTimeout)
            {
                OnAttackEnd();
                return;
            }
        }
    }

    public override void Exit()
    {
        // Toggle attack colliders OFF (idempotent)
        if (boss.attackCollider != null)
            Array.ForEach(boss.attackCollider, c => { if (c) c.enabled = false; });

        // Stop & clean director safely
        if (director != null)
        {
            if (director.state == PlayState.Playing) director.Stop();

            if (subscribed)
            {
                director.stopped -= OnDirectorStopped;
                subscribed = false;
            }

            // Clear playable asset so re-entry can set fresh graph
            director.playableAsset = null;
        }

        base.Exit();
    }

    private void OnDirectorStopped(PlayableDirector _)
    {
        // Unity can invoke .stopped multiple times in some edge cases—guard this.
        OnAttackEnd();
    }

    private void OnAttackEnd()
    {
        if (endedOnce) return; // ensure single exit path
        endedOnce = true;

        isFinished = true;
        // If boss has a UnityEvent, guard null & no listeners scenarios
        if (boss.onAttackEnd != null)
            boss.onAttackEnd.Invoke();

        stage = StateStage.Exit;
    }
}


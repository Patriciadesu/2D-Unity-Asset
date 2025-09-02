using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.VisualScripting;
using UnityEngine.AI;
using System;

[System.Serializable]

public class PatrolState : BossState
{
    [Tooltip("Patrol points in the scene. The boss will loop through these.")]
    public Transform[] waypoints => boss.waypoints;

    [Tooltip("Movement speed when patrolling (used if no NavMeshAgent).")]
    [SerializeField]public float moveSpeed = 2f;

    [Tooltip("How close to a waypoint before switching to the next.")]
    public float arriveThreshold = 0.2f;

    [Tooltip("If true and a NavMeshAgent exists on the Boss, use it for movement.")]
    public bool useNavMeshIfAvailable = true;

    [Tooltip("Idle time (in seconds) at each waypoint before moving on.")]
    public float idleTime = 2f;

    private int _index;
    private Transform _self;
    private NavMeshAgent _agent;

    private float _idleTimer;
    private bool _isWaiting;

    public PatrolState(Boss bossInstance) : base("Patrol", bossInstance) { }

    public override void BindRuntime(Boss bossInstance)
    {
        base.BindRuntime(bossInstance);
        _self = boss != null ? boss.transform : null;
        _agent = boss != null ? boss.GetComponent<NavMeshAgent>() : null;
    }

    public override void Enter()
    {
        base.Enter();
        if (animator != null) animator.SetBool("Walk", true);

        _isWaiting = false;
        _idleTimer = 0f;

        if (_self == null) return;

        // Use nearest point as starting index (simple quality-of-life)
        if (waypoints != null && waypoints.Length > 0)
        {
            _index = 0;
            float best = float.PositiveInfinity;
            var pos = _self.position;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var w = waypoints[i];
                if (w == null) continue;
                float d = (w.transform.position - pos).sqrMagnitude;
                if (d < best) { best = d; _index = i; }
            }

            if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.speed = Mathf.Max(0.01f, moveSpeed);
                _agent.SetDestination(waypoints[_index].transform.position);
            }
        }
    }

    public override void Update()
    {
        if (_self == null || waypoints == null || waypoints.Length == 0)
        {
            base.Update();
            return;
        }

        // Handle idle waiting
        if (_isWaiting)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                _isWaiting = false;
                NextWaypoint();
            }
            return; // skip movement while waiting
        }

        var target = waypoints[_index];
        if (target == null) { base.Update(); return; }

        if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
        {
            // Agent handles movement; check arrival
            if (!_agent.pathPending && _agent.remainingDistance <= Mathf.Max(0.05f, arriveThreshold))
            {
                StartIdle();
            }
        }
        else
        {
            // Transform-based movement
            Vector3 dir = (target.transform.position - _self.position);
            float dist = dir.magnitude;
            if (dist <= arriveThreshold) { StartIdle(); return; }

            dir.Normalize();
            _self.position += dir * moveSpeed * Time.deltaTime;

            // Face movement direction
            if (dir.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(dir, Vector3.up);
                _self.rotation = Quaternion.Slerp(_self.rotation, look, 10f * Time.deltaTime);
            }
        }
    }

    public override void Exit()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
        animator.SetBool("Walk", false);
        base.Exit();
    }

    private void StartIdle()
    {
        _isWaiting = true;
        _idleTimer = idleTime;
        if (animator != null) animator.SetBool("Walk", false);

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.isStopped = true;
        }
    }

    private void NextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        _index = (_index + 1) % waypoints.Length;

        if (useNavMeshIfAvailable && _agent != null && _agent.isOnNavMesh)
        {
            var t = waypoints[_index];
            if (t != null)
            {
                _agent.isStopped = false;
                _agent.SetDestination(t.transform.position);
            }
        }

        if (animator != null) animator.SetBool("Walk", true);
    }
}

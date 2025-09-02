using System;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public partial class Boss : MonoBehaviour
{
    public BossStateGraph stateGraph;
    public Collider[] attackCollider;

    private bool hasPatrolState => stateGraph != null && stateGraph.transitionNodes != null &&
                                   stateGraph.transitionNodes.Any(t => t.nextStates != null &&
                                                                       t.nextStates.Any(s => s != null && s.state is PatrolState));
    [ShowIf("hasPatrolState")] public Transform[] waypoints;

    [HideInInspector] public UnityEvent onStateChanged;
    [HideInInspector] public UnityEvent<float> onStateTimeChanged;
    [HideInInspector] public UnityEvent<float> onHealthChanged;
    [HideInInspector] public UnityEvent onPlayerInSight;
    [HideInInspector] public UnityEvent onPlayerOutOfSight;
    [HideInInspector] public UnityEvent onPlayerInAttackRange;
    [HideInInspector] public UnityEvent onPlayerOutOfAttackRange;
    [HideInInspector] public UnityEvent onAttackEnd;

    public float maxHealth = 100f;
    public float health = 1f;
    public float attackRange = 5f;
    public float sightRange = 10f;
    public float speed = 2f;
    public float attackAnimationSpeedMultiplier = 1f;

    private Transform _player;

    private float _stateTime;
    private BossStateNode _lastStateNode;

    // Debounce flags for events
    private bool _isPlayerInSight;
    private bool _isPlayerInAttackRange;

    private const string PlayerTag = "Player";
    private float _reacquireTimer;
    [HideInInspector] public Vector3 initialPosition;
    [HideInInspector] public Quaternion initialRotation;
    private Vector3[] childrenInitialPositions;
    private Quaternion[] childrenInitialRotations;
    private void Awake()
    {
        onStateChanged ??= new UnityEvent();
        onStateTimeChanged ??= new UnityEvent<float>();
        onHealthChanged ??= new UnityEvent<float>();
        onPlayerInSight ??= new UnityEvent();
        onPlayerOutOfSight ??= new UnityEvent();
        onPlayerInAttackRange ??= new UnityEvent();
        onPlayerOutOfAttackRange ??= new UnityEvent();
        onAttackEnd ??= new UnityEvent();
        health = maxHealth;


        // Ensure compound trigger events from child weapon colliders reach this Boss
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Weapon colliders: triggers, disabled by default (enabled in Attack state)
        if (attackCollider != null)
        {
            for (int i = 0; i < attackCollider.Length; i++)
            {
                var c = attackCollider[i];
                if (c == null) continue;
                c.isTrigger = true;
                c.enabled = false;
            }
        }
    }

    private void Start()
    {
        SetInitialTransform();
        _player = Player.Instance.transform;
        if (TryGetComponent<PlayableDirector>(out PlayableDirector director))
        {
            director.playableAsset = null;
        }
        if (stateGraph != null)
        {
            if (stateGraph.transitionNodes != null)
            {
                for (int i = 0; i < stateGraph.transitionNodes.Length; i++)
                {
                    var t = stateGraph.transitionNodes[i];
                    if (t != null) t.Bind(this);
                }
            }

            stateGraph.Awake();
            stateGraph.StartState();

            _lastStateNode = stateGraph.currentState;
            _stateTime = 0f;

            if (_lastStateNode != null)
                _lastStateNode.onStateChange.AddListener(OnGraphRequestedStateChange);
        }

        onHealthChanged.Invoke(health);
    }

    private void OnDisable()
    {
        if (_lastStateNode != null)
            _lastStateNode.onStateChange.RemoveListener(OnGraphRequestedStateChange);
    }

    private void Update()
    {
        var g = stateGraph;
        if (g == null) return;

        if (g.currentState != _lastStateNode)
        {
            if (_lastStateNode != null)
                _lastStateNode.onStateChange.RemoveListener(OnGraphRequestedStateChange);

            _lastStateNode = g.currentState;
            _stateTime = 0f;
            onStateChanged.Invoke();

            if (_lastStateNode != null)
                _lastStateNode.onStateChange.AddListener(OnGraphRequestedStateChange);
        }

        var cs = g.currentState;
        if (cs != null)
        {
            switch (cs.state.stage)
            {
                case StateStage.Enter: cs.state.Enter(); break;
                case StateStage.Update: cs.state.Update(); break;
                    //case StateStage.Exit: cs.state.Exit(); break;
            }

        }
    }

    private void FixedUpdate()
    {
        _stateTime += Time.deltaTime;
        onStateTimeChanged.Invoke(_stateTime);
        var g = stateGraph;
        if (g != null && g.currentState != null && g.currentState.state.stage == StateStage.Update)
        {
            g.currentState.state.FixedUpdate();
        }

        // -------- Distance-based sight / attack-range checks (NO triggers) --------
        if (_player == null)
        {
            // Light reacquire once per 0.5s (avoids per-frame Find)
            _reacquireTimer -= Time.fixedDeltaTime;
            if (_reacquireTimer <= 0f)
            {
                _reacquireTimer = 0.5f;
            }
            // If still null, ensure we mark both false once
            if (_player == null)
            {
                SetSight(false);
                SetAttackRange(false);
                return;
            }
        }

        Vector3 to = _player.position - transform.position;
        float d2 = to.sqrMagnitude;

        float sight2 = sightRange * sightRange;
        float atk2 = attackRange * attackRange;

        SetSight(d2 <= sight2);
        SetAttackRange(d2 <= atk2);
    }

    private void OnGraphRequestedStateChange(BossStateNode _)
    {
        onStateChanged.Invoke();
        _stateTime = 0f;
    }

    // --------- Attack-only trigger: weapon collider hits player's collider -> TakeDamage ----------
    private void OnTriggerEnter(Collider other)
    {
        if (!IsInAttackState()) return;
        if (!other.CompareTag(PlayerTag)) return;

        float dmg = GetCurrentAttackDamage();
        if (dmg <= 0f) return;

        var targetGO = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        // Default simple signature; your Player can implement a different TakeDamage; SendMessage is flexible
        targetGO.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

    // ---------------- Helpers ---------------

    private void SetSight(bool value)
    {
        //if (_isPlayerInSight == value) return;
        _isPlayerInSight = value;
        if (value) onPlayerInSight.Invoke();
        else onPlayerOutOfSight.Invoke();
    }

    private void SetAttackRange(bool value)
    {
        //if (_isPlayerInAttackRange == value) return;
        _isPlayerInAttackRange = value;
        if (value) onPlayerInAttackRange.Invoke();
        else onPlayerOutOfAttackRange.Invoke();
    }

    private bool IsInAttackState()
        => stateGraph != null && stateGraph.currentState != null && stateGraph.currentState.state is AttackState;

    private float GetCurrentAttackDamage()
        => stateGraph != null && stateGraph.currentState != null && stateGraph.currentState.state is AttackState a
           ? a.damage
           : 0f;

    // Signals to drive conditions from gameplay
    public void TakeDamage(float damage)
    {
        float newHealth = health - damage;
        health = Mathf.Clamp(newHealth, 0f, maxHealth);
        onHealthChanged.Invoke(health);
    }
    public void PlayerInSight() { SetSight(true); }
    public void PlayerOutOfSight() { SetSight(false); }
    public void PlayerInAttackRange() { SetAttackRange(true); }
    public void PlayerOutOfAttackRange() { SetAttackRange(false); }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    public void SetInitialTransform()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Store initial positions and rotations of all children
        int childCount = transform.childCount;
        childrenInitialPositions = new Vector3[childCount];
        childrenInitialRotations = new Quaternion[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            childrenInitialPositions[i] = child.position;
            childrenInitialRotations[i] = child.rotation;
        }
    }
    public void ResetTransform()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (i < childrenInitialPositions.Length)
            {
                child.position = childrenInitialPositions[i];
                child.rotation = childrenInitialRotations[i];
            }
        }
    }
}

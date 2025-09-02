using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UIElements;
using static NodeHelper.NodeUIHelpers;

public class RotateToPlayerState : BossState
{
    public enum RotateMode
    {
        FollowPlayerMovement,
        PredictPlayerMovement
    }
    [Header("Rotation Settings")]
    private float rotationSpeed = 5f;      // How fast boss turns
    private float angleThreshold = 5f;     // Degrees considered "aligned"

    [Header("Prediction Settings")]
    public RotateMode rotateMode;
    private float predictionTime = 0.35f;
    private float maxLeadAngle = 45f;
    private float velocitySmoothing = 10f;
    private float minPredictSpeed = 0.05f;
    private bool debugDraw;

    private Transform _self;
    private Transform _player;
    private bool endedOnce;

    // Internal velocity tracking when no Rigidbody is available
    private Vector3 _lastPlayerPos; 
    private Vector3 _smoothedVel;
    private VisualElement _predictionGroup;

    public RotateToPlayerState(Boss bossInstance) : base("RotateToPlayer", bossInstance) { }

    public override void BindRuntime(Boss bossInstance)
    {
        base.BindRuntime(bossInstance);
        _self = boss != null ? boss.transform : null;
        _player = Player.Instance != null ? Player.Instance.transform : null;
    }

    public override void Enter()
    {
        base.Enter();
        isFinished = false;
        endedOnce = false;

        if (_player != null)
        {
            _lastPlayerPos = _player.position;
            _smoothedVel = Vector3.zero;
        }
    }

    public override void Update()
    {
        base.Update();
        if (_self == null || _player == null) return;
        if (Time.deltaTime <= 0f) return;

        // Base direction (current player position)
        Vector3 toPlayer = (_player.position - _self.position);
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f) return;

        Quaternion baseRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        Quaternion targetRot = baseRot;

        // --- Predictive target rotation ---
        if (rotateMode == RotateMode.PredictPlayerMovement)
        {
            // 1) Get player velocity
            Vector3 vel = Vector3.zero;
            Rigidbody rb = _player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                vel = rb.linearVelocity;
            }
            else
            {
                // Estimate from position delta
                Vector3 delta = (_player.position - _lastPlayerPos);
                vel = (Time.deltaTime > 0f) ? (delta / Time.deltaTime) : Vector3.zero;
            }
            _lastPlayerPos = _player.position;

            // Smooth velocity to reduce jitter
            float k = 1f - Mathf.Exp(-velocitySmoothing * Time.deltaTime);
            _smoothedVel = Vector3.Lerp(_smoothedVel, vel, k);

            // 2) Predict future point (horizontal)
            Vector3 predicted = _player.position + _smoothedVel * predictionTime;
            Vector3 toPred = predicted - _self.position;
            toPred.y = 0f;

            // If moving slow, fall back to direct aim
            if (toPred.sqrMagnitude > 0.0001f && _smoothedVel.magnitude >= minPredictSpeed)
            {
                Quaternion fullLeadRot = Quaternion.LookRotation(toPred.normalized, Vector3.up);

                // 3) Clamp lead so we don't over-anticipate
                if (maxLeadAngle < 179.9f)
                {
                    targetRot = Quaternion.RotateTowards(baseRot, fullLeadRot, maxLeadAngle);
                }
                else
                {
                    targetRot = fullLeadRot;
                }

                if (debugDraw)
                {
                    Debug.DrawLine(_self.position + Vector3.up * 0.1f, _player.position + Vector3.up * 0.1f, Color.white);
                    Debug.DrawLine(_player.position + Vector3.up * 0.1f, predicted + Vector3.up * 0.1f, Color.gray);
                    Debug.DrawRay(_self.position + Vector3.up * 0.1f, toPlayer.normalized, Color.cyan);
                    Debug.DrawRay(_self.position + Vector3.up * 0.1f, toPred.normalized, Color.yellow);
                }
            }
        }

        // 4) Rotate toward target (predicted or direct)
        _self.rotation = Quaternion.Slerp(_self.rotation, targetRot, rotationSpeed * Time.deltaTime);

        // 5) Done when close enough to the TARGET (predicted if used)
        float angle = Quaternion.Angle(_self.rotation, targetRot);
        if (angle <= angleThreshold)
        {
            OnRotationComplete();
        }
    }

    private void OnRotationComplete()
    {
        if (endedOnce) return;
        endedOnce = true;

        isFinished = true;
        if (boss.onAttackEnd != null)
            boss.onAttackEnd.Invoke();
    }
    public override void BuildInspectorUI(VisualElement container)
    {
        base.BuildInspectorUI(container);
        // Rotation (always visible)
        container.Add(FloatField("Rotation Speed", () => rotationSpeed, v => { rotationSpeed = v; RefreshInspectorUI(); }));
        container.Add(FloatField("Angle Threshold", () => angleThreshold, v => { angleThreshold = v; RefreshInspectorUI(); }));

        // Mode
        var enumField = EnumField("Rotate Mode", () => rotateMode, v =>
        {
            rotateMode = v;
            RefreshInspectorUI();
        });
        container.Add(enumField);

        // Prediction group (toggle visibility based on mode)
        _predictionGroup = new VisualElement();
        _predictionGroup.style.marginTop = 4;

        _predictionGroup.Add(FloatField("Prediction Time", () => predictionTime, v => predictionTime = v));
        _predictionGroup.Add(FloatField("Max Lead Angle", () => maxLeadAngle, v => maxLeadAngle = v));
        _predictionGroup.Add(FloatField("Velocity Smoothing", () => velocitySmoothing, v => velocitySmoothing = v));
        _predictionGroup.Add(FloatField("Min Predict Speed", () => minPredictSpeed, v => minPredictSpeed = v));
        _predictionGroup.Add(Toggle("Debug Draw", () => debugDraw, v => debugDraw = v));

        container.Add(_predictionGroup);

        RefreshInspectorUI();
    }

    public override void RefreshInspectorUI()
    {
        base.RefreshInspectorUI();
        if (_predictionGroup != null)
            Show(_predictionGroup, rotateMode == RotateMode.PredictPlayerMovement);
    }
}

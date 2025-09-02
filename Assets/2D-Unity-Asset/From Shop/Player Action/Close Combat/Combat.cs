using NaughtyAttributes;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;


public class Combat : PlayerExtension, IUseStamina
{
    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Mouse0;
    public float cooldownTime = 0.5f;
    public float holdThreshold = 2f;
    public float damage = 10f;
    [Range(1, 10)] public float damagemultiplier = 2f;

    float oneHandHitWindow = 0.15f; // ระยะเวลานับโดนของมือละครั้ง
    float twoHandHitWindow = 0.20f;
    float heldTime;
    float lastPunchTime = 0f;
    bool isPunch = false;
    bool isHolding = false;
    bool is2hand;
    float keyDownTime = 0f;

    bool attackWindowOpen;
    HashSet<Boss> hitThisSwing = new();   // กันตีซ้ำศัตรูตัวเดิมในสวิงเดียว

    bool isReadyToPunch => Time.time >= lastPunchTime + cooldownTime;
    bool CanPunch => isReadyToPunch && _player.currentstamina >= staminaCost;

    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 2f;
    [ShowIf("useStamina")][Range(1, 10)] public int staminaCostMultiplyer = 3;
    public bool isUsingStamina => useStamina && isPunch;
    public bool canDrainStamina => _player.currentstamina >= staminaCost && useStamina;

    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
            _player.currentstamina = Mathf.Max(_player.currentstamina - amount, 0f);
    }

    public void SetUp(float _damage) => damage = _damage;

    void Update()
    {
        // โหมดเตรียม (ถืออาวุธ)
        if (Input.GetKey(activateKey)) PrepareCombat();
        else FinishedPrepareCombat();

        if (Input.GetKeyDown(activateKey) && CanPunch)
        {
            keyDownTime = Time.time;
            isHolding = true;
        }

        // ปล่อย
        if (Input.GetKeyUp(activateKey) && isHolding)
        {
            PrepareCombat();
            heldTime = Time.time - keyDownTime;

            if (heldTime < holdThreshold) Melee1Hand();
            else Melee2Hand();

            isHolding = false;
        }
    }

    void Melee1Hand()
    {
        if (isReadyToPunch)
        {
            if (isPunch) return;
            isPunch = true;
            attackWindowOpen = true;
            hitThisSwing.Clear();

            if (canDrainStamina) DrainStamina(staminaCost);

            _player.animator.SetTrigger("MeleeAttack1hand");

            // เปิดหน้าต่างโดนเฉพาะช่วงนี้
            Invoke(nameof(CloseAttackWindow), oneHandHitWindow);
            Invoke(nameof(FinishedPunch), 0.2f);
        }
        
    }

    void Melee2Hand()
    {
        if (isReadyToPunch)
        {
            is2hand = true;
            if (isPunch) return;
            isPunch = true;
            attackWindowOpen = true;
            hitThisSwing.Clear();

            if (canDrainStamina) DrainStamina(staminaCost * staminaCostMultiplyer);

            _player.animator.SetTrigger("MeleeAttack2hand");

            Invoke(nameof(CloseAttackWindow), twoHandHitWindow);
            Invoke(nameof(FinishedPunch), 0.5f);
        }
    }

        void PrepareCombat() => _player.animator.SetBool("PrepareCombat", true);
    void FinishedPrepareCombat() => _player.animator.SetBool("PrepareCombat", false);

    void FinishedPunch()
    {
        if (!isPunch) return;
        isPunch = false;
        lastPunchTime = Time.time;
        attackWindowOpen = false;     // safety ปิดหน้าต่าง
        _player.animator.speed = 1f;
        is2hand = false;
    }

    void CloseAttackWindow() => attackWindowOpen = false;

    // ---------- ทำดาเมจ ----------
    void TryHit(Boss boss)
    {
        if (!attackWindowOpen) return;
        if (!is2hand)
        {
            if (hitThisSwing.Add(boss))   // ครั้งแรกของศัตรูตัวนี้ในสวิงนี้เท่านั้น
                boss.TakeDamage(damage);
        }else
        {
            if (hitThisSwing.Add(boss))
                boss.TakeDamage(damage * damagemultiplier);
        }
        
    }


    // ใช้ทั้ง Enter + Stay เพื่อไม่พลาดกรณีเริ่มซ้อนทับกันอยู่แล้ว
    void OnTriggerEnter(Collider other) { if (other.TryGetComponent(out Boss b)) TryHit(b); }
    void OnTriggerStay(Collider other) { if (other.TryGetComponent(out Boss b)) TryHit(b); }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Boss b)) TryHit(b);
    }
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Boss b)) TryHit(b);
    }
}

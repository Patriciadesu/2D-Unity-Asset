using NaughtyAttributes;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Block : PlayerExtension, IUseStamina
{
    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Mouse1;

    public bool isBlocking = false;
    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 5f;
    public bool isUsingStamina => useStamina;
    public bool canDrainStamina => _player.currentstamina >= staminaCost && useStamina;
    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
        {
            _player.currentstamina = Mathf.Max(_player.currentstamina - amount, 0f);
        }
    }

    void Update()
    {
        UpdateisBlocking();
        if (Input.GetKey(activateKey))
        {
            StartBlocking();
        }
        else
        {
            StopBlocking();
        }
    }
    void UpdateisBlocking()
    {
        if (isBlocking)
        {
            _player.canHit = false;
        }else
        {
            _player.canHit = true;
        }
    }

    void StartBlocking()
    {
        if (canDrainStamina)
        {
            DrainStamina(staminaCost * Time.deltaTime);
        }
        isBlocking = true;
        _player.animator.SetBool("isBlocking", true);
    }

    void StopBlocking()
    {
        isBlocking = false;
        _player.animator.SetBool("isBlocking", false);
    }
}
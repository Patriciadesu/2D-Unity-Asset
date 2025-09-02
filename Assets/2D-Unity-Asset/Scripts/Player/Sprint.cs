using NaughtyAttributes;
using UnityEngine;

public class Sprint : PlayerExtension, IUseStamina
{
    [Header("UI")]
    public bool enableSprintUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.LeftShift;
    public float sprintSpeed = 8f;
    public bool useStamina = true; // Toggle stamina consumption during sprint
    [ShowIf("useStamina")] public float sprintCost = 10f; // Stamina consumed per second
    public bool isUsingStamina => useStamina && isSprinting;
    public bool canDrainStamina => _player.currentstamina >= sprintCost && useStamina;
    private bool isSprinting = false;
    public bool IsSprinting => isSprinting;



    public override void OnStart(Player player)
    {
        base.OnStart(player);
        if (enableSprintUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {
        bool canSprint = _player.canMove && _player.currentstamina > 0;
        if (Input.GetKey(activateKey) && canSprint && !isSprinting)
        {
            StartSprint();
        }
        else if (Input.GetKeyUp(activateKey) || !canSprint)
        {
            StopSprint();
        }

        if (isSprinting)
        {
            if (canDrainStamina)
            {
                DrainStamina(sprintCost * Time.deltaTime);
            }
            else if (useStamina) // If stamina can't be drained, stop sprinting
            {
                StopSprint();
            }
        }

        if (enableSprintUI && uiManager != null)
            uiManager.UpdateSprint(isSprinting);
    }

    private void StartSprint()
    {
        isSprinting = true;
        _player.additionalSpeed += sprintSpeed;
        _player.animator.SetBool("isRunning", true);
    }

    private void StopSprint()
    {
        if (isSprinting)
        {
            isSprinting = false;
            _player.additionalSpeed -= sprintSpeed;
            _player.animator.SetBool("isRunning", false);
        }
    }
    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
        {
            _player.currentstamina = Mathf.Max(_player.currentstamina - amount, 0f);
        }
    }
}

public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    public bool enableSprintUI = true;

    
    public void UpdateSprint(bool isSprinting)
    {
        if (!enableSprintUI) return;
        sprintUI.SetActive(isSprinting);
    }

}

using UnityEngine;

public class Crouch : PlayerExtension
{
    [Header("UI")]
    public bool enableCrouchUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.C;
    public float crouchSpeed = 2f;

    private bool isCrouching = false;
    private bool CanCrouch => _player.canMove && _player.isGrounded && _player.canApplyGravity;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        if (enableCrouchUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(activateKey) && CanCrouch)
        {
            ToggleCrouch();
        }

        if (enableCrouchUI && uiManager != null)
            uiManager.UpdateCrouch(isCrouching);
    }

    public void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        _player.animator.SetBool("isCrouching", isCrouching);

        CapsuleCollider2D collider = _player._capsule2D;
        if (collider == null) return;

        if (isCrouching)
        {
            _player.additionalSpeed -= crouchSpeed;

            // Shrink collider
            collider.size = new Vector2(collider.size.x, collider.size.y / 2f);
            collider.offset = new Vector2(collider.offset.x, collider.offset.y / 2f);
        }
        else
        {
            _player.additionalSpeed += crouchSpeed;

            // Restore collider (double back)
            collider.size = new Vector2(collider.size.x, collider.size.y * 2f);
            collider.offset = new Vector2(collider.offset.x, collider.offset.y * 2f);
        }
    }
}

public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    public bool enableCrouchUI = true;

    public void UpdateCrouch(bool isCrouching)
    {
        if (!enableCrouchUI || crouchUI == null) return;
        crouchUI.SetActive(isCrouching);
    }
}

using UnityEngine;

public class MultipleJump : PlayerExtension
{
    [Header("UI")]
    public bool enableMultipleJumpUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Space;
    public int maxJumps = 3;
    private int jumpCount;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        if (enableMultipleJumpUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {
        if (_player.isGrounded)
        {
            jumpCount = 1;
        }
        else if (Input.GetKeyDown(activateKey) && jumpCount < maxJumps && _player.canApplyGravity)
        {
            _player.Jump2D();
            jumpCount++;
        }

        if (enableMultipleJumpUI && uiManager != null)
            uiManager.UpdateMultipleJump(jumpCount, maxJumps, _player.isGrounded);
    }
}
public partial class PlayerUIManager : Singleton<PlayerUIManager>
{

    public bool enableMultipleJumpUI = true;
    public void UpdateMultipleJump(int jumpCount, int maxJumps, bool isGrounded)
    {
        if (!enableMultipleJumpUI) return;
        bool showUI = !isGrounded && 1 < jumpCount && jumpCount <= maxJumps;
        multipleJumpUI.gameObject.SetActive(showUI);
        if (showUI)
            multipleJumpUI.text = $"Jump X{jumpCount}";
    }

}
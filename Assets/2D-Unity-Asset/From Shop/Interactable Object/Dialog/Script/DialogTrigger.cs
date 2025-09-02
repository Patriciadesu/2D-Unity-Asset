using NaughtyAttributes;
using UnityEngine;

public class DialogTrigger : ObjectEffect
{
    [TextArea(3, 10), SerializeField] private string[] dialog;

    public void Start()
    {
        if (DialogManager.Instance == null)
        {
            Player.Instance.gameObject.AddComponent<DialogManager>();
        }
    }
    public override void ApplyEffect(Player player)
    {
        if (player != null)
        {
            TriggerDialog();
            //Debug.Log($"{gameObject.name} triggered dialog - {player.gameObject.name} started dialog!");
        }
    }

    public void TriggerDialog()
    {
        DialogManager dialogManager = DialogManager.Instance;
        if (dialogManager != null)
        {
            dialogManager.StartDialog(dialog);
        }
        else
        {
            Debug.LogWarning("DialogManager not found in the scene.");
        }
    }
}

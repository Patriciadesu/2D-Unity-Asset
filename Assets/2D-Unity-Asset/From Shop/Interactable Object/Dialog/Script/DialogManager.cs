using System;
using System.Collections;
using System.ComponentModel;
using NaughtyAttributes;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogManager : Singleton<DialogManager> , ICancleGravity
{
    enum ContinueInput
    {
        [Description("Click Button")] ClickButton,
        [Description("Press Any Key")] PressAnyKey
    }
    [Header("Dialog Setting")]
    [SerializeField,Range(0,1)] float textInterval = 0.1f;
    [SerializeField] bool freezePlayerWhileDialog = true;
    [SerializeField] ContinueInput continueWith = ContinueInput.ClickButton;
    [Foldout("UI Setting"), SerializeField] GameObject dialogUI;
    [Foldout("UI Setting"), SerializeField] Button dialogButton;
    [Foldout("UI Setting"), SerializeField] TMP_Text dialogText;
    private string[] dialog;
    private int currentDialog;
    public bool canApplyGravity { get; set; } = true;
    private CanvasGroup canvasGroup => this.GetComponent<CanvasGroup>();

    #region Dialog State
    private bool isDialogPlaying;
    private bool isRunLetter => isDialogPlaying && dialogText.maxVisibleCharacters < dialogText.text.Length;
    private bool canContinue => currentDialog < dialog.Length;
    private bool skipLetter;
    #endregion
    void Start()
    {
        dialogButton.onClick.AddListener(() =>
        {
            if (isRunLetter)
            {
                skipLetter = true;
            }
            else if (canContinue)
            {
                NextDialog();
            }
            else
            {
                EndDialog();
            }
        });
        canvasGroup.alpha = 0;
    }
    void Update()
    {
        if (isDialogPlaying && continueWith == ContinueInput.PressAnyKey && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            if (isRunLetter)
            {
                skipLetter = true;
            }
            else if (canContinue)
            {
                NextDialog();
            }
            else
            {
                EndDialog();
            }
        }
    }
    public void StartDialog(string[] _dialog)
    {
        canvasGroup.alpha = 1;
        switch (continueWith)
        {
            case ContinueInput.ClickButton:
                dialogButton.gameObject.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case ContinueInput.PressAnyKey:
                dialogButton.gameObject.SetActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        if (freezePlayerWhileDialog)
        {
            Player.Instance.canMove = false;
            canApplyGravity = false;
        }
        Player.Instance.animator.SetBool("isRunning", false);
        Player.Instance.animator.SetBool("isRun", false);
        dialog = _dialog;
        currentDialog = 0;
        isDialogPlaying = true;
        dialogUI.SetActive(true);
        dialogText.text = "";
        StartCoroutine(RunLetter());
    }
    public void NextDialog()
    {
        currentDialog++;
        StartCoroutine(RunLetter());
    }
    public void EndDialog()
    {
        canvasGroup.alpha = 0;
        if (freezePlayerWhileDialog)
        {
            Player.Instance.canMove = true;
            canApplyGravity = true;
        }
        if(ContinueInput.ClickButton == continueWith)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        isDialogPlaying = false;
        dialogUI.SetActive(false);

        dialogText.text = "";
        currentDialog = 0;
    }
    public IEnumerator RunLetter()
    {
        if (isDialogPlaying&& canContinue)
        {
            string text = dialog[currentDialog];
            //text = text.Trim();
            dialogText.text = text;
            dialogText.maxVisibleCharacters = 0;
            foreach (char letter in text.ToCharArray())
            {
                if (skipLetter) //If Skip
                {
                    skipLetter = false;
                    dialogText.maxVisibleCharacters = text.Length;
                    break;
                }
                else //Normal
                {
                    dialogText.maxVisibleCharacters ++;
                    yield return new WaitForSeconds(textInterval);
                }
            }


        }
        else
        {
            EndDialog();
        }
    }

}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using NaughtyAttributes;

public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    [Foldout("UI Components")]public Slider staminaBar; // Always visible
    [Foldout("UI Components")]public Slider healthBar; // Always visible
    [Foldout("UI Components")]public Slider dashCooldownUI; // Shown during cooldown
    [Foldout("UI Components")]public GameObject sprintUI; // Shown when sprinting
    [Foldout("UI Components")]public TextMeshProUGUI multipleJumpUI; // Shown when not grounded

    // Enable/Disable toggles for UI elements
    [Header("Enable/Disable UI Elements")]
    public bool enableStaminaBar = true;
    public bool enableHealthBar = true;
    
    private Player player;

    public void Start()
    {
        player = Object.FindAnyObjectByType<Player>();

        // // Apply UI settings from Player
        enableHealthBar = player.enableHealthBar;
        enableStaminaBar = player.enableStaminaBar;

        // Initialize UI states
        if (enableSprintUI) sprintUI.SetActive(false);
        if (enableDashCooldownUI) dashCooldownUI.gameObject.SetActive(false);
        if (enableMultipleJumpUI) multipleJumpUI.gameObject.SetActive(false);
    }

    void Update()
    {
        // Always update stamina bar if enabled
        if (enableStaminaBar)
            staminaBar.value = player.currentstamina / player.maxstamina;

        // Always update health bar if enabled
        if (enableHealthBar)
            healthBar.value = player.currenthealth / player.maxhealth;
    }

    // Methods to update UI for each ability
    
    
    }

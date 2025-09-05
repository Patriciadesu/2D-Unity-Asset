using UnityEngine;
using System.Collections.Generic;
using Unity.Cinemachine;
using NaughtyAttributes;


public partial class Player : Singleton<Player>
{
    [Header("UI Settings")]
    [Foldout("Player Stats", true)] public bool enableHealthBar = true;
    [Foldout("Player Stats", true)] public bool enableStaminaBar = true;
    public bool EnableHealthBar => enableHealthBar;
    public bool EnableStaminaBar => enableStaminaBar;
}
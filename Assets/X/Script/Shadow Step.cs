using Mono.Cecil;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShadowStep : PlayerExtension
{
    [Foldout("ShadowStep Control", true)] public KeyCode SpawnShadowKey = KeyCode.R;
    [Foldout("ShadowStep Control", true)] public KeyCode TeleportToShadowKey = KeyCode.T;
    [Foldout("ShadowStep Control", true)] public int ShadowCount = 3;
    [Foldout("ShadowStep Control", true)] public GameObject ShadowStepPrefab;
    [Foldout("ShadowStep Control", true), SerializeField] private List<GameObject> spawnedShadow = new List<GameObject>();

    [Foldout("ShadowStep UI", true)] public List<Image> shadowUI = new List<Image>();
    [Foldout("ShadowStep UI", true)] public Color selectedUIColor = Color.red;
    [Foldout("ShadowStep UI", true)] public Color unselectedUIColor = Color.white;
    [Foldout("ShadowStep UI", true)] public Color emptyUIColor = new Color(1f, 1f, 1f, 0.35f);

    GameObject player;
    private int selectedIndex = -1;

    // UI parent container (auto-created if needed)
    private RectTransform uiContainer;

    void Start()
    {
        player = this.gameObject;

        // If no UI assigned, create default UI
        if (shadowUI == null || shadowUI.Count == 0)
        {
            CreateDefaultUI();
        }

        UIUpdate();
    }

    void Update()
    {
        if (Input.GetKeyDown(SpawnShadowKey)) DropShadow();
        if (Input.GetKeyDown(TeleportToShadowKey)) Teleport();

        float scroll = Input.mouseScrollDelta.y;
        if (scroll > 0.01f) MoveSelection(-1);
        else if (scroll < -0.01f) MoveSelection(+1);
    }

    void DropShadow()
    {
        Vector2 playerPosition = player.transform.position;
        playerPosition.y -= 0.3562498f;

        GameObject shadow = Instantiate(ShadowStepPrefab, playerPosition, player.transform.rotation);
        spawnedShadow.Add(shadow);

        while (spawnedShadow.Count > Mathf.Max(0, ShadowCount))
        {
            var oldest = spawnedShadow[0];
            spawnedShadow.RemoveAt(0);
            if (oldest != null) Destroy(oldest);
        }
        if(spawnedShadow.Count == 1)
            selectedIndex = 0;
        UIUpdate();
    }

    void MoveSelection(int step)
    {
        if (spawnedShadow.Count == 0) return;

        int next = selectedIndex + step;
        next = (next % spawnedShadow.Count + spawnedShadow.Count) % spawnedShadow.Count;

        if (next != selectedIndex)
        {
            selectedIndex = next;
            UIUpdate();
        }
    }

    void Teleport()
{
    if (selectedIndex < 0 || selectedIndex >= spawnedShadow.Count) return;

    // Move player to shadow
    Vector2 shadowPosition = spawnedShadow[selectedIndex].transform.position;
    shadowPosition.y += 0.3562498f;
    player.transform.position = shadowPosition;

    // Destroy the used shadow
    Destroy(spawnedShadow[selectedIndex]);
    spawnedShadow.RemoveAt(selectedIndex);

    // Rearrange selection: if we still have shadows, clamp index
    if (spawnedShadow.Count > 0)
    {
        selectedIndex = Mathf.Clamp(selectedIndex, 0, spawnedShadow.Count - 1);
    }
    else
    {
        selectedIndex = -1; // nothing left
    }

    UIUpdate();
}

void UIUpdate()
{
    if (shadowUI == null || shadowUI.Count == 0) return;

    for (int i = 0; i < shadowUI.Count; i++)
    {
        var img = shadowUI[i];
        if (!img) continue;

        bool slotHasShadow = (i < spawnedShadow.Count) && spawnedShadow[i] != null;

        if (i == selectedIndex && slotHasShadow)
            img.color = selectedUIColor;
        else
            img.color = slotHasShadow ? unselectedUIColor : emptyUIColor;
    }
}

    /// <summary>
    /// Auto-creates a default UI bar with empty slots if none are assigned.
    /// </summary>
    void CreateDefaultUI()
    {
        // Ensure Canvas exists
        GameObject canvasGO = Instantiate(Resources.Load<GameObject>("ShadowStep UI"));
        foreach (Transform child in canvasGO.transform)
        {
            shadowUI.Add(child.GetComponent<Image>());
        }

    }
}



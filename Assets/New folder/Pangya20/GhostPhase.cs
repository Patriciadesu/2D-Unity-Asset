using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostPhaseRobust : PlayerExtension
{
 [Header("Ability Settings")]
    public string normalLayerName = "Player";
    public string ghostLayerName = "Ghost";
    public float ghostDuration = 3f;
    public float cooldown = 5f;
    public KeyCode activateKey = KeyCode.E;

    // Runtime state variables, protected so child classes can access them
    protected bool isGhosting = false;
    protected bool isOnCooldown = false;
    protected Coroutine activeCoroutine = null;

    // Store original layers for every transform under this object
    private List<Transform> allTransforms = new List<Transform>();
    private Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();

    // 'virtual' allows child classes to override this method
    public virtual void Awake()
    {
        // Gather hierarchy and store original layers
        allTransforms.Clear();
        originalLayers.Clear();

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            allTransforms.Add(t);
            originalLayers[t] = t.gameObject.layer;
        }
    }

    // 'virtual' allows child classes to override this method
    public virtual void Start()
    {
        // Sanity checks
        if (LayerMask.NameToLayer(normalLayerName) == -1)
            Debug.LogError($"[Ghost] Normal layer '{normalLayerName}' not found.");
        if (LayerMask.NameToLayer(ghostLayerName) == -1)
            Debug.LogError($"[Ghost] Ghost layer '{ghostLayerName}' not found.");
    }

    void Update()
    {
        // If you use Unity's new Input System, replace this input check with your action callback.
        if (Input.GetKeyDown(activateKey) && !isGhosting && !isOnCooldown)
        {
            activeCoroutine = StartCoroutine(DoGhostPhase());
        }
    }

    /// <summary>
    /// The main coroutine that handles the phasing logic.
    /// It's 'protected virtual' so that child classes (like ShadowDash) can override and add their own logic.
    /// </summary>
    protected virtual IEnumerator DoGhostPhase()
    {
        isGhosting = true;
        isOnCooldown = true;

        SetAllLayers(ghostLayerName);
        Debug.Log("[Ghost] Entered ghost state.");

        // Visual example: reduce alpha if we have a SpriteRenderer on root
        var sr = GetComponent<SpriteRenderer>();
        if (sr) { Color c = sr.color; c.a = 0.5f; sr.color = c; }

        // Use realtime so pause/timeScale doesn't stop it
        yield return new WaitForSecondsRealtime(ghostDuration);

        // Restore everything back to normal
        RestoreOriginalLayers();
        if (sr) { Color c = sr.color; c.a = 1f; sr.color = c; }

        isGhosting = false;
        Debug.Log("[Ghost] Exited ghost state.");

        // Cooldown
        yield return new WaitForSecondsRealtime(cooldown);
        isOnCooldown = false;
        activeCoroutine = null;
        Debug.Log("[Ghost] Cooldown finished.");
    }
    
    /// <summary>
    /// Sets the layer for this GameObject and all its children.
    /// </summary>
    protected void SetAllLayers(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
        {
            Debug.LogError($"[Ghost] Layer '{layerName}' not found, aborting layer change.");
            return;
        }

        foreach (var t in allTransforms)
        {
            if (t != null)
            {
                t.gameObject.layer = layer;
            }
        }
    }

    /// <summary>
    /// Restores the original layers for this GameObject and all children.
    /// </summary>
    protected void RestoreOriginalLayers()
    {
        foreach (var kv in originalLayers)
        {
            if (kv.Key != null) // if child wasn't destroyed
                kv.Key.gameObject.layer = kv.Value;
        }
    }

    void OnDisable()
    {
        // If object gets disabled while ghosting, restore layers immediately
        if (isGhosting)
        {
            RestoreOriginalLayers();
            isGhosting = false;
            Debug.Log("[Ghost] OnDisable: restored layers.");
        }

        // Stop coroutine (it won't continue while disabled). Clear flags.
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);
        activeCoroutine = null;
        isOnCooldown = false;
    }

    void OnDestroy()
    {
        // cleanup if object is destroyed
        if (isGhosting)
            RestoreOriginalLayers();
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for using Lists

public class ShadowClone : PlayerExtension
{
    [Header("Activation")]
    public KeyCode activateKey = KeyCode.Q;

    [Header("Clone Settings")]
    public GameObject clonePrefab;   // The prefab for the clones
    public float cloneDuration = 8f; // How long each clone lasts
    public int maxClones = 5;        // The maximum number of clones in the chain

    [Header("Follow Behavior")]
    public float followSpeed = 10f;  // How fast the clones follow their target
    private float followDistance = 1f;// Distance between each clone in the chain

    [Header("Cooldown")]
    public float cooldownTime = 0.5f;// Cooldown between each spawn

    private bool isOnCooldown = false;
    // We use a List to keep track of all active clones in order
    private List<GameObject> activeClones = new List<GameObject>();

    void Update()
    {
        // 1. Clean up the list by removing any clones that have been destroyed
        activeClones.RemoveAll(item => item == null);

        // 2. Check for input to spawn a new clone
        if (Input.GetKeyDown(activateKey) && !isOnCooldown && activeClones.Count < maxClones)
        {
            StartCoroutine(SpawnClone());
        }

        // 3. Move all active clones in a chain formation
        PositionClonesInChain();
    }

    /// <summary>
    /// This coroutine handles spawning one clone and its cooldown.
    /// </summary>
    IEnumerator SpawnClone()
    {
        isOnCooldown = true;

        Vector3 spawnPosition;
        Quaternion spawnRotation;

        // If this is the first clone, spawn it behind the player.
        // Otherwise, spawn it behind the last clone in the chain.
        if (activeClones.Count == 0)
        {
            spawnPosition = transform.position - transform.forward * followDistance;
            spawnRotation = transform.rotation;
        }
        else
        {
            Transform lastClone = activeClones[activeClones.Count - 1].transform;
            spawnPosition = lastClone.position - lastClone.forward * followDistance;
            spawnRotation = lastClone.rotation;
        }

        // Instantiate a new clone at the calculated position
        GameObject newClone = Instantiate(clonePrefab, spawnPosition, spawnRotation);
        activeClones.Add(newClone);

        // Schedule the clone to be automatically destroyed after its duration expires
        Destroy(newClone, cloneDuration);

        // Wait for the cooldown period before another clone can be spawned
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }

    /// <summary>
    /// Moves each clone to follow the object in front of it (player or another clone).
    /// </summary>
    void PositionClonesInChain()
    {
        if (activeClones.Count == 0) return;

        for (int i = 0; i < activeClones.Count; i++)
        {
            // Determine the target for the current clone
            Transform target;
            if (i == 0)
            {
                // The first clone always follows the player
                target = this.transform;
            }
            else
            {
                // Every other clone follows the one in front of it in the list
                target = activeClones[i - 1].transform;
            }

            GameObject currentClone = activeClones[i];
            Vector3 targetPosition = target.position - target.forward * followDistance;

            // Smoothly move the clone to its target position
            currentClone.transform.position = Vector3.Lerp(currentClone.transform.position, targetPosition, followSpeed * Time.deltaTime);

            // Match the target's rotation
            currentClone.transform.rotation = Quaternion.Lerp(currentClone.transform.rotation, target.rotation, followSpeed * Time.deltaTime);
        }
    }
}
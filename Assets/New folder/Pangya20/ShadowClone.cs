using UnityEngine;
using System.Collections;

public class ShadowClone : PlayerExtension
{
    public KeyCode activateKey = KeyCode.Q;
    public GameObject clonePrefab;   // Assign your player prefab here
    public float cloneDuration = 5f; // How long the clone lasts
    public float cooldownTime = 3f;  // Cooldown before skill can be used again

    private bool isOnCooldown = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isOnCooldown)
        {
            StartCoroutine(SpawnCloneCoroutine());
        }
    }

    IEnumerator SpawnCloneCoroutine()
    {
        isOnCooldown = true;

       
        GameObject clone = Instantiate(clonePrefab, transform.position, transform.rotation);

        
        ShadowFollow shadow = clone.AddComponent<ShadowFollow>();
        if (shadow != null)
        {
            shadow.target = this.transform; 
        }

    
        yield return new WaitForSeconds(cloneDuration);

        
        Destroy(clone);

        // Cooldown timer
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }
}

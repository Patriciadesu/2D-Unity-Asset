using UnityEngine;

public class VanishEffect : ObjectEffect
{
    [SerializeField] private float vanishDelay = 2f;
    [SerializeField] private float returnDelay = 2f;

    private bool isVanishing = false;
    private Renderer objectRenderer;
    private Collider objectCollider;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        objectCollider = GetComponent<Collider>();
    }


    
    public override void ApplyEffect(Player player)
    {
        if (player != null && !isVanishing)
        {
            StartCoroutine(VanishRoutine(player));
            Debug.Log($"{gameObject.name} triggered vanish effect on {player.gameObject.name}");
        }
    }

    private System.Collections.IEnumerator VanishRoutine(Player player)
    {
        isVanishing = true;

        yield return new WaitForSeconds(vanishDelay);

        if (objectRenderer != null) objectRenderer.enabled = false;
        if (objectCollider != null) objectCollider.enabled = false;

        yield return new WaitForSeconds(returnDelay);

        if (objectRenderer != null) objectRenderer.enabled = true;
        if (objectCollider != null) objectCollider.enabled = true;

        isVanishing = false;
    }
}
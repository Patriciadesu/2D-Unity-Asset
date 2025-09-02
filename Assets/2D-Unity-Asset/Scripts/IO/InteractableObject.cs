using Unity.VisualScripting;
using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class InteractableObject : MonoBehaviour
{
    [SerializeField] bool usePhysic = true;
    [SerializeField] bool useGravity = true;
    [SerializeField] bool isTrigger = false;
    private ObjectEffect[] effects;
    void Start()
    {
        GetComponent<Rigidbody>().isKinematic = !usePhysic;
        if (isTrigger) useGravity = false;
        GetComponent<Rigidbody>().useGravity = useGravity;
        effects = GetComponents<ObjectEffect>();
        EnsureColliderExists();
        EnsureRigidbodyExists();
        EnsureIsTrigger();
    }
    void OnCollisionEnter(Collision collision)
    {
        // First try to get Player directly from the colliding object
        Player player = collision.gameObject.GetComponent<Player>();
        
        if (player != null)
        {
            Debug.Log($"Player with Player Hit: {collision.gameObject.name}");
            HandlePlayerCollision(collision, player);
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            // If it has Player tag but no Player, search in hierarchy
            player = FindPlayerInHierarchy(collision.gameObject);
            
            if (player != null)
            {
                Debug.Log($"Player found in hierarchy: {player.gameObject.name}");
                HandlePlayerCollision(collision, player);
            }
            else
            {
                Debug.LogWarning($"GameObject with 'Player' tag hit {gameObject.name} but has no Player component anywhere in hierarchy!");
            }
        }
        else
        {
            // Debug: Log all collisions to help troubleshoot
            Debug.Log($"Collision detected with: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        }
    }
    public void OnTriggerEnter(Collider other)
    {
        // First try to get Player directly from the colliding object
        Player player = other.gameObject.GetComponent<Player>();
        
        if (player != null)
        {
            Debug.Log($"Player with Player Hit: {other.gameObject.name}");
            foreach (ObjectEffect effect in effects)
            {
                effect.ApplyEffect(player);
                effect.ApplyEffect(other, player);
            }
        }
        else if (other.gameObject.CompareTag("Player"))
        {
            // If it has Player tag but no Player, search in hierarchy
            player = FindPlayerInHierarchy(other.gameObject);
            
            if (player != null)
            {
                Debug.Log($"Player found in hierarchy: {player.gameObject.name}");
                foreach (ObjectEffect effect in effects)
                {
                    effect.ApplyEffect(player);
                    effect.ApplyEffect(other, player);
                }
            }
            else
            {
                Debug.LogWarning($"GameObject with 'Player' tag hit {gameObject.name} but has no Player component anywhere in hierarchy!");
            }
        }
    }

    private Player FindPlayerInHierarchy(GameObject obj)
    {
        // Search in parent hierarchy
        Player parentPlayer = obj.GetComponentInParent<Player>();
        if (parentPlayer != null)
        {
            return parentPlayer;
        }

        // Search in children hierarchy
        Player childPlayer = obj.GetComponentInChildren<Player>();
        if (childPlayer != null)
        {
            return childPlayer;
        }

        // Search in siblings
        if (obj.transform.parent != null)
        {
            Player siblingPlayer = obj.transform.parent.GetComponentInChildren<Player>();
            if (siblingPlayer != null && siblingPlayer.gameObject != obj)
            {
                return siblingPlayer;
            }
        }

        return null;
    }
    protected virtual void HandlePlayerCollision(Collision playerCollision)
    {
        // Try to find Player from the collision
        Player player = playerCollision.gameObject.GetComponent<Player>();
        if (player == null && playerCollision.gameObject.CompareTag("Player"))
        {
            player = FindPlayerInHierarchy(playerCollision.gameObject);
        }
        
        Debug.Log($"Handling player collision with {effects.Length} effects");
        foreach (ObjectEffect effect in effects)
        {
            if (player != null)
            {
                effect.ApplyEffect(player);
                effect.ApplyEffect(playerCollision, player);
            }
        }
    }
    protected virtual void HandlePlayerCollision(Collision collision, Player player)
    {
        Debug.Log($"Handling player collision with {effects.Length} effects for player: {player.gameObject.name}");
        foreach (ObjectEffect effect in effects)
        {
            effect.ApplyEffect(player);
            effect.ApplyEffect(collision, player);
        }
    }
    private void EnsureColliderExists()
    {
        bool hasCollider = false;
        if (GetComponent<Collider>() == null)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = isTrigger;
                Debug.Log($"Added MeshCollider to {gameObject.name}");
            }
            else if (TryGetComponent<LODGroup>(out LODGroup lodGroup))
            {
                foreach (Transform lodChild in lodGroup.transform)
                {
                    if (lodChild.TryGetComponent<MeshRenderer>(out _) &&
                        !lodChild.TryGetComponent<Collider>(out _) &&
                        lodChild.TryGetComponent<MeshFilter>(out MeshFilter lodMeshFilter) &&
                        lodMeshFilter.sharedMesh != null)
                    {
                        var lodCollider = lodChild.gameObject.AddComponent<MeshCollider>();
                        lodCollider.sharedMesh = lodMeshFilter.sharedMesh;
                        lodCollider.convex = true;
                        lodCollider.isTrigger = isTrigger;
                        Debug.Log($"Added MeshCollider to LOD child: {lodChild.name}");
                        break;
                    }
                }
            }
            else
            {
                foreach (Transform child in transform)
                {
                    MeshFilter childMeshFilter = child.GetComponent<MeshFilter>();
                    Collider childCollider = child.GetComponent<Collider>();
                    if (childMeshFilter != null && childMeshFilter.sharedMesh != null && childCollider == null)
                    {
                        MeshCollider meshCollider = child.gameObject.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = childMeshFilter.sharedMesh;
                        meshCollider.convex = true;
                        meshCollider.isTrigger = isTrigger;
                        Debug.Log($"Added MeshCollider to child: {child.name}");
                        hasCollider = true;
                    }
                }
            }
        }
        else { hasCollider = true; }
        if (!hasCollider)
        {
            Debug.LogError("No Collider Can Be Added Please Add It Manually.");
        }
    }
    private void EnsureIsTrigger()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = isTrigger;
            Debug.Log($"Set {gameObject.name} collider as trigger.");
        }
        foreach (Transform child in transform)
        {
            Collider childCollider = child.GetComponent<Collider>();
            if (childCollider != null)
            {
                childCollider.isTrigger = isTrigger;
                Debug.Log($"Set {child.name} collider as trigger.");
            }
        }
    }
    private void EnsureRigidbodyExists()
    {
        if (GetComponent<Rigidbody>() == null)
        {
            gameObject.AddComponent<Rigidbody>();
        }
    }
    public void RefreshEffects()
    {
        effects = GetComponents<ObjectEffect>();
    }
}
public abstract class ObjectEffect : MonoBehaviour
{
    public virtual void ApplyEffect(Player player) { }
    public virtual void ApplyEffect(Collision playerCollision, Player player) { }
    public virtual void ApplyEffect(Collider playerCollider, Player player) { }
}
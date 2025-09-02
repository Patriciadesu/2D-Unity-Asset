using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class InteractableObject : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] bool usePhysic = true;        // Dynamic if true, Kinematic if false
    [SerializeField] bool useGravity = true;
    [SerializeField] bool isTrigger = false;

    private ObjectEffect[] effects;
    private Rigidbody2D rb2d;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();

        // Rigidbody2D config
        rb2d.bodyType = usePhysic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        if (isTrigger) useGravity = false;
        rb2d.gravityScale = useGravity ? 1f : 0f;

        effects = GetComponents<ObjectEffect>();

        EnsureRigidbodyExists2D();
        EnsureColliderExists2D();
        EnsureIsTrigger2D();
    }

    // ----- COLLISION (2D) -----
    void OnCollisionEnter2D(Collision2D collision)
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
            // If tagged Player but no Player component, search in hierarchy
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
            Debug.Log($"Collision2D with: {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        }
    }

    // ----- TRIGGER (2D) -----
    void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.gameObject.GetComponent<Player>();

        if (player != null)
        {
            Debug.Log($"Player with Player Hit: {other.gameObject.name}");
            foreach (var effect in effects)
            {
                effect.ApplyEffect(player);
                effect.ApplyEffect(other, player);
            }
        }
        else if (other.gameObject.CompareTag("Player"))
        {
            player = FindPlayerInHierarchy(other.gameObject);

            if (player != null)
            {
                Debug.Log($"Player found in hierarchy: {player.gameObject.name}");
                foreach (var effect in effects)
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

    // ----- HELPERS -----
    private Player FindPlayerInHierarchy(GameObject obj)
    {
        // Search in parent
        var parentPlayer = obj.GetComponentInParent<Player>();
        if (parentPlayer != null) return parentPlayer;

        // Search in children
        var childPlayer = obj.GetComponentInChildren<Player>();
        if (childPlayer != null) return childPlayer;

        // Search siblings via parent
        if (obj.transform.parent != null)
        {
            var siblingPlayer = obj.transform.parent.GetComponentInChildren<Player>();
            if (siblingPlayer != null && siblingPlayer.gameObject != obj)
                return siblingPlayer;
        }

        return null;
    }

    protected virtual void HandlePlayerCollision(Collision2D playerCollision)
    {
        Player player = playerCollision.gameObject.GetComponent<Player>();
        if (player == null && playerCollision.gameObject.CompareTag("Player"))
        {
            player = FindPlayerInHierarchy(playerCollision.gameObject);
        }

        if (effects == null) effects = GetComponents<ObjectEffect>();
        Debug.Log($"Handling player collision with {(effects?.Length ?? 0)} effects");

        if (player != null && effects != null)
        {
            foreach (var effect in effects)
            {
                effect.ApplyEffect(player);
                effect.ApplyEffect(playerCollision, player);
            }
        }
    }

    protected virtual void HandlePlayerCollision(Collision2D collision, Player player)
    {
        if (effects == null) effects = GetComponents<ObjectEffect>();
        Debug.Log($"Handling player collision with {(effects?.Length ?? 0)} effects for player: {player.gameObject.name}");

        if (effects != null)
        {
            foreach (var effect in effects)
            {
                effect.ApplyEffect(player);
                effect.ApplyEffect(collision, player);
            }
        }
    }

    // ----- ENSURE COMPONENTS (2D) -----
    private void EnsureColliderExists2D()
    {
        bool hasCollider = false;

        // If this object lacks a Collider2D, try to add one sensibly
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            // Prefer PolygonCollider2D if a SpriteRenderer with a Sprite exists (better fit)
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                var poly = gameObject.AddComponent<PolygonCollider2D>();
                poly.isTrigger = isTrigger;
                hasCollider = true;
                Debug.Log($"Added PolygonCollider2D to {gameObject.name}");
            }
            else
            {
                // Fallback: BoxCollider2D sized to Renderer bounds if possible
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.isTrigger = isTrigger;

                // Best-effort sizing from Renderer bounds
                var rend = GetComponent<Renderer>();
                if (rend != null)
                {
                    var b = rend.bounds;
                    box.size = new Vector2(b.size.x, b.size.y);
                    box.offset = Vector2.zero;
                }

                hasCollider = true;
                Debug.Log($"Added BoxCollider2D to {gameObject.name}");
            }
        }
        else
        {
            hasCollider = true;
        }

        // If still nothing (very unlikely), scan children for sprites and add to first viable child
        if (!hasCollider)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Collider2D>() == null)
                {
                    var childSR = child.GetComponent<SpriteRenderer>();
                    if (childSR != null && childSR.sprite != null)
                    {
                        var childPoly = child.gameObject.AddComponent<PolygonCollider2D>();
                        childPoly.isTrigger = isTrigger;
                        Debug.Log($"Added PolygonCollider2D to child: {child.name}");
                        hasCollider = true;
                        break;
                    }
                }
            }
        }

        if (!hasCollider)
        {
            Debug.LogError("No 2D Collider can be added automatically. Please add one manually.");
        }
    }

    private void EnsureIsTrigger2D()
    {
        // Set trigger flag on all Collider2D on this and immediate children (like your 3D version)
        foreach (var c in GetComponentsInChildren<Collider2D>())
        {
            c.isTrigger = isTrigger;
        }
    }

    private void EnsureRigidbodyExists2D()
    {
        if (rb2d == null)
            rb2d = gameObject.AddComponent<Rigidbody2D>();
    }

    // Allow runtime re-scan after adding/removing effects
    public void RefreshEffects()
    {
        effects = GetComponents<ObjectEffect>();
    }
}

// ----- EFFECT BASE (2D) -----
public abstract class ObjectEffect : MonoBehaviour
{
    public virtual void ApplyEffect(Player player) { }
    public virtual void ApplyEffect(Collision2D playerCollision, Player player) { }
    public virtual void ApplyEffect(Collider2D playerCollider, Player player) { }
}

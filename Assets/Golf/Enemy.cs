using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    [Header("Movement & Detection")]
    public float detectionRadius = 5f;
    public float moveSpeed = 3f;
    public LayerMask playerLayer;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;
    public int damage = 10;

    private Transform player;
    private bool isTouchingPlayer = false;
    private Coroutine attackCoroutine;

    void Update()
    {
        // Detect player in radius
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
        if (hit != null)
        {
            player = hit.transform;

            // Move only on X if not touching
            if (!isTouchingPlayer)
            {
                Vector3 targetPos = new Vector3(player.position.x, transform.position.y, transform.position.z);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            player = null;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            isTouchingPlayer = true;

            // Start attacking if not already
            if (attackCoroutine == null)
                attackCoroutine = StartCoroutine(AttackPlayer());
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            isTouchingPlayer = false;

            // Stop attacking
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }
        }
    }

    private IEnumerator AttackPlayer()
    {
        while (true)
        {
            if (player != null)
            {
                // Example: reduce player's health
                Player playerHealth = player.GetComponent<Player>();
                if (playerHealth != null)
                    playerHealth.TakeDamage(damage);
            }

            yield return new WaitForSeconds(attackCooldown);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

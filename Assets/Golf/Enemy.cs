using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Enemy : MonoBehaviour
{
    public float detectionRadius = 5f;    // How far the enemy can detect the player
    public float moveSpeed = 3f;          // Movement speed
    public LayerMask playerLayer;
    public bool collideplayer = false;// Assign your Player layer here
    public int damage;

    private Transform player;

    void Update()
    {
        // Check if player is within detection radius
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);

        if (hit != null && collideplayer == false )
        {
            player = hit.transform;

            // Move only on the X axis
            Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    // Optional: Draw the detection circle in the editor
  private void OnDrawGizmosSelected()
   {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, detectionRadius);
   }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collideplayer = true;
            // Implement what happens when the enemy collides with the player
            Debug.Log("Enemy collided with Player!");
        }
    }
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

            collideplayer = false;
            // Implement what happens when the enemy stops colliding with the player
            Debug.Log("Enemy stopped colliding with Player!");
        }
    }

}

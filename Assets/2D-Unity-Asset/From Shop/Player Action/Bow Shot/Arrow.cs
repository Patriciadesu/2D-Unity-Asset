using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float damage;
    public void SetUp(float _damage)
    {
        damage = _damage;
    }
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<Entity>(out Entity entity))
        {
            entity.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Entity>(out Entity entity))
        {
            entity.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }
}

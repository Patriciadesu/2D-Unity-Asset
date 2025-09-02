using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float damage;
    public void SetUp(float _damage)
    {
        damage = _damage;
    }
    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Boss>(out Boss boss))
        {
            boss.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Boss>(out Boss boss))
        {
            boss.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }
}

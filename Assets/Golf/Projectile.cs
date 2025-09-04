using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 7f;
    public int damage = 1;
    private Vector2 moveDirection;

    // ��駷�ȷҧ�������͹���
    public void Setup(Vector2 direction)
    {
        moveDirection = direction.normalized;
        Destroy(gameObject, 5f); // ź projectile ��ѧ 5 �Թҷ�
    }

    void Update()
    {
        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Destroy(this.gameObject);
            collision.GetComponent<Player>()?.TakeDamage(damage);

        }
    }
}

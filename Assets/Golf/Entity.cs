using UnityEngine;

public class Entity : MonoBehaviour
{
    public float maxhealth = 1;
    public float currenthealth;
    public virtual void Awake()
    {
        currenthealth = maxhealth;
    }
    public void TakeDamage(float damage)
    {
        currenthealth -= damage;
        if (currenthealth <= 0)
        {
            currenthealth = 0;
            Destroy(gameObject);
        }
    }
}

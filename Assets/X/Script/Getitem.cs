using UnityEngine;

public class Getitem : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log(gameObject.name + " : Has Destroy");
            Destroy(gameObject);
        }
        
    }
}

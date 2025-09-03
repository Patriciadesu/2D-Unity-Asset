using UnityEngine;

public class FlipUpsideDown : MonoBehaviour
{
    Camera gameCamera;
    private bool worldFlipped = false;

    private void Start()
    {

        gameCamera = Camera.main;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            worldFlipped = !worldFlipped;
            FlipWorld(worldFlipped);
        }
    }

    private void FlipWorld(bool flip)
    {
        Player.Instance.axis *= -1;


        if (gameCamera != null)
        {

            gameCamera.transform.rotation = flip
                ? Quaternion.Euler(0f, 0f, 180f)
                : Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("GameCamera not assigned in FlipWorldOnCollision.");
        }
    }
}

using UnityEngine;

[ExecuteInEditMode]
public class AutoUpdateObject : MonoBehaviour
{
    [SerializeField] GameObject[] oppositeToggleObjects;
    private void OnEnable()
    {
        foreach (GameObject go in oppositeToggleObjects)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }
    }
    private void OnDisable()
    {
        foreach (GameObject go in oppositeToggleObjects)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }
    }
}

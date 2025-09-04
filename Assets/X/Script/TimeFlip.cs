using NaughtyAttributes;
using UnityEngine;

public class TimeFlip : PlayerExtension
{
    [Foldout("Rewind Control", true)] public KeyCode rewindKey = KeyCode.R;       // ปุ่มย้อนเวลา (กดค้าง)
    [Foldout("Rewind Control", true)] public float rewindTime = 3f;

    GameObject player;
    Vector3 PositionToRewind;
    Quaternion RotationToRewind;
    float elapsed;

    void Start()
    {
        player = gameObject;
        PositionToRewind = player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        elapsed += Time.deltaTime;

        if (Input.GetKeyDown(rewindKey))
        {
            Rewind();
        }

    }
    private void FixedUpdate()
    {
        SavePoint();
    }

    void SavePoint()
    {
        if (elapsed >= rewindTime)
        {
            Debug.Log("Position has Saved!!");
            PositionToRewind = player.transform.position;
            elapsed = 0f;
        }
    }
    void Rewind()
    {
        player.transform.position = PositionToRewind;
    }
}

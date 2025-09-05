using NaughtyAttributes;
using UnityEngine;

public class RewindTime : PlayerExtension
{
    public KeyCode rewindKey = KeyCode.R;
    [Foldout("Rewind Control", true)] public float rewindTime = 3f;
    [Foldout("Rewind Control", true)] public float cooldown = 3f;

    GameObject player;
    Vector3 PositionToRewind;
    Quaternion RotationToRewind;
    float elapsed;
    float cooldownTime;
    bool isReady = false;

    void Start()
    {
        player = gameObject;
        PositionToRewind = player.transform.position;
        RotationToRewind = player.transform .rotation;
    }

    // Update is called once per frame
    void Update()
    {
        elapsed += Time.deltaTime;
        cooldownTime -= Time.deltaTime;
        Debug.Log(cooldownTime);

        if (Input.GetKeyDown(rewindKey))
        {
            onCoolDown();
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
            RotationToRewind = player.transform.rotation;
            elapsed = 0f;
        }
    }
    void onCoolDown()
    {
        if (cooldownTime <= 0f)
        {
            isReady = true;
            cooldownTime = cooldown;
        }else
        {
            isReady = false;
        }
    }
    void Rewind()
    {
        if (isReady)
        {
            player.transform.position = PositionToRewind;
            player.transform.rotation = RotationToRewind;
        }
    }
}

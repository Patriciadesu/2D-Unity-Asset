using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class Magnet : PlayerExtension
{
    [Foldout("Targets", true)] public List<GameObject> canPullObj = new List<GameObject>();   // จะถูกเติมอัตโนมัติจากการสแกน
    [Foldout("Targets", true)] public LayerMask targetLayers;                                  // เลเยอร์ของวัตถุที่ให้ดูด (เช่น "Magnetable")

    [Foldout("Magnet Settings", true)] public float range = 5f;                          // รัศมีดูด
    [Foldout("Magnet Settings", true)] public float pullForce = 15f;                                   // แรงดึง (หน่วยเป็น Acceleration ถ้ามี Rigidbody)
    [Foldout("Magnet Settings", true)] public float maxSpeed = 10f;                                    // จำกัดความเร็วสูงสุดของชิ้นที่ถูกดูด
    [Foldout("Magnet Settings", true)] public float stopDistance = 0.4f;                               // ระยะหยุดเมื่อเข้ามาใกล้มากพอ

    [Foldout("Scan Settings", true)] public bool autoScan = true;                          // สแกนรอบ ๆ อัตโนมัติ
    [Foldout("Scan Settings", true)] public float rescanInterval = 0.25f;                            // ความถี่ในการสแกน (วินาที)

    float _scanTimer;

    void Update()
    {
        if (autoScan)
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                ScanAround();
                _scanTimer = rescanInterval;
            }
        }
    }

    void FixedUpdate()
    {
        // ลูปดึงทุกชิ้นในลิสต์
        for (int i = canPullObj.Count - 1; i >= 0; i--)
        {
            GameObject obj = canPullObj[i];

            // ลบของว่าง หรือถูกทำลาย
            if (obj == null)
            {
                canPullObj.RemoveAt(i);
                continue;
            }

            Vector3 toPlayer = transform.position - obj.transform.position;
            float dist = toPlayer.magnitude;

            // อยู่นอกรัศมี -> ข้าม (ถ้าอยากเอาออกจากลิสต์เลยก็ได้)
            if (dist > range)
                continue;

            // ใกล้พอแล้ว -> หยุด
            if (dist <= stopDistance)
                continue;

            Vector3 dir = toPlayer / Mathf.Max(dist, 0.0001f);

            // ถ้ามี Rigidbody ใช้แรงดึงแบบฟิสิกส์
            if (obj.TryGetComponent<Rigidbody>(out var rb))
            {
                // ดึงด้วยความเร่งคงที่
                rb.AddForce(dir * pullForce, ForceMode.Acceleration);

                // จำกัดความเร็วไม่ให้ปลิวแรงเกินไป
                if (rb.linearVelocity.magnitude > maxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                }
            }
            else
            {
                // ไม่มี Rigidbody: ขยับตำแหน่งแบบนิ่ม ๆ
                float moveSpeed = pullForce; // ใช้ค่าเดียวกับแรง เพื่อให้ปรับง่าย
                obj.transform.position += dir * moveSpeed * Time.fixedDeltaTime;
            }
        }
    }

    // สแกนหาวัตถุในรัศมีแล้วใส่ลิสต์
    void ScanAround()
    {
        // หา Collider ในรัศมี (ไม่สนใจ Trigger เพื่อเลี่ยง UI/พื้นที่)
        Collider[] hits = Physics.OverlapSphere(transform.position, range, targetLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            // ถ้ามี Rigidbody ให้เลือกตัว GO จาก Rigidbody เพื่อหลีกเลี่ยงชิ้นส่วนย่อย
            GameObject go = hits[i].attachedRigidbody ? hits[i].attachedRigidbody.gameObject : hits[i].gameObject;

            if (go != null && !canPullObj.Contains(go) && go != gameObject)
            {
                canPullObj.Add(go);
            }
        }
    }

    // เผื่ออยากลงทะเบียนเป้าหมายเองจากสคริปต์อื่น
    public void RegisterTarget(GameObject obj)
    {
        if (obj != null && !canPullObj.Contains(obj))
            canPullObj.Add(obj);
    }

    public void UnregisterTarget(GameObject obj)
    {
        if (obj != null)
            canPullObj.Remove(obj);
    }

    // วาดรัศมีใน Scene ให้เห็นง่าย
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}

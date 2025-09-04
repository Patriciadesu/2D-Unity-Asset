using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class Magnet : PlayerExtension
{
    [Foldout("Targets", true)] public List<GameObject> canPullObj = new List<GameObject>();   // �ж١����ѵ��ѵԨҡ����᡹
    [Foldout("Targets", true)] public LayerMask targetLayers;                                  // �������ͧ�ѵ�ط�����ٴ (�� "Magnetable")

    [Foldout("Magnet Settings", true)] public float range = 5f;                          // ����մٴ
    [Foldout("Magnet Settings", true)] public float pullForce = 15f;                                   // �ç�֧ (˹����� Acceleration ����� Rigidbody)
    [Foldout("Magnet Settings", true)] public float maxSpeed = 10f;                                    // �ӡѴ���������٧�ش�ͧ��鹷��١�ٴ
    [Foldout("Magnet Settings", true)] public float stopDistance = 0.4f;                               // ������ش��������������ҡ��

    [Foldout("Scan Settings", true)] public bool autoScan = true;                          // �᡹�ͺ � �ѵ��ѵ�
    [Foldout("Scan Settings", true)] public float rescanInterval = 0.25f;                            // �������㹡���᡹ (�Թҷ�)

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
        // �ٻ�֧�ء������ʵ�
        for (int i = canPullObj.Count - 1; i >= 0; i--)
        {
            GameObject obj = canPullObj[i];

            // ź�ͧ��ҧ ���Ͷ١�����
            if (obj == null)
            {
                canPullObj.RemoveAt(i);
                continue;
            }

            Vector3 toPlayer = transform.position - obj.transform.position;
            float dist = toPlayer.magnitude;

            // ����͡����� -> ���� (�����ҡ����͡�ҡ��ʵ���¡���)
            if (dist > range)
                continue;

            // �������� -> ��ش
            if (dist <= stopDistance)
                continue;

            Vector3 dir = toPlayer / Mathf.Max(dist, 0.0001f);

            // ����� Rigidbody ���ç�֧Ẻ���ԡ��
            if (obj.TryGetComponent<Rigidbody>(out var rb))
            {
                // �֧���¤�����觤����
                rb.AddForce(dir * pullForce, ForceMode.Acceleration);

                // �ӡѴ������������������ç�Թ�
                if (rb.linearVelocity.magnitude > maxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                }
            }
            else
            {
                // ����� Rigidbody: ��Ѻ���˹�Ẻ���� �
                float moveSpeed = pullForce; // �������ǡѺ�ç ��������Ѻ����
                obj.transform.position += dir * moveSpeed * Time.fixedDeltaTime;
            }
        }
    }

    // �᡹���ѵ�����������������ʵ�
    void ScanAround()
    {
        // �� Collider ������ (���ʹ� Trigger ��������§ UI/��鹷��)
        Collider[] hits = Physics.OverlapSphere(transform.position, range, targetLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            // ����� Rigidbody ������͡��� GO �ҡ Rigidbody ������ա����§�����ǹ����
            GameObject go = hits[i].attachedRigidbody ? hits[i].attachedRigidbody.gameObject : hits[i].gameObject;

            if (go != null && !canPullObj.Contains(go) && go != gameObject)
            {
                canPullObj.Add(go);
            }
        }
    }

    // ������ҡŧ����¹��������ͧ�ҡʤ�Ի�����
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

    // �Ҵ������ Scene �����繧���
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}

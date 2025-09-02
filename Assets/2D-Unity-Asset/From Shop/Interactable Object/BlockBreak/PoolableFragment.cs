using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PoolableFragment : MonoBehaviour
{
    [HideInInspector] public ObjectPool pool;
    [Header("Fragment Settings")]
    public float initialForceRange = 5f;
    public float lifetime = 3f;
    private Rigidbody rb;
    private EnhancedFragmentBounce bounceScript;
    private Coroutine returnCoroutine;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        bounceScript = GetComponent<EnhancedFragmentBounce>();
        if (bounceScript != null)
        {
            bounceScript.fragmentRb = rb;
        }
        rb.mass = Random.Range(0.5f, 2f);
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.05f;
    }
    private void OnEnable()
    {
        ApplyInitialForce();
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }
        returnCoroutine = StartCoroutine(ReturnAfterTime());
    }
    private void ApplyInitialForce()
    {
        if (rb != null)
        {
            Vector3 randomDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(0.5f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            float forceAmount = Random.Range(initialForceRange * 0.5f, initialForceRange);
            rb.AddForce(randomDirection * forceAmount, ForceMode.Impulse);
            Vector3 randomTorque = Random.insideUnitSphere * forceAmount * 0.5f;
            rb.AddTorque(randomTorque, ForceMode.Impulse);
        }
    }
    private IEnumerator ReturnAfterTime()
    {
        yield return new WaitForSeconds(lifetime);
        ReturnToPool();
    }
    public void ReturnToPool()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        if (bounceScript != null)
        {
        }
        if (pool != null)
        {
            pool.ReturnObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnDisable()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }
}
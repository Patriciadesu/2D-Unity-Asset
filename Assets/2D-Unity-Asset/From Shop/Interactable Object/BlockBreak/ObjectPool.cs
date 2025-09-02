using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPool
{
    private Queue<GameObject> objectPool;
    private GameObject prefab;
    private Transform poolParent;
    public void Initialize(int poolSize, GameObject prefab)
    {
        this.prefab = prefab;
        objectPool = new Queue<GameObject>();
        GameObject poolParentObj = new GameObject("FragmentPool");
        poolParent = poolParentObj.transform;
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = CreateNewObject();
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }
    }
    public void Initialize(int poolSize, System.Func<GameObject> createFunction)
    {
        objectPool = new Queue<GameObject>();
        GameObject poolParentObj = new GameObject("FragmentPool");
        poolParent = poolParentObj.transform;
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = createFunction();
            obj.transform.SetParent(poolParent);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }
    }
    private GameObject CreateNewObject()
    {
        GameObject obj = Object.Instantiate(prefab);
        obj.transform.SetParent(poolParent);
        return obj;
    }
    public GameObject GetObject()
    {
        if (objectPool.Count > 0)
        {
            GameObject obj = objectPool.Dequeue();
            obj.SetActive(true);
            obj.transform.localScale = Vector3.one;
            obj.transform.rotation = Quaternion.identity;
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            EnhancedFragmentBounce bounce = obj.GetComponent<EnhancedFragmentBounce>();
            if (bounce != null)
            {
                bounce.SendMessage("ResetBounceCount", SendMessageOptions.DontRequireReceiver);
            }
            return obj;
        }
        else
        {
            GameObject obj = CreateNewObject();
            obj.SetActive(true);
            return obj;
        }
    }
    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(poolParent);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Material mat = renderer.material;
            if (mat.HasProperty("_BaseColor"))
            {
                Color color = mat.GetColor("_BaseColor");
                color.a = 1f;
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Surface", 0);
                mat.renderQueue = 2000;
            }
        }
        objectPool.Enqueue(obj);
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class BlockBreakEffect : ObjectEffect
{
    [Header("Fragment Settings")]
    [SerializeField] private int fragmentCount = 20;
    [SerializeField] private float minFragmentSize = 0.05f;
    [SerializeField] private float maxFragmentSize = 0.15f;
    [SerializeField] private float explosionForce = 8f;
    [SerializeField] private float fragmentLifetime = 4f;
    [SerializeField] private float fragmentSpread = 1f;
    [Header("Physics Settings")]
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float gravityMultiplier = 1f;
    [SerializeField] private float fragmentDrag = 0.3f;
    [SerializeField] private float fragmentAngularDrag = 0.3f;
    [SerializeField] private float fragmentBounce = 0.5f;
    [SerializeField] private float minFragmentMass = 0.01f;
    [SerializeField] private float maxFragmentMass = 0.05f;
    [Header("Object Pooling")]
    [SerializeField] private int poolSize = 100;
    [SerializeField] private GameObject fragmentPrefab;
    private Renderer objectRenderer;
    private Collider objectCollider;
    private MeshFilter meshFilter;
    private bool hasBeenDestroyed = false;
    private static ObjectPool fragmentPool;
    private static bool poolInitialized = false;
    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        objectCollider = GetComponent<Collider>();
        meshFilter = GetComponent<MeshFilter>();
        if (!poolInitialized)
        {
            InitializeFragmentPool();
        }
    }
    private void InitializeFragmentPool()
    {
        if (fragmentPool == null)
        {
            fragmentPool = new ObjectPool();
            fragmentPool.Initialize(poolSize, CreateFragmentPrefab);
            poolInitialized = true;
            Debug.Log("Fragment object pool initialized with size: " + poolSize);
        }
    }
    private GameObject CreateFragmentPrefab()
    {
        GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fragment.name = "BlockFragment";
        fragment.layer = LayerMask.NameToLayer("Default");
        Rigidbody rb = fragment.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = Random.Range(minFragmentMass, maxFragmentMass);
            rb.linearDamping = fragmentDrag;
            rb.angularDamping = fragmentAngularDrag;
            rb.useGravity = useGravity;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.automaticCenterOfMass = true;
            rb.automaticInertiaTensor = true;
            if (gravityMultiplier != 1f)
            {
                rb.useGravity = false;
                fragment.AddComponent<CustomGravity>().gravityMultiplier = gravityMultiplier;
            }
        }
        BoxCollider collider = fragment.GetComponent<BoxCollider>();
        if (collider != null)
        {
            PhysicsMaterial bounceMaterial = new PhysicsMaterial("FragmentBounce");
            bounceMaterial.bounciness = fragmentBounce;
            bounceMaterial.dynamicFriction = 0.4f;
            bounceMaterial.staticFriction = 0.4f;
            bounceMaterial.frictionCombine = PhysicsMaterialCombine.Average;
            bounceMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
            collider.material = bounceMaterial;
            collider.isTrigger = false;
        }
        EnhancedFragmentBounce bounceScript = fragment.AddComponent<EnhancedFragmentBounce>();
        bounceScript.bounceForce = fragmentBounce;
        bounceScript.fragmentRb = rb;
        PoolableFragment poolable = fragment.AddComponent<PoolableFragment>();
        poolable.pool = fragmentPool;
        fragment.SetActive(false);
        return fragment;
    }
    public override void ApplyEffect(Collision2D playerCollision, Player player)
    {
        if (playerCollision.contacts == null || playerCollision.contacts.Length == 0)
        {
            Debug.LogWarning("No collision contacts found!");
            return;
        }
        if (player != null && !hasBeenDestroyed)
        {
            Vector3 hitPoint = playerCollision.contacts[0].point;
            Vector3 hitNormal = playerCollision.contacts[0].normal;
            Debug.Log($"BlockBreakEffect triggered by {player.gameObject.name} at point: {hitPoint}");
            BreakBlock(hitPoint, hitNormal);
        }
    }
    private void BreakBlock(Vector3 hitPoint, Vector3 hitNormal)
    {
        hasBeenDestroyed = true;
        Debug.Log($"Breaking block: {gameObject.name} at hit point: {hitPoint}");
        Color blockColor = GetBlockColor();
        Debug.Log($"Block color: {blockColor}");
        CreateCubeFragments(hitPoint, hitNormal, blockColor);
        if (objectRenderer != null)
        {
            objectRenderer.enabled = false;
            Debug.Log("Disabled original object renderer");
        }
        if (objectCollider != null)
        {
            objectCollider.enabled = false;
            Debug.Log("Disabled original object collider");
        }
        Destroy(gameObject);
        Debug.Log($"{gameObject.name} destroyed immediately");
    }
    private Color GetBlockColor()
    {
        if (objectRenderer != null && objectRenderer.material != null)
        {
            Material mat = objectRenderer.material;
            if (mat.HasProperty("_Color"))
                return mat.color;
            else if (mat.HasProperty("_BaseColor"))
                return mat.GetColor("_BaseColor");
            else if (mat.HasProperty("_MainColor"))
                return mat.GetColor("_MainColor");
            else if (mat.mainTexture != null)
            {
                return SampleTextureColor(mat.mainTexture as Texture2D);
            }
        }
        return Color.white;
    }
    private Color SampleTextureColor(Texture2D texture)
    {
        if (texture == null) return Color.white;
        try
        {
            return texture.GetPixel(texture.width / 2, texture.height / 2);
        }
        catch
        {
            return Color.white;
        }
    }
    private void CreateCubeFragments(Vector3 hitPoint, Vector3 hitNormal, Color blockColor)
    {
        Vector3 blockCenter = transform.position;
        Vector3 blockSize = GetBlockSize();
        Debug.Log($"Creating {fragmentCount} fragments at center: {blockCenter}, size: {blockSize}");
        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = fragmentPool.GetObject();
            if (fragment == null)
            {
                Debug.LogWarning("Failed to get fragment from pool!");
                continue;
            }
            SetupFragment(fragment, blockColor, blockCenter, blockSize, hitPoint, hitNormal, i);
        }
        Debug.Log($"Successfully created {fragmentCount} cube fragments!");
    }
    private void SetupFragment(GameObject fragment, Color blockColor, Vector3 blockCenter, Vector3 blockSize, Vector3 hitPoint, Vector3 hitNormal, int index)
    {
        float size = Random.Range(minFragmentSize, maxFragmentSize);
        fragment.transform.localScale = Vector3.one * size;
        Vector3 randomPos = blockCenter + new Vector3(
            Random.Range(-blockSize.x * 0.4f, blockSize.x * 0.4f),
            Random.Range(-blockSize.y * 0.4f, blockSize.y * 0.4f),
            Random.Range(-blockSize.z * 0.4f, blockSize.z * 0.4f)
        );
        fragment.transform.position = randomPos;
        fragment.transform.rotation = Random.rotation;
        SetupFragmentMaterial(fragment, blockColor, size);
        Rigidbody fragmentRb = fragment.GetComponent<Rigidbody>();
        if (fragmentRb != null)
        {
            fragmentRb.linearVelocity = Vector3.zero;
            fragmentRb.angularVelocity = Vector3.zero;
            fragmentRb.mass = Random.Range(minFragmentMass, maxFragmentMass);
            fragmentRb.useGravity = useGravity;
            fragmentRb.linearDamping = fragmentDrag;
            fragmentRb.angularDamping = fragmentAngularDrag;
            Vector3 explosionDirection;
            explosionDirection = hitNormal + Random.insideUnitSphere * fragmentSpread;
            explosionDirection = explosionDirection.normalized;
            explosionDirection.y += Random.Range(0.2f, 0.8f);
            explosionDirection = explosionDirection.normalized;
            float randomForce = explosionForce * Random.Range(0.7f, 1.3f) * (1f + size);
            fragmentRb.AddForce(explosionDirection * randomForce, ForceMode.Impulse);
            Vector3 randomTorque = Random.insideUnitSphere * explosionForce * Random.Range(0.3f, 0.8f);
            fragmentRb.AddTorque(randomTorque, ForceMode.Impulse);
            Debug.Log($"Fragment {index}: Force={explosionDirection * randomForce}, Mass={fragmentRb.mass}, Gravity={fragmentRb.useGravity}");
        }
        StartCoroutine(FadeOutFragment(fragment, fragmentLifetime));
    }
    private void SetupFragmentMaterial(GameObject fragment, Color blockColor, float size)
    {
        Renderer renderer = fragment.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material fragmentMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color colorVariation = blockColor;
            float variation = Random.Range(-0.1f, 0.1f);
            colorVariation.r = Mathf.Clamp01(colorVariation.r + variation);
            colorVariation.g = Mathf.Clamp01(colorVariation.g + variation);
            colorVariation.b = Mathf.Clamp01(colorVariation.b + variation);
            fragmentMat.SetColor("_BaseColor", colorVariation);
            fragmentMat.SetFloat("_Metallic", Random.Range(0.0f, 0.2f));
            fragmentMat.SetFloat("_Smoothness", Random.Range(0.1f, 0.4f));
            renderer.material = fragmentMat;
        }
    }
    private IEnumerator FadeOutFragment(GameObject fragment, float lifetime)
    {
        yield return new WaitForSeconds(lifetime * 0.7f);
        Renderer renderer = fragment.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            Material mat = renderer.material;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.renderQueue = 3000;
                Color originalColor = mat.GetColor("_BaseColor");
                float fadeTime = lifetime * 0.3f;
                float elapsedTime = 0f;
                while (elapsedTime < fadeTime && fragment.activeInHierarchy)
                {
                    elapsedTime += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                    Color newColor = originalColor;
                    newColor.a = alpha;
                    mat.SetColor("_BaseColor", newColor);
                    yield return null;
                }
            }
        }
        PoolableFragment poolable = fragment.GetComponent<PoolableFragment>();
        if (poolable != null)
        {
            poolable.ReturnToPool();
        }
        else
        {
            Destroy(fragment);
        }
    }
    private Vector3 GetBlockSize()
    {
        if (objectRenderer != null)
        {
            return objectRenderer.bounds.size;
        }
        else if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh.bounds.size;
        }
        return Vector3.one;
    }
}
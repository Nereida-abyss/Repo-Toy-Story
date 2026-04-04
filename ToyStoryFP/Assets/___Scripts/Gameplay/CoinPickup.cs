using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class CoinPickup : MonoBehaviour
{
    private static Material sharedCoinMaterial;

    [SerializeField] private int coinValue = 1;
    [SerializeField] private float pickupRadius = 1.1f;
    [SerializeField] private float rotationSpeed = 165f;
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobFrequency = 2.2f;
    [SerializeField] private float pickupDelay = 0.08f;
    [SerializeField] private Vector3 visualScale = new Vector3(0.11f, 0.018f, 0.11f);
    [SerializeField] private Vector3 visualLocalOffset = new Vector3(0f, 0.1f, 0f);

    private float spawnTime;
    private Vector3 basePosition;
    private Transform visualRoot;
    private bool collected;

    public static CoinPickup Spawn(Vector3 worldPosition, int value)
    {
        GameObject coinObject = new GameObject("CoinPickup");
        coinObject.name = "CoinPickup";
        coinObject.transform.position = worldPosition;

        CoinPickup pickup = coinObject.AddComponent<CoinPickup>();
        pickup.coinValue = Mathf.Max(1, value);
        pickup.ConfigureRuntimeCoin();
        return pickup;
    }

    void Awake()
    {
        ConfigureRuntimeCoin();
    }

    void OnEnable()
    {
        spawnTime = Time.time;
        basePosition = transform.position;
    }

    void Update()
    {
        float bobOffset = Mathf.Sin((Time.time - spawnTime) * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = basePosition + Vector3.up * bobOffset;

        if (visualRoot != null)
        {
            visualRoot.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void ConfigureRuntimeCoin()
    {
        basePosition = transform.position;

        foreach (Collider existingCollider in GetComponents<Collider>())
        {
            if (!(existingCollider is SphereCollider))
            {
                Destroy(existingCollider);
            }
        }

        SphereCollider triggerCollider = GetComponent<SphereCollider>();

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<SphereCollider>();
        }

        triggerCollider.isTrigger = true;
        triggerCollider.radius = Mathf.Max(0.1f, pickupRadius);

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        EnsureVisual();
    }

    private void TryCollect(Collider other)
    {
        if (collected)
        {
            return;
        }

        if (Time.time < spawnTime + pickupDelay)
        {
            return;
        }

        PlayerCurrencyController playerCurrency = other.GetComponentInParent<PlayerCurrencyController>();

        if (playerCurrency == null)
        {
            return;
        }

        collected = true;
        SphereCollider triggerCollider = GetComponent<SphereCollider>();

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        playerCurrency.AddCoins(coinValue);
        Destroy(gameObject);
    }

    private void EnsureVisual()
    {
        if (visualRoot == null)
        {
            Transform existingVisual = transform.Find("CoinVisual");
            visualRoot = existingVisual;
        }

        if (visualRoot == null)
        {
            GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visualObject.name = "CoinVisual";
            visualRoot = visualObject.transform;
            visualRoot.SetParent(transform, false);

            Collider visualCollider = visualObject.GetComponent<Collider>();

            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }
        }

        visualRoot.localPosition = visualLocalOffset;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = visualScale;

        MeshRenderer meshRenderer = visualRoot.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sharedMaterial = GetSharedCoinMaterial();
        }
    }

    private static Material GetSharedCoinMaterial()
    {
        if (sharedCoinMaterial != null)
        {
            return sharedCoinMaterial;
        }

        Shader coinShader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Sprites/Default");

        if (coinShader == null)
        {
            return null;
        }

        sharedCoinMaterial = new Material(coinShader)
        {
            color = new Color(1f, 0.82f, 0.18f, 1f)
        };

        return sharedCoinMaterial;
    }
}

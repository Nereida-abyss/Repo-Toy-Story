using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class CoinPickup : MonoBehaviour
{
    private const string IgnoreRaycastLayerName = "Ignore Raycast";

    private static Material sharedCoinMaterial;

    [SerializeField] private int coinValue = 1;
    [SerializeField] private CoinPickupProfile pickupProfile;
    [SerializeField] private float pickupRadius = 0.2f;
    [SerializeField] private float rotationSpeed = 165f;
    [SerializeField] private float bobAmplitude = 0.03f;
    [SerializeField] private float bobFrequency = 2.2f;
    [SerializeField] private float pickupDelay = 0.08f;
    [SerializeField] private Vector3 visualScale = new Vector3(0.05f, 0.008f, 0.05f);
    [SerializeField] private Vector3 visualLocalOffset = new Vector3(0f, 0.04f, 0f);

    private float spawnTime;
    private Vector3 basePosition;
    private Transform visualRoot;
    private bool collected;
    private bool missingProfileWarningShown;

    // Gestiona spawn.
    public static CoinPickup Spawn(Vector3 worldPosition, int value, CoinPickupProfile profile = null)
    {
        GameObject coinObject = new GameObject("CoinPickup");
        coinObject.name = "CoinPickup";
        coinObject.transform.position = worldPosition;

        CoinPickup pickup = coinObject.AddComponent<CoinPickup>();
        pickup.coinValue = Mathf.Max(1, value);
        pickup.pickupProfile = profile;
        pickup.ConfigureRuntimeCoin();
        return pickup;
    }

    void Awake()
    {
        ConfigureRuntimeCoin();
    }

    void Start()
    {
        ApplyProfile(warnIfMissing: true);
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

    // Configura runtime moneda.
    private void ConfigureRuntimeCoin()
    {
        ApplyProfile(warnIfMissing: false);
        basePosition = transform.position;
        ApplyIgnoreRaycastLayer(gameObject);

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
        triggerCollider.radius = Mathf.Max(0.02f, pickupRadius);

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        EnsureVisual();
    }

    private void ApplyProfile(bool warnIfMissing)
    {
        if (pickupProfile == null)
        {
            if (warnIfMissing && !missingProfileWarningShown)
            {
                GameDebug.Advertencia(
                    "Jugador",
                    "CoinPickup no tiene CoinPickupProfile asignado. Se usaran los valores locales del componente.",
                    this);
                missingProfileWarningShown = true;
            }

            return;
        }

        pickupRadius = pickupProfile.PickupRadius;
        rotationSpeed = pickupProfile.RotationSpeed;
        bobAmplitude = pickupProfile.BobAmplitude;
        bobFrequency = pickupProfile.BobFrequency;
        pickupDelay = pickupProfile.PickupDelay;
        visualScale = pickupProfile.VisualScale;
        visualLocalOffset = pickupProfile.VisualLocalOffset;
    }

    // Intenta collect.
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

    // Asegura visual.
    private void EnsureVisual()
    {
        if (visualRoot == null)
        {
            visualRoot = FindChildByName("CoinVisual");
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

        ApplyIgnoreRaycastLayer(visualRoot.gameObject);
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

    // Obtiene shared moneda material.
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

    // Aplica ignore raycast layer.
    private static void ApplyIgnoreRaycastLayer(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        int ignoreRaycastLayer = LayerMask.NameToLayer(IgnoreRaycastLayerName);

        if (ignoreRaycastLayer < 0)
        {
            return;
        }

        target.layer = ignoreRaycastLayer;
    }

    // Busca hijo por nombre.
    private Transform FindChildByName(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform candidate = children[i];

            if (candidate != null && candidate.name == childName)
            {
                return candidate;
            }
        }

        return null;
    }
}

using UnityEngine;

[CreateAssetMenu(fileName = "DefaultCoinPickupProfile", menuName = "Player/Coin Pickup Profile")]
public class CoinPickupProfile : ScriptableObject
{
    [SerializeField] [Min(0.02f)] private float pickupRadius = 0.2f;
    [SerializeField] private float rotationSpeed = 165f;
    [SerializeField] [Min(0f)] private float bobAmplitude = 0.03f;
    [SerializeField] [Min(0f)] private float bobFrequency = 2.2f;
    [SerializeField] [Min(0f)] private float pickupDelay = 0.08f;
    [SerializeField] private Vector3 visualScale = new Vector3(0.05f, 0.008f, 0.05f);
    [SerializeField] private Vector3 visualLocalOffset = new Vector3(0f, 0.04f, 0f);

    public float PickupRadius => pickupRadius;
    public float RotationSpeed => rotationSpeed;
    public float BobAmplitude => bobAmplitude;
    public float BobFrequency => bobFrequency;
    public float PickupDelay => pickupDelay;
    public Vector3 VisualScale => visualScale;
    public Vector3 VisualLocalOffset => visualLocalOffset;
}

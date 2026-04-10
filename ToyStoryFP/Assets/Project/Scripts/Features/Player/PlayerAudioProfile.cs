using UnityEngine;

[CreateAssetMenu(fileName = "DefaultPlayerAudioProfile", menuName = "Player/Player Audio Profile")]
public class PlayerAudioProfile : ScriptableObject
{
    [Header("Jump")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] [Range(0f, 1f)] private float jumpVolume = 0.5f;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepClips = System.Array.Empty<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float footstepVolume = 0.12f;
    [SerializeField] private float footstepMinInterval = 0.3f;
    [SerializeField] private float footstepMaxInterval = 0.48f;
    [SerializeField] private float footstepMinMoveAmount = 0.2f;
    [SerializeField] private float footstepPitchRandomness = 0.03f;

    [Header("Weapon Switch")]
    [SerializeField] private AudioClip weaponSwitchClip;
    [SerializeField] [Range(0f, 1f)] private float weaponSwitchVolume = 0.28f;

    [Header("Coin Pickup")]
    [SerializeField] private AudioClip coinPickupClip;
    [SerializeField] [Range(0f, 1f)] private float coinPickupVolume = 0.24f;
    [SerializeField] private float coinPickupPitchRandomness = 0.04f;

    [Header("Kill Confirm")]
    [SerializeField] private AudioClip killConfirmClip;
    [SerializeField] [Range(0f, 1f)] private float killConfirmVolume = 0.22f;

    [Header("Damage")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] [Range(0f, 1f)] private float hurtVolume = 0.2f;
    [SerializeField] private float hurtPitchRandomness = 0.035f;
    [SerializeField] private float hurtMinInterval = 0.08f;

    public AudioClip JumpClip => jumpClip;
    public float JumpVolume => jumpVolume;
    public AudioClip[] FootstepClips => footstepClips;
    public float FootstepVolume => footstepVolume;
    public float FootstepMinInterval => footstepMinInterval;
    public float FootstepMaxInterval => footstepMaxInterval;
    public float FootstepMinMoveAmount => footstepMinMoveAmount;
    public float FootstepPitchRandomness => footstepPitchRandomness;
    public AudioClip WeaponSwitchClip => weaponSwitchClip;
    public float WeaponSwitchVolume => weaponSwitchVolume;
    public AudioClip CoinPickupClip => coinPickupClip;
    public float CoinPickupVolume => coinPickupVolume;
    public float CoinPickupPitchRandomness => coinPickupPitchRandomness;
    public AudioClip KillConfirmClip => killConfirmClip;
    public float KillConfirmVolume => killConfirmVolume;
    public AudioClip HurtClip => hurtClip;
    public float HurtVolume => hurtVolume;
    public float HurtPitchRandomness => hurtPitchRandomness;
    public float HurtMinInterval => hurtMinInterval;
}

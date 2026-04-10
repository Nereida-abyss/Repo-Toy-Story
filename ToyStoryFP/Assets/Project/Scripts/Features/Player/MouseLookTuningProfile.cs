using UnityEngine;

[CreateAssetMenu(fileName = "DefaultMouseLookTuningProfile", menuName = "Player/Mouse Look Tuning Profile")]
public class MouseLookTuningProfile : ScriptableObject
{
    [Header("Mouse Look Settings")]
    [SerializeField] private Vector2 clampInDegrees = new Vector2(360f, 180f);
    [SerializeField] private bool lockCursor = true;
    [SerializeField] [Min(0f)] private float postUnpauseLookBlockDuration = 0.08f;
    [SerializeField] private bool pauseInputResetOnOpen = true;
    [SerializeField] private bool pauseInputResetOnClose = true;
    [SerializeField] [Min(0f)] private float postUnpauseSpikeFilterDuration = 0.12f;
    [SerializeField] [Min(0.1f)] private float postUnpauseSpikeThreshold = 3.5f;
    [SerializeField] private bool restorePoseOnUnpause = true;
    [SerializeField] private bool restoreCameraLocalPositionOnUnpause = true;
    [SerializeField] private bool clearAngularVelocityOnResume = true;
    [SerializeField] private Vector2 smoothing = new Vector2(3f, 3f);

    [Header("Recoil")]
    [SerializeField] [Min(0.01f)] private float recoilReturnTime = 0.08f;

    [Header("Jump Camera Lift")]
    [SerializeField] private float jumpLiftHeight = 0.04f;
    [SerializeField] [Min(0.001f)] private float jumpLiftUpDuration = 0.1f;
    [SerializeField] [Min(0.001f)] private float jumpLiftDownDuration = 0.16f;

    [Header("Jump Camera Bounce")]
    [SerializeField] private float jumpBounceDropHeight = 0.025f;
    [SerializeField] [Min(0.001f)] private float jumpBounceDropDuration = 0.04f;
    [SerializeField] [Min(0.001f)] private float jumpBounceRecoverDuration = 0.06f;

    [Header("Jump Camera Anticipation")]
    [SerializeField] private float anticipationDropHeight = 0.025f;
    [SerializeField] [Min(0.001f)] private float anticipationDropReturnDuration = 0.06f;

    public Vector2 ClampInDegrees => clampInDegrees;
    public bool LockCursor => lockCursor;
    public float PostUnpauseLookBlockDuration => postUnpauseLookBlockDuration;
    public bool PauseInputResetOnOpen => pauseInputResetOnOpen;
    public bool PauseInputResetOnClose => pauseInputResetOnClose;
    public float PostUnpauseSpikeFilterDuration => postUnpauseSpikeFilterDuration;
    public float PostUnpauseSpikeThreshold => postUnpauseSpikeThreshold;
    public bool RestorePoseOnUnpause => restorePoseOnUnpause;
    public bool RestoreCameraLocalPositionOnUnpause => restoreCameraLocalPositionOnUnpause;
    public bool ClearAngularVelocityOnResume => clearAngularVelocityOnResume;
    public Vector2 Smoothing => smoothing;
    public float RecoilReturnTime => recoilReturnTime;
    public float JumpLiftHeight => jumpLiftHeight;
    public float JumpLiftUpDuration => jumpLiftUpDuration;
    public float JumpLiftDownDuration => jumpLiftDownDuration;
    public float JumpBounceDropHeight => jumpBounceDropHeight;
    public float JumpBounceDropDuration => jumpBounceDropDuration;
    public float JumpBounceRecoverDuration => jumpBounceRecoverDuration;
    public float AnticipationDropHeight => anticipationDropHeight;
    public float AnticipationDropReturnDuration => anticipationDropReturnDuration;
}

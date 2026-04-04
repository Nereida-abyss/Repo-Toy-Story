using UnityEngine;

public class MouseLookScript : MonoBehaviour
{
    private enum JumpCameraPhase
    {
        None,
        Rise,
        Fall,
        BounceDrop,
        BounceRecover
    }

    public static MouseLookScript instance;

    [Header("Mouse Look Settings")]
    public Vector2 clampInDegrees = new Vector2(360, 180);
    public bool lockCursor = true;
    [Space]
    private Vector2 sensitivity = new Vector2(2f, 2f); 
    [Space]
    public Vector2 smoothing = new Vector2(3f, 3f);

    [Header("First Person")]
    public GameObject characterBody;

    [Header("Recoil")]
    [SerializeField] private float recoilReturnTime = 0.08f;

    [Header("Jump Camera Lift")]
    [SerializeField] private float jumpLiftHeight = 0.04f;
    [SerializeField] private float jumpLiftUpDuration = 0.1f;
    [SerializeField] private float jumpLiftDownDuration = 0.16f;

    [Header("Jump Camera Bounce")]
    [SerializeField] private float jumpBounceDropHeight = 0.025f;
    [SerializeField] private float jumpBounceDropDuration = 0.04f;
    [SerializeField] private float jumpBounceRecoverDuration = 0.06f;

    [Header("Jump Camera Anticipation")]
    [SerializeField] private float anticipationDropHeight = 0.025f;
    [SerializeField] private float anticipationDropReturnDuration = 0.06f;

    private Vector2 targetDirection;
    private Vector2 targetCharacterDirection;

    private Vector2 _mouseAbsolute;
    private Vector2 _smoothMouse;

    private Vector2 mouseDelta;
    private float jumpPreparationPitchOffset;
    private Vector3 baseLocalPosition;
    private float recoilPitchOffset;
    private float recoilPitchVelocity;
    private float recoilYawOffset;
    private float recoilYawVelocity;
    private float anticipationDropOffset;
    private float anticipationDropVelocity;
    private float anticipationDropTarget;
    private float anticipationDropSmoothTime = 0.1f;
    private float jumpCameraOffset;
    private JumpCameraPhase jumpCameraPhase;
    private float jumpCameraPhaseTimer;

    [HideInInspector]
    public bool scoped;

    void Start()
    {
        instance = this;
        baseLocalPosition = transform.localPosition;

        targetDirection = transform.localRotation.eulerAngles;

        if (characterBody)
        {
            targetCharacterDirection = characterBody.transform.localRotation.eulerAngles;
        }

        if (lockCursor)
        {
            LockCursor();
        }
    }


    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var targetOrientation = Quaternion.Euler(targetDirection);
        var targetCharacterOrientation = Quaternion.Euler(targetCharacterDirection);

        mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));

        _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
        _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

        _mouseAbsolute += _smoothMouse;

        if (clampInDegrees.x < 360)
            _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

        if (clampInDegrees.y < 360)
            _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

        recoilPitchOffset = Mathf.SmoothDamp(recoilPitchOffset, 0f, ref recoilPitchVelocity, Mathf.Max(0.01f, recoilReturnTime));
        recoilYawOffset = Mathf.SmoothDamp(recoilYawOffset, 0f, ref recoilYawVelocity, Mathf.Max(0.01f, recoilReturnTime));

        float combinedPitch = _mouseAbsolute.y + jumpPreparationPitchOffset + recoilPitchOffset;
        Quaternion pitchRotation = Quaternion.AngleAxis(-combinedPitch, targetOrientation * Vector3.right) * targetOrientation;
        Quaternion yawRecoilRotation = Quaternion.AngleAxis(recoilYawOffset, targetOrientation * Vector3.up);
        transform.localRotation = yawRecoilRotation * pitchRotation;
        UpdateCameraVerticalOffsets();

        if (characterBody)
        {
            var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, Vector3.up);
            characterBody.transform.localRotation = yRotation * targetCharacterOrientation;
        }

        else
        {
            var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, transform.InverseTransformDirection(Vector3.up));
            transform.localRotation *= yRotation;
        }

    }

    public void PlayJumpPreparationDip(float downwardAngle, float duration)
    {
        if (duration <= 0f)
        {
            jumpPreparationPitchOffset = 0f;
            return;
        }

        CancelInvoke(nameof(ResetJumpPreparationDip));
        jumpPreparationPitchOffset = downwardAngle;
        Invoke(nameof(ResetJumpPreparationDip), duration);
    }

    public void PlayJumpCameraSequence()
    {
        ReleaseJumpAnticipationDrop();
        jumpCameraPhase = JumpCameraPhase.Rise;
        jumpCameraPhaseTimer = 0f;
        jumpCameraOffset = 0f;
    }

    public void PlayJumpLift()
    {
        PlayJumpCameraSequence();
    }

    public void PlayJumpAnticipationDrop(float anticipationDuration)
    {
        if (anticipationDuration <= 0f || anticipationDropHeight <= 0f)
        {
            ReleaseJumpAnticipationDrop();
            return;
        }

        CancelInvoke(nameof(BeginAnticipationDropReturn));
        anticipationDropTarget = anticipationDropHeight;
        anticipationDropSmoothTime = Mathf.Max(0.01f, anticipationDuration);
        Invoke(nameof(BeginAnticipationDropReturn), anticipationDuration);
    }

    public void ReleaseJumpAnticipationDrop()
    {
        CancelInvoke(nameof(BeginAnticipationDropReturn));
        BeginAnticipationDropReturn();
    }

    public void ApplyRecoil(float pitchKick, float yawKick)
    {
        recoilPitchOffset += pitchKick;
        recoilYawOffset += yawKick;
    }

    private void ResetJumpPreparationDip()
    {
        jumpPreparationPitchOffset = 0f;
    }

    private void BeginAnticipationDropReturn()
    {
        anticipationDropTarget = 0f;
        anticipationDropSmoothTime = Mathf.Max(0.01f, anticipationDropReturnDuration);
    }

    private void UpdateCameraVerticalOffsets()
    {
        anticipationDropOffset = Mathf.SmoothDamp(
            anticipationDropOffset,
            anticipationDropTarget,
            ref anticipationDropVelocity,
            anticipationDropSmoothTime);

        UpdateJumpCameraOffset();

        float combinedOffset = jumpCameraOffset - anticipationDropOffset;
        Vector3 desiredPosition = baseLocalPosition + Vector3.up * combinedOffset;
        transform.localPosition = desiredPosition;
    }

    private void UpdateJumpCameraOffset()
    {
        if (jumpCameraPhase == JumpCameraPhase.None)
        {
            jumpCameraOffset = 0f;
            return;
        }

        jumpCameraPhaseTimer += Time.deltaTime;

        if (jumpCameraPhase == JumpCameraPhase.Rise)
        {
            float duration = Mathf.Max(0.001f, jumpLiftUpDuration);
            float t = Mathf.Clamp01(jumpCameraPhaseTimer / duration);
            jumpCameraOffset = Mathf.Lerp(0f, jumpLiftHeight, t);

            if (t >= 1f)
            {
                jumpCameraPhase = JumpCameraPhase.Fall;
                jumpCameraPhaseTimer = 0f;
            }

            return;
        }

        if (jumpCameraPhase == JumpCameraPhase.Fall)
        {
            float duration = Mathf.Max(0.001f, jumpLiftDownDuration);
            float t = Mathf.Clamp01(jumpCameraPhaseTimer / duration);
            jumpCameraOffset = Mathf.Lerp(jumpLiftHeight, 0f, t);

            if (t >= 1f)
            {
                if (jumpBounceDropHeight > 0f && jumpBounceDropDuration > 0f && jumpBounceRecoverDuration > 0f)
                {
                    jumpCameraPhase = JumpCameraPhase.BounceDrop;
                    jumpCameraPhaseTimer = 0f;
                }
                else
                {
                    jumpCameraPhase = JumpCameraPhase.None;
                    jumpCameraPhaseTimer = 0f;
                    jumpCameraOffset = 0f;
                }
            }

            return;
        }

        if (jumpCameraPhase == JumpCameraPhase.BounceDrop)
        {
            float duration = Mathf.Max(0.001f, jumpBounceDropDuration);
            float t = Mathf.Clamp01(jumpCameraPhaseTimer / duration);
            jumpCameraOffset = Mathf.Lerp(0f, -jumpBounceDropHeight, t);

            if (t >= 1f)
            {
                jumpCameraPhase = JumpCameraPhase.BounceRecover;
                jumpCameraPhaseTimer = 0f;
            }

            return;
        }

        float recoverDuration = Mathf.Max(0.001f, jumpBounceRecoverDuration);
        float recoverT = Mathf.Clamp01(jumpCameraPhaseTimer / recoverDuration);
        jumpCameraOffset = Mathf.Lerp(-jumpBounceDropHeight, 0f, recoverT);

        if (recoverT >= 1f)
        {
            jumpCameraPhase = JumpCameraPhase.None;
            jumpCameraPhaseTimer = 0f;
            jumpCameraOffset = 0f;
        }
    }
}

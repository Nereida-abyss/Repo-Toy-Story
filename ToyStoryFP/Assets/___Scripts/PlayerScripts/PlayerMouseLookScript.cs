using UnityEngine;

public class MouseLookScript : MonoBehaviour
{
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

    [Header("Jump Camera Lift")]
    [SerializeField] private float jumpLiftHeight = 0.04f;
    [SerializeField] private float jumpLiftUpDuration = 0.1f;
    [SerializeField] private float jumpLiftDownDuration = 0.16f;

    [Header("Jump Camera Anticipation")]
    [SerializeField] private float anticipationDropHeight = 0.025f;
    [SerializeField] private float anticipationDropReturnDuration = 0.06f;

    [Header("Landing Camera Bounce")]
    [SerializeField] private float landingBounceDropHeight = 0.015f;
    [SerializeField] private float landingBounceDropDuration = 0.05f;
    [SerializeField] private float landingBounceRecoverHeight = 0.008f;
    [SerializeField] private float landingBounceRiseDuration = 0.025f;
    [SerializeField] private float landingBounceRecoverDuration = 0.08f;

    private Vector2 targetDirection;
    private Vector2 targetCharacterDirection;

    private Vector2 _mouseAbsolute;
    private Vector2 _smoothMouse;

    private Vector2 mouseDelta;
    private float jumpPreparationPitchOffset;
    private Vector3 baseLocalPosition;
    private float jumpLiftOffset;
    private float jumpLiftVelocity;
    private float jumpLiftTarget;
    private float jumpLiftSmoothTime = 0.1f;
    private float anticipationDropOffset;
    private float anticipationDropVelocity;
    private float anticipationDropTarget;
    private float anticipationDropSmoothTime = 0.1f;
    private float landingBounceOffset;
    private float landingBounceImpactStrength = 1f;
    private bool jumpLiftReturning;
    private bool landingBounceQueued;
    private float queuedLandingBounceImpactStrength = 1f;
    private int landingBouncePhase;
    private float landingBouncePhaseTimer;

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

        float combinedPitch = _mouseAbsolute.y + jumpPreparationPitchOffset;
        transform.localRotation = Quaternion.AngleAxis(-combinedPitch, targetOrientation * Vector3.right) * targetOrientation;
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

    public void PlayJumpLift()
    {
        CancelInvoke(nameof(BeginJumpLiftReturn));
        ReleaseJumpAnticipationDrop();
        jumpLiftReturning = false;

        jumpLiftTarget = jumpLiftHeight;
        jumpLiftSmoothTime = Mathf.Max(0.01f, jumpLiftUpDuration);
        Invoke(nameof(BeginJumpLiftReturn), jumpLiftUpDuration);
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

    public void PlayLandingBounce(float impactStrength = 1f)
    {
        if (landingBounceDropHeight <= 0f)
        {
            return;
        }

        float clampedImpactStrength = Mathf.Max(0f, impactStrength);

        if (jumpLiftReturning && jumpLiftOffset > 0.001f)
        {
            landingBounceQueued = true;
            queuedLandingBounceImpactStrength = clampedImpactStrength;
            return;
        }

        StartLandingBounce(clampedImpactStrength);
    }

    private void StartLandingBounce(float impactStrength)
    {
        landingBounceImpactStrength = impactStrength;
        landingBouncePhase = 1;
        landingBouncePhaseTimer = 0f;
        landingBounceOffset = 0f;
    }

    private void ResetJumpPreparationDip()
    {
        jumpPreparationPitchOffset = 0f;
    }

    private void BeginJumpLiftReturn()
    {
        jumpLiftReturning = true;
        jumpLiftTarget = 0f;
        jumpLiftSmoothTime = Mathf.Max(0.01f, jumpLiftDownDuration);
    }

    private void BeginAnticipationDropReturn()
    {
        anticipationDropTarget = 0f;
        anticipationDropSmoothTime = Mathf.Max(0.01f, anticipationDropReturnDuration);
    }

    private void UpdateCameraVerticalOffsets()
    {
        jumpLiftOffset = Mathf.SmoothDamp(
            jumpLiftOffset,
            jumpLiftTarget,
            ref jumpLiftVelocity,
            jumpLiftSmoothTime);

        anticipationDropOffset = Mathf.SmoothDamp(
            anticipationDropOffset,
            anticipationDropTarget,
            ref anticipationDropVelocity,
            anticipationDropSmoothTime);

        UpdateLandingBounceOffset();

        if (jumpLiftReturning && jumpLiftTarget == 0f && jumpLiftOffset <= 0.001f)
        {
            jumpLiftReturning = false;
            jumpLiftVelocity = 0f;
            jumpLiftOffset = 0f;

            if (landingBounceQueued)
            {
                landingBounceQueued = false;
                StartLandingBounce(queuedLandingBounceImpactStrength);
            }
        }

        float combinedOffset = jumpLiftOffset - anticipationDropOffset + landingBounceOffset;
        Vector3 desiredPosition = baseLocalPosition + Vector3.up * combinedOffset;
        transform.localPosition = desiredPosition;
    }

    private void UpdateLandingBounceOffset()
    {
        if (landingBouncePhase == 0)
        {
            landingBounceOffset = 0f;
            return;
        }

        landingBouncePhaseTimer += Time.deltaTime;

        float fullDrop = landingBounceDropHeight * landingBounceImpactStrength;
        float partialDrop = Mathf.Max(0f, landingBounceDropHeight - landingBounceRecoverHeight) * landingBounceImpactStrength;

        if (landingBouncePhase == 1)
        {
            float duration = Mathf.Max(0.001f, landingBounceDropDuration);
            float t = Mathf.Clamp01(landingBouncePhaseTimer / duration);
            landingBounceOffset = Mathf.Lerp(0f, -fullDrop, t);

            if (t >= 1f)
            {
                landingBouncePhase = 2;
                landingBouncePhaseTimer = 0f;
            }

            return;
        }

        if (landingBouncePhase == 2)
        {
            float duration = Mathf.Max(0.001f, landingBounceRiseDuration);
            float t = Mathf.Clamp01(landingBouncePhaseTimer / duration);
            landingBounceOffset = Mathf.Lerp(-fullDrop, -partialDrop, t);

            if (t >= 1f)
            {
                landingBouncePhase = 3;
                landingBouncePhaseTimer = 0f;
            }

            return;
        }

        float settleDuration = Mathf.Max(0.001f, landingBounceRecoverDuration);
        float settleT = Mathf.Clamp01(landingBouncePhaseTimer / settleDuration);
        landingBounceOffset = Mathf.Lerp(-partialDrop, 0f, settleT);

        if (settleT >= 1f)
        {
            landingBouncePhase = 0;
            landingBouncePhaseTimer = 0f;
            landingBounceOffset = 0f;
        }
    }
    
}

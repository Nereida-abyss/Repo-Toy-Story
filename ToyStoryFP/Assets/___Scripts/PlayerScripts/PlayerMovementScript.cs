using UnityEngine;

public class MovementScript : MonoBehaviour
{
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int JumpStateHash = Animator.StringToHash("Base Layer.Pistol Jump 0");
    private static readonly int LocomotionStateHash = Animator.StringToHash("Base Layer.MovementTree");

    [Header("Movement Settings")]
    public float WalkSpeed = 5f;
    public float maxVelocityChange = 10f;
    [SerializeField] private float turnSpeed = 360f;

    [Header("Jump Settings")]
    public float JumpForce = 5f;
    [SerializeField] private float jumpDelay = 0.12f;
    [SerializeField] private float jumpInputBuffer = 0.15f;
    [SerializeField] private float jumpGroundLockTime = 0.1f;
    [SerializeField] private float jumpPreparationCameraDip = 6f;

    [Header("Visual Model")]
    [SerializeField] private Transform visualModelRoot;
    [SerializeField] private Animator modelAnimator;
    [SerializeField] private float jumpStateTransitionDuration = 0.05f;
    [SerializeField] private float locomotionTransitionDuration = 0.1f;

    private Vector2 moveInput;
    private Vector2 animationInput;
    private Rigidbody rb;
    private MouseLookScript mouseLook;
    private PlayerAudioController playerAudio;
    private Vector3 externalPlanarVelocity;
    private bool isGrounded;
    private bool ignoreGroundingWhileAscending;
    private bool jumpQueued;
    private bool jumpAnimationTriggeredThisFrame;
    private bool hasJumpParameter;
    private bool hasGroundedParameter;
    private bool hasVerticalSpeedParameter;
    private bool hasExternalAnimationInput;
    private bool externalTranslationDriven;
    private float jumpDelayTimer;
    private float jumpBufferTimer;
    private float groundedLockTimer;

    public bool IsGrounded => isGrounded;
    public float VerticalSpeed => rb != null ? rb.linearVelocity.y : 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mouseLook = GetComponentInChildren<MouseLookScript>(true);
        playerAudio = GetComponent<PlayerAudioController>();
        ResolveVisualReferences();
    }

    void Update()
    {
        jumpAnimationTriggeredThisFrame = false;
        bool groundedAtFrameStart = isGrounded;

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (groundedLockTimer > 0f)
        {
            groundedLockTimer -= Time.deltaTime;
        }

        if (jumpQueued)
        {
            jumpDelayTimer -= Time.deltaTime;

            if (jumpDelayTimer <= 0f)
            {
                ExecuteQueuedJump();
            }
        }
        else if (jumpBufferTimer > 0f && isGrounded)
        {
            QueueJump();
        }

        UpdateMovementAnimation(groundedAtFrameStart);
        UpdateFootstepAudio();
    }

    void FixedUpdate()
    {
        if (rb == null || rb.isKinematic || externalTranslationDriven)
        {
            return;
        }

        rb.AddForce(CalculateMovement(), ForceMode.VelocityChange);
    }

    void OnValidate()
    {
        if (visualModelRoot == null)
        {
            visualModelRoot = transform.Find("PlayerModelo");
        }
    }

    public void SetMoveInput(Vector2 newInput)
    {
        moveInput = Vector2.ClampMagnitude(newInput, 1f);

        if (!hasExternalAnimationInput)
        {
            animationInput = moveInput;
        }
    }

    public void SetAnimationInput(Vector2 newInput)
    {
        animationInput = Vector2.ClampMagnitude(newInput, 1f);
        hasExternalAnimationInput = true;
    }

    public void ClearAnimationInputOverride()
    {
        hasExternalAnimationInput = false;
        animationInput = moveInput;
    }

    public void SetExternalMovementState(Vector3 worldPlanarVelocity, bool grounded)
    {
        Vector3 localVelocity = PrepareExternalMovementState(worldPlanarVelocity, grounded);
        Vector2 normalizedVelocity = WalkSpeed > 0.01f
            ? new Vector2(localVelocity.x, localVelocity.z) / WalkSpeed
            : Vector2.zero;

        SetAnimationInput(normalizedVelocity);
    }

    public void SetExternalMovementAnimation(
        Vector3 worldPlanarVelocity,
        bool grounded,
        float animationSpeedReference,
        float minimumMoveBlend,
        float animationMoveThreshold)
    {
        Vector3 localVelocity = PrepareExternalMovementState(worldPlanarVelocity, grounded);
        Vector2 planarVelocity = new Vector2(localVelocity.x, localVelocity.z);
        float planarSpeed = planarVelocity.magnitude;

        if (planarSpeed <= Mathf.Max(0f, animationMoveThreshold))
        {
            SetAnimationInput(Vector2.zero);
            return;
        }

        Vector2 direction = planarVelocity / planarSpeed;
        float normalizedBlend = Mathf.Clamp01(planarSpeed / Mathf.Max(0.01f, animationSpeedReference));
        normalizedBlend = Mathf.Max(normalizedBlend, Mathf.Clamp01(minimumMoveBlend));
        SetAnimationInput(direction * normalizedBlend);
    }

    public void ClearExternalMovementState()
    {
        externalTranslationDriven = false;
        externalPlanarVelocity = Vector3.zero;
        ClearAnimationInputOverride();
    }

    public void RequestJump()
    {
        jumpBufferTimer = jumpInputBuffer;
    }

    public void FaceDirection(Vector3 worldDirection, float overrideTurnSpeed = -1f)
    {
        Vector3 flattenedDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);

        if (flattenedDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float appliedTurnSpeed = overrideTurnSpeed > 0f ? overrideTurnSpeed : turnSpeed;
        Quaternion targetRotation = Quaternion.LookRotation(flattenedDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, appliedTurnSpeed * Time.deltaTime);
    }

    Vector3 CalculateMovement()
    {
        Vector3 targetVelocity = transform.TransformDirection(new Vector3(moveInput.x, 0f, moveInput.y)) * WalkSpeed;
        Vector3 velocityChange = targetVelocity - rb.linearVelocity;


        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0f;

        return moveInput.magnitude > 0 ? velocityChange : Vector3.zero;
    }

    void OnCollisionStay(Collision collision)
    {
        if (externalTranslationDriven)
        {
            return;
        }

        if (groundedLockTimer > 0f)
        {
            return;
        }

        if (jumpQueued)
        {
            return;
        }

        if (ignoreGroundingWhileAscending && VerticalSpeed > 0.05f)
        {
            return;
        }

        ignoreGroundingWhileAscending = false;
        isGrounded = true;
    }

    private Vector3 PrepareExternalMovementState(Vector3 worldPlanarVelocity, bool grounded)
    {
        externalTranslationDriven = true;
        externalPlanarVelocity = Vector3.ProjectOnPlane(worldPlanarVelocity, Vector3.up);
        isGrounded = grounded;
        return transform.InverseTransformDirection(externalPlanarVelocity);
    }

    private void ResolveVisualReferences()
    {
        if (visualModelRoot == null)
        {
            visualModelRoot = transform.Find("PlayerModelo");
        }

        if (modelAnimator == null && visualModelRoot != null)
        {
            modelAnimator = visualModelRoot.GetComponentInChildren<Animator>(true);
        }

        CacheAnimatorParameters();
    }

    private void UpdateMovementAnimation(bool groundedAtFrameStart)
    {
        if (modelAnimator == null)
        {
            ResolveVisualReferences();
        }

        if (modelAnimator == null)
        {
            return;
        }

        modelAnimator.SetFloat(HorizontalHash, animationInput.x);
        modelAnimator.SetFloat(VerticalHash, animationInput.y);

        if (hasGroundedParameter)
        {
            modelAnimator.SetBool(IsGroundedHash, isGrounded);
        }

        if (hasVerticalSpeedParameter)
        {
            modelAnimator.SetFloat(VerticalSpeedHash, VerticalSpeed);
        }

        bool startedAirborneThisFrame = groundedAtFrameStart && !isGrounded;
        bool landedThisFrame = !groundedAtFrameStart && isGrounded;

        if (startedAirborneThisFrame && !jumpAnimationTriggeredThisFrame)
        {
            CrossFadeIfNeeded(JumpStateHash, jumpStateTransitionDuration);
        }
        else if (landedThisFrame)
        {
            CrossFadeIfNeeded(LocomotionStateHash, locomotionTransitionDuration);
        }
    }

    private void TriggerJumpAnimation()
    {
        if (modelAnimator == null)
        {
            ResolveVisualReferences();
        }

        if (modelAnimator == null)
        {
            return;
        }

        if (hasJumpParameter)
        {
            modelAnimator.ResetTrigger(JumpHash);
            modelAnimator.SetTrigger(JumpHash);
            jumpAnimationTriggeredThisFrame = true;
        }
        else
        {
            CrossFadeIfNeeded(JumpStateHash, jumpStateTransitionDuration);
        }
    }

    private void QueueJump()
    {
        if (jumpQueued)
        {
            return;
        }

        jumpQueued = true;
        jumpDelayTimer = Mathf.Max(0f, jumpDelay);
        jumpBufferTimer = 0f;
        TriggerJumpAnimation();
        PlayJumpAnticipationDrop();

        if (jumpDelayTimer <= 0f)
        {
            ExecuteQueuedJump();
        }
    }

    private void ExecuteQueuedJump()
    {
        if (!jumpQueued)
        {
            return;
        }

        jumpQueued = false;
        jumpDelayTimer = 0f;
        isGrounded = false;
        groundedLockTimer = jumpGroundLockTime;
        ignoreGroundingWhileAscending = true;
        PlayJumpCameraLift();
        PlayJumpAudio();
        rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
    }

    private void CacheAnimatorParameters()
    {
        if (modelAnimator == null)
        {
            hasJumpParameter = false;
            hasGroundedParameter = false;
            hasVerticalSpeedParameter = false;
            return;
        }

        hasJumpParameter = false;
        hasGroundedParameter = false;
        hasVerticalSpeedParameter = false;

        foreach (AnimatorControllerParameter parameter in modelAnimator.parameters)
        {
            if (parameter.nameHash == JumpHash)
            {
                hasJumpParameter = true;
            }
            else if (parameter.nameHash == IsGroundedHash)
            {
                hasGroundedParameter = true;
            }
            else if (parameter.nameHash == VerticalSpeedHash)
            {
                hasVerticalSpeedParameter = true;
            }
        }
    }

    private void CrossFadeIfNeeded(int stateHash, float transitionDuration)
    {
        AnimatorStateInfo currentState = modelAnimator.GetCurrentAnimatorStateInfo(0);

        if (currentState.fullPathHash == stateHash)
        {
            return;
        }

        modelAnimator.CrossFadeInFixedTime(stateHash, transitionDuration);
    }

    private void PlayJumpPreparationDip()
    {
        if (mouseLook == null)
        {
            mouseLook = GetComponentInChildren<MouseLookScript>(true);
        }

        if (mouseLook == null || jumpPreparationCameraDip <= 0f)
        {
            return;
        }

        mouseLook.PlayJumpPreparationDip(jumpPreparationCameraDip, jumpDelay);
    }

    private void PlayJumpCameraLift()
    {
        if (mouseLook == null)
        {
            mouseLook = GetComponentInChildren<MouseLookScript>(true);
        }

        if (mouseLook == null)
        {
            return;
        }

        mouseLook.PlayJumpCameraSequence();
    }

    private void PlayJumpAnticipationDrop()
    {
        if (mouseLook == null)
        {
            mouseLook = GetComponentInChildren<MouseLookScript>(true);
        }

        if (mouseLook == null)
        {
            return;
        }

        mouseLook.PlayJumpAnticipationDrop(jumpDelay);
    }

    private void PlayJumpAudio()
    {
        if (playerAudio == null)
        {
            playerAudio = GetComponent<PlayerAudioController>();
        }

        if (playerAudio == null)
        {
            return;
        }

        playerAudio.PlayJump();
    }

    private void UpdateFootstepAudio()
    {
        if (playerAudio == null)
        {
            playerAudio = GetComponent<PlayerAudioController>();
        }

        if (playerAudio == null || rb == null)
        {
            return;
        }

        Vector3 planarVelocity = externalTranslationDriven
            ? externalPlanarVelocity
            : Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
        float speedNormalized = WalkSpeed > 0.01f
            ? Mathf.Clamp01(planarVelocity.magnitude / WalkSpeed)
            : 0f;

        playerAudio.UpdateFootsteps(isGrounded && !jumpQueued, animationInput.magnitude, speedNormalized);
    }

    void OnCollisionExit(Collision collision)
    {
        if (externalTranslationDriven)
        {
            return;
        }

        isGrounded = false;

        if (groundedLockTimer <= 0f && VerticalSpeed <= 0.05f)
        {
            ignoreGroundingWhileAscending = false;
        }
    }
}

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

    private Vector2 input;
    private Rigidbody rb;
    private MouseLookScript mouseLook;
    private bool isGrounded;
    private bool ignoreGroundingWhileAscending;
    private bool jumpQueued;
    private bool jumpAnimationTriggeredThisFrame;
    private bool hasJumpParameter;
    private bool hasGroundedParameter;
    private bool hasVerticalSpeedParameter;
    private float jumpDelayTimer;
    private float jumpBufferTimer;
    private float groundedLockTimer;

    public bool IsGrounded => isGrounded;
    public float VerticalSpeed => rb != null ? rb.linearVelocity.y : 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mouseLook = GetComponentInChildren<MouseLookScript>(true);
        ResolveVisualReferences();
    }

    void Update()
    {
        jumpAnimationTriggeredThisFrame = false;
        bool groundedAtFrameStart = isGrounded;
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpInputBuffer;
        }

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
    }

    void FixedUpdate()
    {
        rb.AddForce(CalculateMovement(), ForceMode.VelocityChange);
    }

    void OnValidate()
    {
        if (visualModelRoot == null)
        {
            visualModelRoot = transform.Find("PlayerModelo");
        }
    }

    Vector3 CalculateMovement()
    {
        Vector3 targetVelocity = transform.TransformDirection(new Vector3(input.x, 0f, input.y)) * WalkSpeed;
        Vector3 velocityChange = targetVelocity - rb.linearVelocity;


        velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
        velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
        velocityChange.y = 0f;

        return input.magnitude > 0 ? velocityChange : Vector3.zero;
    }

    void OnCollisionStay(Collision collision)
    {
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

        modelAnimator.SetFloat(HorizontalHash, input.x);
        modelAnimator.SetFloat(VerticalHash, input.y);

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
        jumpDelayTimer = jumpDelay;
        jumpBufferTimer = 0f;
        TriggerJumpAnimation();
        PlayJumpPreparationDip();
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

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;

        if (groundedLockTimer <= 0f && VerticalSpeed <= 0.05f)
        {
            ignoreGroundingWhileAscending = false;
        }
    }
}

using UnityEngine;

public class MovementScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float WalkSpeed = 5f;
    public float maxVelocityChange = 10f;

    [Header("Jump Settings")]
    public float JumpForce = 5f;

    [Header("Visual Model")]
    [SerializeField] private Transform visualModelRoot;
    [SerializeField] private Animator modelAnimator;

    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private Vector2 input;
    private Rigidbody rb;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ResolveVisualReferences();
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        UpdateMovementAnimation();

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
            TriggerJumpAnimation();
        }
    }

    void FixedUpdate()
    {
        rb.AddForce(CalculateMovement(), ForceMode.VelocityChange);

        isGrounded = false;
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
    }

    private void UpdateMovementAnimation()
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

        modelAnimator.SetTrigger(JumpHash);
    }
}

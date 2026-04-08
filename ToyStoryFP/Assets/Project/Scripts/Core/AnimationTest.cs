using UnityEngine;

public class AnimationTest : MonoBehaviour
{
    [Range(-2, 2)]
    [SerializeField] private float horizontal;

    [Range(-2, 2)]
    [SerializeField] private float vertical;

    private Animator animator;

    [SerializeField] private bool jump;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        animator.SetFloat("Horizontal", horizontal);
        animator.SetFloat("Vertical", vertical);

        if (jump)
        {
            jump = false;
            animator.SetTrigger("Jump");
        }
    }
}

using UnityEngine;

public class AnimationTest : MonoBehaviour
{
    
    [Range(-2,2)]
    public float horizontal;
    [Range(-2,2)]
    public float vertical;

    private Animator animator; 

    public bool Jump;



    void Start()
    {
        animator = GetComponent<Animator>();
    }

    
    void Update()
    {
        animator.SetFloat("Horizontal", horizontal);
        animator.SetFloat("Vertical", vertical);

        if (Jump)
        {
            Jump = false;
            animator.SetTrigger("Jump");
        }
    }
}

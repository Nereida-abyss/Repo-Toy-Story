using System;
using NUnit.Framework;
using UnityEngine;

public class PlayerMovementScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float WalkSpeed = 5f;
    public float maxVelocityChange = 10f;

    [Header("Jump Settings")]
    public float JumpForce = 5f;

    private Vector2 input;
    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        rb.AddForce(CalculateMovement(), ForceMode.VelocityChange);

        isGrounded = false;
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
}

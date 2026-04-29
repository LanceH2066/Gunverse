using Fusion;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public float walkSpeed = 4f;
    public float sprintSpeed = 14f;
    public float maxVelocityChange = 10f;
    [Space]
    public float airControl = 0.5f;
    [Space]
    public float jumpHeight = 5f;

    private Rigidbody rb;
    private bool grounded;
    private Vector2 moveInput;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log($"[PlayerMovement] Spawned - HasInputAuthority: {HasInputAuthority} | HasStateAuthority: {HasStateAuthority}");

        if (!HasStateAuthority)
        {
            rb.isKinematic = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        grounded = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        moveInput = input.Direction;
        bool sprinting = input.Sprinting;
        bool jumping = input.Jumping;

        if (grounded)
        {
            if (jumping)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpHeight, rb.linearVelocity.z);
            }
            else if (moveInput.magnitude > 0.5f)
            {
                rb.AddForce(CalculateMovement(sprinting ? sprintSpeed : walkSpeed), ForceMode.VelocityChange);
            }
            else
            {
                var v = rb.linearVelocity;
                rb.linearVelocity = new Vector3(v.x * 0.2f * Runner.DeltaTime, v.y, v.z * 0.2f * Runner.DeltaTime);
            }
        }
        else
        {
            if (moveInput.magnitude > 0.5f)
            {
                rb.AddForce(CalculateMovement(sprinting ? sprintSpeed * airControl : walkSpeed * airControl), ForceMode.VelocityChange);
            }
            else
            {
                var v = rb.linearVelocity;
                rb.linearVelocity = new Vector3(v.x * 0.2f * Runner.DeltaTime, v.y, v.z * 0.2f * Runner.DeltaTime);
            }
        }

        grounded = false;
    }

    Vector3 CalculateMovement(float _speed)
    {
        Vector3 targetVelocity = new Vector3(moveInput.x, 0, moveInput.y);
        targetVelocity = transform.TransformDirection(targetVelocity);
        targetVelocity *= _speed;
        Vector3 velocity = rb.linearVelocity;
        if (moveInput.magnitude > 0.5f)
        {
            Vector3 velocityChange = targetVelocity - velocity;
            velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
            velocityChange.y = 0;
            return velocityChange;
        }
        else
        {
            return new Vector3();
        }
    }
}
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float walkSpeed      = 4f;
    public float sprintSpeed    = 14f;
    public float maxVelChange   = 10f;
    public float airControl     = 0.5f;
    public float jumpHeight     = 5f;
    [Header("Look")]
    public float sensitivityX   = 0.2f;

    [Networked] public float NetworkedYaw   { get; set; }
    [Networked] public float NetworkedPitch { get; set; }

    public float LocalYaw    { get; private set; }
    public bool  JumpPending { get; set; }

    public static PlayerMovement Local { get; private set; }

    private Rigidbody _rb;
    private bool      _grounded;
    private FPSCamera _fpsCamera;

    public override void Spawned()
    {
        _rb        = GetComponent<Rigidbody>();
        _fpsCamera = GetComponent<FPSCamera>();

        if (HasInputAuthority)
            Local = this;

        Runner.SetIsSimulated(Object, true);
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        LocalYaw += mouse.delta.ReadValue().x * sensitivityX;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            JumpPending = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            NetworkedYaw   = input.Yaw;
            NetworkedPitch = input.Pitch;

            bool  sprinting = input.Buttons.IsSet(InputButtons.Sprint);
            float speed     = sprinting ? sprintSpeed : walkSpeed;

            if (_grounded)
            {
                if (input.Buttons.IsSet(InputButtons.Jump))
                {
                    _rb.linearVelocity = new Vector3(
                        _rb.linearVelocity.x, jumpHeight, _rb.linearVelocity.z);
                }
                else if (input.Direction.magnitude > 0.1f)
                {
                    _rb.AddForce(CalculateMovement(input.Direction, speed, input.Yaw),
                        ForceMode.VelocityChange);
                }
                else
                {
                    var v = _rb.linearVelocity;
                    _rb.linearVelocity = new Vector3(v.x * 0.85f, v.y, v.z * 0.85f);
                }
            }
            else
            {
                if (input.Direction.magnitude > 0.1f)
                    _rb.AddForce(
                        CalculateMovement(input.Direction, speed * airControl, input.Yaw),
                        ForceMode.VelocityChange);
            }

            _grounded = false;
        }
    }

    public override void Render()
    {
        if (HasInputAuthority)
        {
            transform.rotation = Quaternion.Euler(0f, LocalYaw, 0f);
        }
        else if (IsProxy)
        {
            transform.rotation = Quaternion.Euler(0f, NetworkedYaw, 0f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (HasInputAuthority || HasStateAuthority)
            _grounded = true;
    }

    private Vector3 CalculateMovement(Vector2 dir, float speed, float yaw)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 target = rotation * new Vector3(dir.x, 0f, dir.y);
        target *= speed;
        Vector3 change = target - _rb.linearVelocity;
        change.x = Mathf.Clamp(change.x, -maxVelChange, maxVelChange);
        change.z = Mathf.Clamp(change.z, -maxVelChange, maxVelChange);
        change.y = 0f;
        return change;
    }
}
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    public float walkSpeed    = 4f;
    public float sprintSpeed  = 14f;
    public float maxVelChange = 10f;
    public float airControl   = 0.5f;
    public float jumpHeight   = 5f;

    [Header("Look")]
    public float sensitivityX = 0.2f;

    [Networked] public float        NetworkedYaw    { get; set; }
    [Networked] public float        NetworkedPitch  { get; set; }
    private bool _fireWasPressed;

    public float LocalYaw    { get; private set; }
    public bool  JumpPending { get; set; }

    public static PlayerMovement Local { get; private set; }

    private Rigidbody _rb;
    private bool      _grounded;
    private Weapons   _weapons;

    public override void Spawned()
    {
        _rb      = GetComponent<Rigidbody>();
        _weapons = GetComponentInChildren<Weapons>();

        if (HasInputAuthority)
            Local = this;

        Runner.SetIsSimulated(Object, true);
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        var mouse = Mouse.current;
        if (mouse != null)
            LocalYaw += mouse.delta.ReadValue().x * sensitivityX;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            JumpPending = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        NetworkedYaw   = input.Yaw;
        NetworkedPitch = input.Pitch;

        transform.rotation = Quaternion.Euler(0f, input.Yaw, 0f);

        if (_weapons != null)
        {
            Quaternion aimRot = Quaternion.Euler(0f, input.Yaw, 0f)
                              * Quaternion.Euler(input.Pitch, 0f, 0f);

            if (HasStateAuthority)
                _weapons.WeaponRotation = aimRot;

            bool fireNow     = input.Buttons.IsSet(InputButtons.Fire);
            bool justPressed = fireNow && !_fireWasPressed;
            _fireWasPressed  = fireNow;

            if (fireNow)
            {
                var weapon = _weapons.CurrentWeapon;
                if (weapon != null && weapon.MuzzleTransform != null)
                {
                    weapon.MuzzleTransform.rotation = aimRot;
                    _weapons.Fire(justPressed);
                }
            }

            if (input.Buttons.IsSet(InputButtons.Reload))
                _weapons.Reload();
        }

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
                _rb.AddForce(
                    CalculateMovement(input.Direction, speed, input.Yaw),
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

    public override void Render()
    {
        if (HasInputAuthority)
            transform.rotation = Quaternion.Euler(0f, LocalYaw, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, NetworkedYaw, 0f);
    }

    private void OnTriggerStay(Collider other)
    {
        if (HasInputAuthority || HasStateAuthority)
            _grounded = true;
    }

    private Vector3 CalculateMovement(Vector2 dir, float speed, float yaw)
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3    target   = rotation * new Vector3(dir.x, 0f, dir.y) * speed;
        Vector3    change   = target - _rb.linearVelocity;

        change.x = Mathf.Clamp(change.x, -maxVelChange, maxVelChange);
        change.z = Mathf.Clamp(change.z, -maxVelChange, maxVelChange);
        change.y = 0f;

        return change;
    }
}
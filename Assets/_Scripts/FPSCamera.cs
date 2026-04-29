using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCamera : NetworkBehaviour
{
    [Header("Sensitivity")]
    public float sensitivityY = 0.2f;

    [Header("Clamp")]
    public Vector2 verticalClamp = new Vector2(-80f, 80f);

    [Networked] private float NetworkedCameraX { get; set; }

    // Public so BasicSpawner.OnInput can read pitch to pack into input struct
    public float LocalCameraX { get; private set; }

    // Static reference to local camera — set in Spawned() alongside PlayerMovement.Local
    public static FPSCamera Local { get; private set; }

    private Transform _cameraTarget;

    public override void Spawned()
    {
        _cameraTarget = transform.Find("CameraTarget");

        if (HasInputAuthority)
        {
            Local = this;

            var vcam = FindAnyObjectByType<CinemachineCamera>();
            if (vcam != null && _cameraTarget != null)
            {
                vcam.Follow = _cameraTarget;
                vcam.LookAt = _cameraTarget;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        // Accumulate pitch locally, apply instantly to camera target
        LocalCameraX -= mouse.delta.ReadValue().y * sensitivityY;
        LocalCameraX = Mathf.Clamp(LocalCameraX, verticalClamp.x, verticalClamp.y);

        if (_cameraTarget != null)
            _cameraTarget.localRotation = Quaternion.Euler(LocalCameraX, 0f, 0f);
    }

    public override void FixedUpdateNetwork()
    {
        // Host stores received pitch so proxies can replicate it
        if (HasStateAuthority && GetInput(out NetworkInputData input))
        {
            NetworkedCameraX = input.Pitch;
        }

        if (IsProxy)
        {
            if (_cameraTarget == null)
                _cameraTarget = transform.Find("CameraTarget");

            if (_cameraTarget != null)
            {
                _cameraTarget.localRotation = Quaternion.Lerp(
                    _cameraTarget.localRotation,
                    Quaternion.Euler(NetworkedCameraX, 0f, 0f),
                    Runner.DeltaTime * 15f
                );
            }
        }
    }
}
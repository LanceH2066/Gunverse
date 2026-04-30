using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Direction;
    public float   Yaw;
    public float   Pitch;
    public NetworkButtons Buttons;
}

public static class InputButtons
{
    public const int Jump   = 0;
    public const int Sprint = 1;
    public const int Fire   = 2; // ← added
    public const int Reload = 3; // ← added (for later)
}
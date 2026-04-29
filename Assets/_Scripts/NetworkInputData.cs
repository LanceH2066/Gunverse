using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Direction;
    public float Yaw;
    public float Pitch;
    public NetworkButtons Buttons;
}

public static class InputButtons
{
    public const int Jump   = 0;
    public const int Sprint = 1;
}
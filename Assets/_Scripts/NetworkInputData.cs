using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 Direction;
    public NetworkBool Sprinting;
    public NetworkBool Jumping;
}
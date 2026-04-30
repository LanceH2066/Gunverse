using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class BulletProjectile : NetworkBehaviour
{
    [Networked] private TickTimer _lifetime { get; set; }
    private Rigidbody    _rb;
    private float        _damage;
    private bool         _hasHit;
    private Vector3      _pendingVelocity;
    private float        _pendingLifetime;
    private const string PlayerTag = "Player";

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Init(Vector3 velocity, float scale, float damage, float lifetime)
    {
        transform.localScale = Vector3.one * scale;
        _pendingVelocity     = velocity;
        _pendingLifetime     = lifetime;
        _damage              = damage;

        if (TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = velocity;
    }

    public override void Spawned()
    {
        _hasHit   = false;
        _lifetime = TickTimer.CreateFromSeconds(Runner, _pendingLifetime);

        Runner.SetIsSimulated(Object, true);

        if (HasStateAuthority)
            _rb.linearVelocity = _pendingVelocity;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && _lifetime.Expired(Runner))
            Runner.Despawn(Object);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!HasStateAuthority)                             return;
        if (_hasHit)                                        return;
        if (collision.gameObject.layer == gameObject.layer) return;

        _hasHit = true;

        if (collision.gameObject.CompareTag(PlayerTag))
            Debug.Log($"[Bullet] Hit {collision.gameObject.name} for {_damage} dmg");

        Runner.Despawn(Object);
    }
}
using Fusion;
using UnityEngine;

public enum EWeaponType
{
    None,
    Pistol,
    Rifle,
    Shotgun,
}

public class Weapon : NetworkBehaviour
{
    [Header("Weapon Info")]
    public EWeaponType Type;
    public string      WeaponName;

    [Header("Fire Setup")]
    public bool  IsAutomatic = true;
    public float Damage      = 25f;
    public int   FireRate    = 600;
    public float BulletSpeed = 40f;

    [Header("Ammo")]
    public int   MaxClipAmmo  = 30;
    public int   StartAmmo    = 90;
    public float ReloadTime   = 2f;

    [Header("Bullet")]
    public NetworkObject BulletPrefab;
    public Transform     MuzzleTransform;
    [Range(0.01f, 1f)]
    public float         BulletScale    = 0.15f;
    public float         BulletLifetime = 4f;

    [Header("Visuals")]
    public Renderer[] WeaponRenderers;
    public GameObject MuzzleEffectPrefab;

    [Networked, HideInInspector] public NetworkBool IsCollected   { get; set; }
    [Networked, HideInInspector] public NetworkBool IsReloading   { get; set; }
    [Networked, HideInInspector] public int         ClipAmmo      { get; set; }
    [Networked, HideInInspector] public int         RemainingAmmo { get; set; }

    [Networked] private TickTimer _fireCooldown { get; set; }

    private int        _fireTicks;
    private GameObject _muzzleEffectInstance;
    private bool       _isAutomatic;

    public bool HasAmmo => ClipAmmo > 0 || RemainingAmmo > 0;

    public bool Fire(bool justPressed)
    {
        if (!IsCollected)                               return false;
        if (!_isAutomatic && !justPressed)              return false;
        if (IsReloading)                                return false;
        if (!_fireCooldown.ExpiredOrNotRunning(Runner)) return false;
        if (ClipAmmo <= 0)                              return false;

        SpawnBullet(MuzzleTransform.position, MuzzleTransform.forward);
        _fireCooldown = TickTimer.CreateFromTicks(Runner, _fireTicks);
        ClipAmmo--;

        return true;
    }

    public void Reload()
    {
        if (!IsCollected)                               return;
        if (ClipAmmo >= MaxClipAmmo)                    return;
        if (RemainingAmmo <= 0)                         return;
        if (IsReloading)                                return;
        if (!_fireCooldown.ExpiredOrNotRunning(Runner)) return;

        IsReloading   = true;
        _fireCooldown = TickTimer.CreateFromSeconds(Runner, ReloadTime);
    }

    public void AddAmmo(int amount)
    {
        RemainingAmmo += amount;
    }

    public void ToggleVisibility(bool visible)
    {
        for (int i = 0; i < WeaponRenderers.Length; i++)
            WeaponRenderers[i].enabled = visible;

        if (_muzzleEffectInstance != null && !visible)
            _muzzleEffectInstance.SetActive(false);
    }

    public float GetReloadProgress()
    {
        if (!IsReloading) return 1f;
        float remaining = _fireCooldown.RemainingTime(Runner) ?? 0f;
        return 1f - remaining / ReloadTime;
    }

    private void Awake()
    {
        if (WeaponRenderers == null || WeaponRenderers.Length == 0)
            WeaponRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    public override void Spawned()
    {
        _isAutomatic = IsAutomatic;

        if (HasStateAuthority)
        {
            ClipAmmo      = Mathf.Clamp(StartAmmo, 0, MaxClipAmmo);
            RemainingAmmo = StartAmmo - ClipAmmo;
        }

        float fireInterval = 60f / FireRate;
        _fireTicks = Mathf.Max(1, Mathf.CeilToInt(fireInterval / Runner.DeltaTime));

        if (MuzzleEffectPrefab != null)
        {
            _muzzleEffectInstance = Instantiate(MuzzleEffectPrefab, MuzzleTransform);
            _muzzleEffectInstance.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!IsCollected) return;

        if (ClipAmmo == 0 && RemainingAmmo > 0)
            Reload();

        if (IsReloading && _fireCooldown.ExpiredOrNotRunning(Runner))
        {
            IsReloading = false;

            int reloadAmount = Mathf.Min(MaxClipAmmo - ClipAmmo, RemainingAmmo);
            ClipAmmo      += reloadAmount;
            RemainingAmmo -= reloadAmount;

            _fireCooldown = TickTimer.CreateFromSeconds(Runner, 0.25f);
        }
    }

    private void SpawnBullet(Vector3 position, Vector3 direction)
    {
        Vector3 spawnPosition = position - direction * (BulletSpeed * Runner.DeltaTime);

        Runner.Spawn(
            BulletPrefab,
            spawnPosition,
            Quaternion.LookRotation(direction),
            Object.InputAuthority,
            (runner, obj) =>
            {
                obj.gameObject.layer = LayerMask.NameToLayer("Bullet");
                obj.GetComponent<BulletProjectile>().Init(
                    direction * BulletSpeed,
                    BulletScale,
                    Damage,
                    BulletLifetime
                );
            }
        );

        PlayMuzzleEffect();
    }

    private void PlayMuzzleEffect()
    {
        if (_muzzleEffectInstance == null) return;
        if (!Runner.IsForward)             return;

        _muzzleEffectInstance.SetActive(false);
        _muzzleEffectInstance.SetActive(true);
    }
}
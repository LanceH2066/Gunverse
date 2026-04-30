using Fusion;
using UnityEngine;

public class Weapons : NetworkBehaviour
{
    [Header("Setup")]
    public float WeaponSwitchTime = 0.5f;

    [HideInInspector] public Weapon[] AllWeapons;

    public bool IsSwitching => !_switchTimer.ExpiredOrNotRunning(Runner);

    [Networked, HideInInspector] public Weapon     CurrentWeapon  { get; set; }
    [Networked] private TickTimer                   _switchTimer   { get; set; }
    [Networked] private Weapon                      _pendingWeapon { get; set; }
    [Networked] public  Quaternion                  WeaponRotation { get; set; }

    public void Fire(bool justPressed)
    {
        if (CurrentWeapon == null || IsSwitching) return;
        CurrentWeapon.Fire(justPressed);
    }

    public void Reload()
    {
        if (CurrentWeapon == null || IsSwitching) return;
        CurrentWeapon.Reload();
    }

    public void SwitchWeapon(EWeaponType weaponType)
    {
        var newWeapon = GetWeapon(weaponType);
        if (newWeapon == null || !newWeapon.IsCollected)          return;
        if (newWeapon == CurrentWeapon && _pendingWeapon == null) return;
        if (newWeapon == _pendingWeapon)                          return;
        if (CurrentWeapon != null && CurrentWeapon.IsReloading)   return;

        _pendingWeapon = newWeapon;
        _switchTimer   = TickTimer.CreateFromSeconds(Runner, WeaponSwitchTime);
    }

    public bool PickupWeapon(EWeaponType weaponType)
    {
        var weapon = GetWeapon(weaponType);
        if (weapon == null) return false;

        if (weapon.IsCollected)
            weapon.AddAmmo(weapon.StartAmmo);
        else
            weapon.IsCollected = true;

        SwitchWeapon(weaponType);
        return true;
    }

    public Weapon GetWeapon(EWeaponType weaponType)
    {
        for (int i = 0; i < AllWeapons.Length; i++)
            if (AllWeapons[i].Type == weaponType)
                return AllWeapons[i];
        return null;
    }

    private void Awake()
    {
        AllWeapons = GetComponentsInChildren<Weapon>(includeInactive: true);
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentWeapon             = AllWeapons[0];
            CurrentWeapon.IsCollected = true;
        }

        UpdateVisibleWeapon();
    }

    public override void FixedUpdateNetwork()
    {
        TryActivatePendingWeapon();
    }

    public override void Render()
    {
        UpdateVisibleWeapon();
    }

    private Weapon _visibleWeapon;

    private void UpdateVisibleWeapon()
    {
        if (_visibleWeapon == CurrentWeapon) return;
        _visibleWeapon = CurrentWeapon;
        for (int i = 0; i < AllWeapons.Length; i++)
            AllWeapons[i].ToggleVisibility(AllWeapons[i] == CurrentWeapon);
    }

    private void TryActivatePendingWeapon()
    {
        if (!IsSwitching || _pendingWeapon == null) return;

        float? remaining = _switchTimer.RemainingTime(Runner);
        if (remaining.HasValue && remaining.Value > WeaponSwitchTime * 0.5f) return;

        CurrentWeapon  = _pendingWeapon;
        _pendingWeapon = null;
        CurrentWeapon.ToggleVisibility(true);
    }
}
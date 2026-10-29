using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerWeapons : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private Sprite _normalWeaponSprite;
    [SerializeField] private Sprite _sniperWeaponSprite;
    [SerializeField] private TextMeshProUGUI _maxAmmoText;
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _currentBombText;

    private PlayerWeaponMode _weaponMode;
    private PlayerGunFire _playerGun;
    private PlayerBombFire _playerBomb;

    private void Awake()
    {
        _weaponMode = _player.GetComponent<PlayerWeaponMode>();
        _playerGun = _player.GetComponent<PlayerGunFire>();
        _playerBomb = _player.GetComponent<PlayerBombFire>();
        _maxAmmoText.text = _playerGun.MaxAmmo.ToString();
        _currentAmmoText.text = _playerGun.CurrentAmmo.ToString();
    }

    private void OnEnable()
    {
        if (_weaponMode != null)
        {
            _weaponMode.OnModeChange += UpdateWeaponUI;
            UpdateWeaponUI(_weaponMode.Mode); // 시작 시 초기 상태 1회 동기화
        }
    }

    private void OnDisable()
    {
        if (_weaponMode != null)
        {
            _weaponMode.OnModeChange -= UpdateWeaponUI;
        }
    }

    private void Update()
    {
        RefreshAmmo();
        RefreshBomb();
    }

    private void UpdateWeaponUI(WeaponMode mode)
    {
        if (_weaponIcon == null)
        {
            return;
        }

        var isSniper = mode == WeaponMode.Sniper;
        _weaponIcon.sprite = isSniper ? _sniperWeaponSprite : _normalWeaponSprite;
    }

    public void UpdateWeaponUI()
    {
        if (_weaponMode != null)
        {
            UpdateWeaponUI(_weaponMode.Mode);
        }
    }

    private void RefreshBomb()
    {
        if (_playerBomb != null && _currentBombText != null)
        {
            _currentBombText.text = _playerBomb.CurrentBomb.ToString();
        }
    }

    private void RefreshAmmo()
    {
        if (_playerGun != null && _currentAmmoText != null)
        {
            _currentAmmoText.text = _playerGun.CurrentAmmo.ToString();
        }
    }
}

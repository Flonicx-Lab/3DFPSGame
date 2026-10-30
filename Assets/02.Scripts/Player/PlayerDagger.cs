using UnityEngine;

namespace FpsGame.Player
{
    // 단검 모드의 무기 표시와 공격 애니메이션을 제어한다.
    public sealed class PlayerDagger : MonoBehaviour
    {
        private const int DaggerLayerIndex = 4;
        private static readonly int s_stabHash = Animator.StringToHash("Stab");
        private static readonly int s_slashHash = Animator.StringToHash("Slash");

        [SerializeField] private GameObject _rifle;
        [SerializeField] private GameObject _dagger;
        [SerializeField] private PlayerAim _aim;
        [SerializeField] private PlayerDaggerAim _daggerAim;

        private PlayerWeaponMode _weaponMode;
        private Animator _animator;

        private void Awake()
        {
            _weaponMode = GetComponent<PlayerWeaponMode>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            _weaponMode.OnModeChange += HandleModeChange;
            HandleModeChange(_weaponMode.Mode);
        }

        private void OnDisable()
        {
            _weaponMode.OnModeChange -= HandleModeChange;
        }

        private void Update()
        {
            if (GameManager.Instance.State != GameState.Playing ||
                _weaponMode.Mode != WeaponMode.Dagger ||
                Cursor.visible)
            {
                return;
            }

            if (Input.GetButtonDown("Fire1"))
            {
                _animator.SetTrigger(s_slashHash);
            }

            if (Input.GetButtonDown("Fire2"))
            {
                _animator.SetTrigger(s_stabHash);
            }
        }

        private void HandleModeChange(WeaponMode mode)
        {
            var isDaggerMode = mode == WeaponMode.Dagger;
            _rifle.SetActive(!isDaggerMode);
            _dagger.SetActive(isDaggerMode);
            _aim.enabled = !isDaggerMode;
            _daggerAim.enabled = isDaggerMode;
            _animator.SetLayerWeight(DaggerLayerIndex, isDaggerMode ? 1f : 0f);
        }
    }
}

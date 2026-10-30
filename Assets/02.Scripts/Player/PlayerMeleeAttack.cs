using UnityEngine;
using FpsGame.Weapon;

namespace FpsGame.Player
{
    // 근접 무기 모드의 표시, 입력, 공격 애니메이션과 타격 시점을 제어한다.
    public sealed class PlayerMeleeAttack : MonoBehaviour
    {
        private const int DaggerLayerIndex = 4;
        private static readonly int s_stabHash = Animator.StringToHash("Stab");
        private static readonly int s_slashHash = Animator.StringToHash("Slash");

        [SerializeField] private GameObject _rifle;
        [SerializeField] private Dagger _dagger;
        [SerializeField] private PlayerAim _aim;
        [SerializeField] private PlayerDaggerAim _daggerAim;
        [SerializeField] private int _stabHitFrame = 32;
        [SerializeField] private int _slashHitFrame = 20;

        private PlayerWeaponMode _weaponMode;
        private Animator _animator;
        private int _pendingStateHash;
        private int _pendingHitFrame;
        private bool _hasPendingAttack;

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

            TryHitPendingAttack();

            if (Input.GetButtonDown("Fire1"))
            {
                _animator.SetTrigger(s_slashHash);
                SetPendingAttack(s_slashHash, _slashHitFrame);
            }

            if (Input.GetButtonDown("Fire2"))
            {
                _animator.SetTrigger(s_stabHash);
                SetPendingAttack(s_stabHash, _stabHitFrame);
            }
        }

        private void SetPendingAttack(int stateHash, int hitFrame)
        {
            _pendingStateHash = stateHash;
            _pendingHitFrame = hitFrame;
            _hasPendingAttack = true;
        }

        private void TryHitPendingAttack()
        {
            if (!_hasPendingAttack || !TryGetPendingState(out AnimatorStateInfo stateInfo, out AnimationClip clip))
            {
                return;
            }

            float hitNormalizedTime = _pendingHitFrame / (clip.length * clip.frameRate);
            if (stateInfo.normalizedTime < hitNormalizedTime)
            {
                return;
            }

            Transform cameraTransform = Camera.main.transform;
            if (_pendingStateHash == s_slashHash)
            {
                _dagger.Slash(cameraTransform.position, cameraTransform.forward);
            }
            else
            {
                _dagger.Stab(new Ray(cameraTransform.position, cameraTransform.forward));
            }

            _hasPendingAttack = false;
        }

        private bool TryGetPendingState(out AnimatorStateInfo stateInfo, out AnimationClip clip)
        {
            stateInfo = _animator.GetCurrentAnimatorStateInfo(DaggerLayerIndex);
            if (stateInfo.shortNameHash == _pendingStateHash)
            {
                AnimatorClipInfo[] clipInfo = _animator.GetCurrentAnimatorClipInfo(DaggerLayerIndex);
                clip = clipInfo.Length > 0 ? clipInfo[0].clip : null;
                return clip != null;
            }

            if (_animator.IsInTransition(DaggerLayerIndex))
            {
                stateInfo = _animator.GetNextAnimatorStateInfo(DaggerLayerIndex);
                if (stateInfo.shortNameHash == _pendingStateHash)
                {
                    AnimatorClipInfo[] clipInfo = _animator.GetNextAnimatorClipInfo(DaggerLayerIndex);
                    clip = clipInfo.Length > 0 ? clipInfo[0].clip : null;
                    return clip != null;
                }
            }

            clip = null;
            return false;
        }

        private void HandleModeChange(WeaponMode mode)
        {
            bool isDaggerMode = mode == WeaponMode.Dagger;
            _rifle.SetActive(!isDaggerMode);
            _dagger.gameObject.SetActive(isDaggerMode);
            _aim.enabled = !isDaggerMode;
            _daggerAim.enabled = isDaggerMode;
            _animator.SetLayerWeight(DaggerLayerIndex, isDaggerMode ? 1f : 0f);
        }
    }
}

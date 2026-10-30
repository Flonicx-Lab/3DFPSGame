using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _stabDamageMultiplier = 1.5f;
        [SerializeField] private int _stabHitFrame = 32;
        [SerializeField] private int _slashHitFrame = 20;
        [FormerlySerializedAs("_reach")]
        [SerializeField] private float _stabReach = 1.5f;
        [SerializeField] private float _slashReach = 2f;
        [SerializeField] private LayerMask _hitLayers;

        private PlayerWeaponMode _weaponMode;
        private Animator _animator;
        private int _pendingStateHash;
        private int _pendingHitFrame;
        private float _pendingDamage;
        private float _pendingReach;
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
                SetPendingAttack(s_slashHash, _slashHitFrame, _damage, _slashReach);
            }

            if (Input.GetButtonDown("Fire2"))
            {
                _animator.SetTrigger(s_stabHash);
                SetPendingAttack(s_stabHash, _stabHitFrame, _damage * _stabDamageMultiplier, _stabReach);
            }
        }

        private void SetPendingAttack(int stateHash, int hitFrame, float damage, float reach)
        {
            _pendingStateHash = stateHash;
            _pendingHitFrame = hitFrame;
            _pendingDamage = damage;
            _pendingReach = reach;
            _hasPendingAttack = true;
        }

        private void TryHitPendingAttack()
        {
            if (!_hasPendingAttack || !TryGetPendingState(out AnimatorStateInfo stateInfo, out AnimationClip clip))
            {
                return;
            }

            var hitNormalizedTime = _pendingHitFrame / (clip.length * clip.frameRate);
            if (stateInfo.normalizedTime < hitNormalizedTime)
            {
                return;
            }

            Transform cameraTransform = Camera.main.transform;
            var ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, _pendingReach, _hitLayers) &&
                hitInfo.collider.TryGetComponent(out IDamagable damagable))
            {
                var damageInfo = new DamageInfo
                {
                    Value = _pendingDamage,
                    HitPoint = hitInfo.point,
                    HitNormal = hitInfo.normal,
                };
                damagable.TakeDamage(damageInfo);
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
            var isDaggerMode = mode == WeaponMode.Dagger;
            _rifle.SetActive(!isDaggerMode);
            _dagger.SetActive(isDaggerMode);
            _aim.enabled = !isDaggerMode;
            _daggerAim.enabled = isDaggerMode;
            _animator.SetLayerWeight(DaggerLayerIndex, isDaggerMode ? 1f : 0f);
        }
    }
}

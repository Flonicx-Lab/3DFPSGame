using UnityEngine;

namespace FpsGame.Player
{
    // 소총을 겨눈 동안 총열의 위아래 방향을 카메라 조준점에 맞춘다.
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerAim : MonoBehaviour
    {
        private const int ThrowLayerIndex = 3;
        private static readonly int s_emptyStateHash = Animator.StringToHash("Empty");

        [SerializeField] private Transform _upperBody;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _aimDistance = 20f;

        private Animator _animator;
        private Transform _cameraTransform;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_animator.IsInTransition(ThrowLayerIndex)
                || _animator.GetCurrentAnimatorStateInfo(ThrowLayerIndex).shortNameHash != s_emptyStateHash)
            {
                // 투척 자세에 소총 기준 보정을 적용하면 상체가 크게 꺾인다.
                return;
            }

            Vector3 aimPosition = _cameraTransform.position + _cameraTransform.forward * _aimDistance;
            Vector3 aimDirection = aimPosition - _muzzle.position;
            Vector3 playerRight = transform.parent.right;
            var muzzlePitchDirection = Vector3.ProjectOnPlane(_muzzle.forward, playerRight);
            var aimPitchDirection = Vector3.ProjectOnPlane(aimDirection, playerRight);
            var angle = Vector3.SignedAngle(muzzlePitchDirection, aimPitchDirection, playerRight);

            _upperBody.rotation = Quaternion.AngleAxis(angle, playerRight) * _upperBody.rotation;
        }
    }
}

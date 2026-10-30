using UnityEngine;

namespace FpsGame.Player
{
    // 소총을 겨눈 동안 총열의 위아래와 좌우 방향을 카메라 조준점에 맞춘다.
    [RequireComponent(typeof(PlayerRifleGrip))]
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private Transform _upperBody;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _aimDistance = 20f;
        [SerializeField] private float _kickAngle = 3f; // 한 발에 더하는 반동 각도다.
        [SerializeField] private float _kickYawAngle = 0.8f; // 한 발 좌우 반동의 최대 각도다.
        [SerializeField] private float _kickRecoverSpeed = 25f; // 클수록 반동이 빠르게 돌아온다.

        private PlayerRifleGrip _rifleGrip;
        private Transform _cameraTransform;
        private float _currentKickAngle;
        private float _currentKickYawAngle;

        private void Awake()
        {
            _rifleGrip = GetComponent<PlayerRifleGrip>();
            _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            Vector3 aimPosition = _cameraTransform.position + _cameraTransform.forward * _aimDistance;
            Vector3 aimDirection = aimPosition - _muzzle.position;

            _upperBody.rotation = Quaternion.Slerp(
                    Quaternion.identity,
                    Quaternion.FromToRotation(_muzzle.forward, aimDirection),
                    _rifleGrip.ShoulderWeight)
                * _upperBody.rotation;

            _upperBody.rotation = Quaternion.AngleAxis(
                    -_currentKickAngle * _rifleGrip.ShoulderWeight,
                    _cameraTransform.right)
                * _upperBody.rotation;

            _upperBody.rotation = Quaternion.AngleAxis(
                    _currentKickYawAngle * _rifleGrip.ShoulderWeight,
                    _cameraTransform.up)
                * _upperBody.rotation;

            _currentKickAngle = Mathf.Lerp(
                _currentKickAngle,
                0f,
                1f - Mathf.Exp(-_kickRecoverSpeed * Time.deltaTime));

            _currentKickYawAngle = Mathf.Lerp(
                _currentKickYawAngle,
                0f,
                1f - Mathf.Exp(-_kickRecoverSpeed * Time.deltaTime));
        }

        public void Kick()
        {
            _currentKickAngle += _kickAngle;
            _currentKickYawAngle += Random.Range(-_kickYawAngle, _kickYawAngle);
        }
    }
}

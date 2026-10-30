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

        private PlayerRifleGrip _rifleGrip;
        private Transform _cameraTransform;

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
        }
    }
}

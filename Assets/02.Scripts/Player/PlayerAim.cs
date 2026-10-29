using UnityEngine;

namespace FpsGame.Player
{
    // 총열의 위아래 방향을 카메라 조준점에 맞춘다.
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private Transform _upperBody;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _aimDistance = 20f;

        private Transform _cameraTransform;

        private void Awake()
        {
            _cameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
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

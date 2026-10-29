using UnityEngine;

namespace FpsGame.Player
{
    // 카메라 조준점을 Humanoid LookAt IK 목표로 전달한다.
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private float _aimDistance = 20f;
        [SerializeField] private float _weight = 1f;
        [SerializeField] private float _bodyWeight = 1f;
        [SerializeField] private float _headWeight;
        [SerializeField] private float _eyesWeight;
        [SerializeField] private float _clampWeight = 0.5f;

        private Animator _animator;
        private Transform _cameraTransform;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _cameraTransform = Camera.main.transform;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            Vector3 aimPosition = _cameraTransform.position + _cameraTransform.forward * _aimDistance;

            _animator.SetLookAtWeight(_weight, _bodyWeight, _headWeight, _eyesWeight, _clampWeight);
            _animator.SetLookAtPosition(aimPosition);
        }
    }
}

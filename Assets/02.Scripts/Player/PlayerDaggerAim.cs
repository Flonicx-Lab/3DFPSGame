using UnityEngine;

namespace FpsGame.Player
{
    // 단검 타격점이 눈에서 본 카메라 앞 방향과 일치하도록 상체 방향을 맞춘다.
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerDaggerAim : MonoBehaviour
    {
        private const int DaggerLayerIndex = 4;
        private static readonly int s_stabStateHash = Animator.StringToHash("Stab");
        private static readonly int s_slashStateHash = Animator.StringToHash("Slash");

        [SerializeField] private Transform _upperBody;
        [SerializeField] private Transform _eye;
        [SerializeField] private Vector3 _stabStrikePoint = new Vector3(-0.487f, 0.512f, 0.137f);
        [SerializeField] private Vector3 _slashStrikePoint = new Vector3(-0.881f, 0.646f, -0.140f);
        [SerializeField] private float _blendTime = 0.22f;

        private Animator _animator;
        private Transform _camera;
        private Vector3 _currentStrikePoint;
        private float _weight;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _camera = Camera.main.transform;
        }

        private void OnEnable()
        {
            _weight = 0f;
        }

        private void LateUpdate()
        {
            AnimatorStateInfo stateInfo = _animator.IsInTransition(DaggerLayerIndex)
                ? _animator.GetNextAnimatorStateInfo(DaggerLayerIndex)
                : _animator.GetCurrentAnimatorStateInfo(DaggerLayerIndex);
            var targetWeight = 0f;

            if (stateInfo.shortNameHash == s_stabStateHash)
            {
                _currentStrikePoint = _stabStrikePoint;
                targetWeight = 1f;
            }
            else if (stateInfo.shortNameHash == s_slashStateHash)
            {
                _currentStrikePoint = _slashStrikePoint;
                targetWeight = 1f;
            }

            _weight = Mathf.MoveTowards(_weight, targetWeight, Time.deltaTime / _blendTime);

            if (_weight == 0f)
            {
                return;
            }

            var rotation = Quaternion.FromToRotation(
                _upperBody.TransformPoint(_currentStrikePoint) - _eye.position,
                _camera.forward);
            _upperBody.rotation = Quaternion.Slerp(Quaternion.identity, rotation, _weight)
                * _upperBody.rotation;
        }
    }
}

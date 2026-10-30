using UnityEngine;

namespace FpsGame.Player
{
    // 단검 찌르기와 베기 타격점 방향을 카메라에 맞추도록 회전을 척추에 나눠 적용한다.
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerDaggerAim : MonoBehaviour
    {
        private const int DaggerLayerIndex = 4;
        private static readonly int s_stabStateHash = Animator.StringToHash("Stab");
        private static readonly int s_slashStateHash = Animator.StringToHash("Slash");

        [SerializeField] private Transform[] _spineBones;
        [SerializeField] private Transform _upperBody;
        [SerializeField] private Transform _eye;
        [SerializeField] private Vector3 _stabStrikePoint = new Vector3(-0.606f, 0.211f, -0.723f);
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

            foreach (Transform bone in _spineBones)
            {
                bone.rotation = Quaternion.Slerp(Quaternion.identity, rotation, _weight / _spineBones.Length)
                    * bone.rotation;
            }
        }
    }
}

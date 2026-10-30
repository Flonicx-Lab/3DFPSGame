using UnityEngine;

namespace FpsGame.Player
{
    // 소총의 견착과 투척 중 왼손으로 드는 자세를 함께 다룬다.
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerRifleGrip : MonoBehaviour
    {
        private const int ThrowLayerIndex = 3;
        private const int IkLayerIndex = 5;
        private static readonly int s_emptyStateHash = Animator.StringToHash("Empty");

        [Header("소총")]
        [SerializeField] private Transform _rifle;
        [SerializeField] private Transform _carryAnchor;
        [SerializeField] private float _carryBlendTime = 0.2f;

        [Header("손잡이")]
        [SerializeField] private Transform _rightHandGrip;
        [SerializeField] private Transform _leftHandGrip;

        private Animator _animator;
        private Vector3 _shoulderLocalPosition;
        private Quaternion _shoulderLocalRotation;
        private float _carryWeight;

        public float ShoulderWeight => 1.0f - _carryWeight;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _shoulderLocalPosition = _rifle.localPosition;
            _shoulderLocalRotation = _rifle.localRotation;
        }

        private void Update()
        {
            var targetWeight = IsThrowing() ? 1.0f : 0.0f;
            _carryWeight = Mathf.MoveTowards(
                _carryWeight,
                targetWeight,
                Time.deltaTime / _carryBlendTime);
        }

        private void LateUpdate()
        {
            Vector3 carryLocalPosition = _rifle.parent.InverseTransformPoint(_carryAnchor.position);
            Quaternion carryLocalRotation = Quaternion.Inverse(_rifle.parent.rotation) * _carryAnchor.rotation;

            _rifle.localPosition = Vector3.Lerp(_shoulderLocalPosition, carryLocalPosition, _carryWeight);
            _rifle.localRotation = Quaternion.Slerp(_shoulderLocalRotation, carryLocalRotation, _carryWeight);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (layerIndex != IkLayerIndex)
            {
                return;
            }

            var weight = (1.0f - _carryWeight)
                * (_leftHandGrip.gameObject.activeInHierarchy ? 1.0f : 0.0f);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weight);
            _animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandGrip.position);
            _animator.SetIKRotation(AvatarIKGoal.RightHand, _rightHandGrip.rotation);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandGrip.position);
        }

        private bool IsThrowing()
        {
            AnimatorStateInfo throwState = _animator.GetCurrentAnimatorStateInfo(ThrowLayerIndex);
            return throwState.shortNameHash != s_emptyStateHash
                || _animator.IsInTransition(ThrowLayerIndex);
        }
    }
}

using UnityEngine;

namespace FpsGame.Player
{
    // 소총을 쥔 자세일 때 양손을 손잡이 목표에 고정한다.
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerRifleGrip : MonoBehaviour
    {
        private const int ThrowLayerIndex = 3;
        private const int IkLayerIndex = 5;
        private static readonly int s_emptyStateHash = Animator.StringToHash("Empty");

        [SerializeField] private Transform _rightHandGrip;
        [SerializeField] private Transform _leftHandGrip;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (layerIndex != IkLayerIndex)
            {
                return;
            }

            AnimatorStateInfo throwState = _animator.GetCurrentAnimatorStateInfo(ThrowLayerIndex);
            var isThrowLayerEmpty = throwState.shortNameHash == s_emptyStateHash
                && !_animator.IsInTransition(ThrowLayerIndex);

            // 투척 중 양손 IK가 소총을 따라가며 던지는 자세를 막지 않도록 끈다.
            var weight = _leftHandGrip.gameObject.activeInHierarchy && isThrowLayerEmpty ? 1.0f : 0.0f;
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, weight);
            _animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandGrip.position);
            _animator.SetIKRotation(AvatarIKGoal.RightHand, _rightHandGrip.rotation);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandGrip.position);
        }
    }
}

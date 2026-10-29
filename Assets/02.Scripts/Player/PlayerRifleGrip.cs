using UnityEngine;

namespace FpsGame.Player
{
    // 소총을 들 때 왼손을 손잡이 목표에 고정한다.
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerRifleGrip : MonoBehaviour
    {
        private const int IkLayerIndex = 5;

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

            var weight = _leftHandGrip.gameObject.activeInHierarchy ? 1.0f : 0.0f;
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
            _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandGrip.position);
        }
    }
}

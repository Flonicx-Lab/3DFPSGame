using UnityEngine;

namespace FpsGame.Player
{
    // 상태 진입과 종료에 맞춰 단검의 역수 쥐기 자세를 전환한다.
    public sealed class ReverseGripState : StateMachineBehaviour
    {
        private DaggerGrip _daggerGrip;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            GetDaggerGrip(animator).SetReverse(true);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            GetDaggerGrip(animator).SetReverse(false);
        }

        private DaggerGrip GetDaggerGrip(Animator animator)
        {
            if (_daggerGrip == null)
            {
                _daggerGrip = animator.GetComponentInChildren<DaggerGrip>(true);
            }

            return _daggerGrip;
        }
    }
}

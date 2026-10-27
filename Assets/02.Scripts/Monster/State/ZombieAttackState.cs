using UnityEngine;

// 공격 : 일정 시간마다 플레이어를 때린다.
public class ZombieAttackState : IZombieState
{
    private readonly Zombie _zombie;
    private float _attackTimer;

    public ZombieAttackState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _attackTimer = 0f;
        _zombie.StopMovement();
        _zombie.SetAnimationTrigger(ZombieAnimatorParams.MoveToAttackDelay);
        // 붙자마자 첫 공격을 시작한다. 이후 공격은 간격마다 한다.
        _zombie.SetAnimationTrigger(ZombieAnimatorParams.StartAttack);
    }

    public void Update()
    {
        // 플레이어가 공격 사정거리 밖으로 나가면 쫒아간다.
        if (!_zombie.IsPlayerInAttackRange())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        _zombie.TurnToPlayer();
        _attackTimer += Time.deltaTime;

        if (_attackTimer >= _zombie.AttackInterval)
        {
            // 데미지는 공격 클립의 애니메이션 이벤트가 타격 프레임에 준다.
            _zombie.SetAnimationTrigger(ZombieAnimatorParams.StartAttack);
            _attackTimer = 0f;
        }
    }

    public void Exit()
    {
        _zombie.SetAnimationTrigger(ZombieAnimatorParams.AttackToMove);
    }
}

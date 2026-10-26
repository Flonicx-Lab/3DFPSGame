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
    }

    public void Update()
    {
        // 플레이어가 공격 사정거리 밖으로 나가면 쫒아간다.
        if (!_zombie.IsPlayerInAttackRange())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        _attackTimer += Time.deltaTime;

        if (_attackTimer >= _zombie.AttackInterval)
        {
            _zombie.AttackPlayer();
            _attackTimer = 0f;
        }
    }

    public void Exit()
    {
    }
}

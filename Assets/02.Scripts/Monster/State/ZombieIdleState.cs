using UnityEngine;

// 대기 : 순찰 지점에서 잠시 쉬었다가 다시 순찰한다.
public class ZombieIdleState : IZombieState
{
    private const float IdleDuration = 2f;

    private readonly Zombie _zombie;
    private float _idleTimer;

    public ZombieIdleState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _idleTimer = 0f;
        _zombie.SetAnimationTrigger(ZombieAnimatorParams.MoveToIdle);
    }

    public void Update()
    {
        if (_zombie.IsPlayerDetected())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        _idleTimer += Time.deltaTime;

        if (_idleTimer >= IdleDuration)
        {
            _zombie.ChangeState(ZombieState.Patrol);
        }
    }

    // 대기에서 나가는 상태는 모두 이동 동작을 쓰므로 나갈 때 한 번만 넘긴다.
    public void Exit()
    {
        _zombie.SetAnimationTrigger(ZombieAnimatorParams.IdleToMove);
    }
}

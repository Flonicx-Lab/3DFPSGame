// 복귀 : 처음 자리로 돌아간다.
public class ZombieComebackState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieComebackState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
    }

    public void Update()
    {
        // 링크에 도착하면 점프로 넘어간다. 다른 전환보다 먼저 봐야 링크 위에서 멈추지 않는다.
        if (_zombie.IsOnNavMeshLink())
        {
            _zombie.ChangeState(ZombieState.Jump);
            return;
        }

        if (_zombie.IsNearStart())
        {
            _zombie.ChangeState(ZombieState.Patrol);
            return;
        }

        _zombie.MoveToStart();
    }

    public void Exit()
    {
    }
}

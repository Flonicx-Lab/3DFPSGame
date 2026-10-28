// 추격 : 플레이어를 쫒아간다.
public class ZombieTraceState : IZombieState
{
    private readonly Zombie _zombie;

    public ZombieTraceState(Zombie zombie)
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

        // 처음 자리에서 너무 멀어지면 돌아간다.
        if (_zombie.IsFarFromStart())
        {
            _zombie.ChangeState(ZombieState.Comeback);
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다.
        if (_zombie.IsPlayerInAttackRange())
        {
            _zombie.ChangeState(ZombieState.Attack);
            return;
        }

        _zombie.MoveToPlayer();
    }

    public void Exit()
    {
    }
}

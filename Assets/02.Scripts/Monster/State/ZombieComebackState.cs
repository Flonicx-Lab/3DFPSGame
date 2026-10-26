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

using UnityEngine;

// 순찰 : 시작 지점 주변 두 지점을 왕복한다.
public class ZombiePatrolState : IZombieState
{
    private const float SpeedRatio = 0.5f; // 추격 속도 대비 순찰 속도

    private readonly Zombie _zombie;
    private Vector3 _pointA;
    private Vector3 _pointB;
    private Vector3 _currentTarget;

    public ZombiePatrolState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void SetPatrolPoints(Vector3 pointA, Vector3 pointB)
    {
        _pointA = pointA;
        _pointB = pointB;
        _currentTarget = pointA;
    }

    public void Enter()
    {
    }

    public void Update()
    {
        if (_zombie.IsPlayerDetected())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        // 도착하면 다음 목표를 반대편 지점으로 정해 두고 쉰다. 대기가 끝나면 그 지점으로 출발한다.
        if (_zombie.HasArrivedAt(_currentTarget))
        {
            _currentTarget = _currentTarget == _pointA ? _pointB : _pointA;
            _zombie.ChangeState(ZombieState.Idle);
            return;
        }

        _zombie.MoveTo(_currentTarget, SpeedRatio);
    }

    public void Exit()
    {
    }
}

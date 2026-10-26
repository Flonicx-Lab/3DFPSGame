using UnityEngine;

// 순찰 : 시작 지점 주변 두 지점을 왕복한다.
public class ZombiePatrolState : IZombieState
{
    private const float ArrivalDistance = 0.2f; // 목표 지점 도착으로 보는 거리
    private const float SpeedRatio = 0.5f; // 추격 속도 대비 순찰 속도

    private readonly Zombie _zombie;
    private readonly Vector3 _pointA;
    private readonly Vector3 _pointB;
    private Vector3 _currentTarget;

    public ZombiePatrolState(Zombie zombie, Vector3 pointA, Vector3 pointB)
    {
        _zombie = zombie;
        _pointA = pointA;
        _pointB = pointB;
        _currentTarget = pointA;
    }

    public void Enter()
    {
    }

    public void Update()
    {
        // 1. 플레이어가 탐지 거리 이내이면 추격 시작
        if (_zombie.IsPlayerDetected())
        {
            _zombie.ChangeState(ZombieState.Trace);
            return;
        }

        // 2. 현재 순찰 목표 지점 근처에 도착하면 반대편 지점으로 목표 전환
        if (_zombie.GetHorizontalDistanceTo(_currentTarget) < ArrivalDistance)
        {
            _currentTarget = _currentTarget == _pointA ? _pointB : _pointA;
        }

        // 3. 목표 지점을 향해 이동
        _zombie.MoveTo(_currentTarget, SpeedRatio);
    }

    public void Exit()
    {
    }
}

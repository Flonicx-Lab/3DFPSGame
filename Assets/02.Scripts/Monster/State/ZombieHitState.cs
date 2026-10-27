using System.Collections;
using UnityEngine;

// 피격 : 공격을 받아 잠깐 멈칫한다.
public class ZombieHitState : IZombieState
{
    private const float HitDuration = 0.5f;

    private readonly Zombie _zombie;
    private readonly WaitForSeconds _hitWait = new(HitDuration);

    public ZombieHitState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _zombie.StartCoroutine(Hit_Coroutine());
    }

    public void Update()
    {
    }

    public void Exit()
    {
    }

    private IEnumerator Hit_Coroutine()
    {
        _zombie.SetAnimationTrigger(ZombieAnimatorParams.Hit);
        yield return _hitWait;
        _zombie.ChangeState(ZombieState.Trace);
    }
}

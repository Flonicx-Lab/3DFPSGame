using System.Collections;
using Lean.Pool;
using UnityEngine;

// 사망 : 쓰러지고 잠시 뒤 사라진다.
public class ZombieDieState : IZombieState
{
    private const float DieDuration = 2.4f;

    private readonly Zombie _zombie;
    private readonly WaitForSeconds _dieWait = new(DieDuration);

    public ZombieDieState(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void Enter()
    {
        _zombie.StartCoroutine(Die_Coroutine());
    }

    public void Update()
    {
    }

    public void Exit()
    {
    }

    private IEnumerator Die_Coroutine()
    {
        _zombie.DisableBody();
        // 연속 피격으로 처리되지 않고 남은 Hit 트리거가 있으면 Any State 전이로 사망 동작을 끊는다.
        _zombie.ResetAnimationTrigger(ZombieAnimatorParams.Hit);
        _zombie.SetAnimationTrigger(ZombieAnimatorParams.Die);
        yield return _dieWait;
        LeanPool.Despawn(_zombie.gameObject);
    }
}

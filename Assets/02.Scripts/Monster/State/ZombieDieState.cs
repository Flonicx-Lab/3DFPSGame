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
        _zombie.DisableMovement();
        _zombie.SetAnimationTrigger(ZombieAnimatorParams.Die);
        yield return _dieWait;
        LeanPool.Despawn(_zombie.gameObject);
    }
}

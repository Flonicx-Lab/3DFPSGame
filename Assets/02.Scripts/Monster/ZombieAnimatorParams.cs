using UnityEngine;

// ZombieAC 애니메이터의 트리거 해시. 컨트롤러에서 파라미터 이름을 바꾸면 여기도 같이 바꿔야 한다.
public static class ZombieAnimatorParams
{
    public static readonly int IdleToMove = Animator.StringToHash("IdleToMove");
    public static readonly int MoveToIdle = Animator.StringToHash("MoveToIdle");
    public static readonly int MoveToAttackDelay = Animator.StringToHash("MoveToAttackDelay");
    public static readonly int StartAttack = Animator.StringToHash("StartAttack");
    public static readonly int AttackToMove = Animator.StringToHash("AttackToMove");
    public static readonly int Die = Animator.StringToHash("Die");
    public static readonly int Hit = Animator.StringToHash("Hit");
}

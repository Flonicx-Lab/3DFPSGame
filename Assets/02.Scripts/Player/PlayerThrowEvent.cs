using UnityEngine;

// 애니메이션 이벤트는 Animator가 붙은 오브젝트에만 오므로 부모의 폭탄 투척에 넘긴다.
public class PlayerThrowEvent : MonoBehaviour
{
    private PlayerBombFire _bombFire;

    private void Awake()
    {
        _bombFire = GetComponentInParent<PlayerBombFire>();
    }

    public void ThrowEvent()
    {
        _bombFire.ThrowBomb();
    }
}

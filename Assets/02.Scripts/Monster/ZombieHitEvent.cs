using UnityEngine;

public class ZombieHitEvent : MonoBehaviour
{
    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponentInParent<Zombie>();
    }

    public void HitEvent()
    {
        _zombie.AttackAction();
    }
}

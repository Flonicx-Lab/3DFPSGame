using UnityEngine;

// 뼈 하나의 피격 판정을 받아 좀비에게 맞은 부위와 함께 넘긴다.
[RequireComponent(typeof(Collider))]
public class ZombieHitbox : MonoBehaviour, IDamagable
{
    private Zombie _zombie;
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    public void SetOwner(Zombie zombie)
    {
        _zombie = zombie;
    }

    public void SetEnabled(bool isEnabled)
    {
        _collider.enabled = isEnabled;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        _zombie.TakeDamage(damageInfo, transform);
    }
}

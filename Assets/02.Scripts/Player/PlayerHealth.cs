using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [SerializeField] private float _maxHealth;
    private float _health;

    public float Health => _health;
    public float MaxHealth => _maxHealth;

    private void Awake()
    {
        _health = _maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f)
        {
            return;
        }

        _health -= damageInfo.Value;

        Debug.Log($"플레이어가 맞았다. 데미지 {damageInfo.Value}, 남은 체력 {_health}");

        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다");
        }
    }
}

using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamagable
{
    [SerializeField] private float _maxHealth;
    private float _health;

    public float Health => _health;
    public float MaxHealth => _maxHealth;

    // Action 자료형은 함수들을 저장할 수 있다.
    // event 로 외부에서 수정,실행 못하게 방어
    public event Action OnDamaged;

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

        OnDamaged?.Invoke(); //구독자들에게 알림 주기


        Debug.Log($"플레이어가 맞았다. 데미지 {damageInfo.Value}, 남은 체력 {_health}");

        if (_health <= 0f)
        {
            Debug.Log("플레이어가 쓰러졌다");
            GameManager.Instance.GameOver();
        }
    }
}

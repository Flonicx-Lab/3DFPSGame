using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _staminaSlider;

    private PlayerHealth _playerHealth;
    private PlayerStamina _playerStamina;

    private void Awake()
    {
        _playerHealth = FindAnyObjectByType<PlayerHealth>();
        _playerStamina = FindAnyObjectByType<PlayerStamina>();
    }

    private void Start()
    {
        RefreshHealth();
        _playerHealth.OnDamaged += RefreshHealth;
    }

    private void OnDestroy()
    {
        _playerHealth.OnDamaged -= RefreshHealth;
    }

    private void LateUpdate()
    {
        // 0 ~ 100% -> 0 ~ 1
        // 좀비의 현재 체력 / 최대 체력
        // 문제 : 체력값이 변하지 않았음에도 초당 60회 이상 캔버스를 불필요하게 다시 그린다. -> 성능 낭비가 일어난다.
        // 불필요한 연산이 다른 UI에서 누적될 수록 느려진다.
        // 해결: 값이 바뀌었을 때만 캔버스 갱신을 한다.

        // 이 때 쓰는게 -> 옵저버 패턴...
        // C# 에서는 Action으로 구현할 수 있다.


        _staminaSlider.value = _playerStamina.Stamina / _playerStamina.MaxStamina;
    }

    private void RefreshHealth()
    {
        _healthSlider.value = _playerHealth.Health / _playerHealth.MaxHealth;
    }
}

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

    private void LateUpdate()
    {
        // 0 ~ 100% -> 0 ~ 1
        // 좀비의 현재 체력 / 최대 체력
        _healthSlider.value = _playerHealth.Health / _playerHealth.MaxHealth;
        _staminaSlider.value = _playerStamina.Stamina / _playerStamina.MaxStamina;
    }
}

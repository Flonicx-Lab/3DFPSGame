using System;
using UnityEngine;

public sealed class PlayerStamina : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _maxStamina = 100f;
    [SerializeField, Min(0f)] private float _recoveryPerSecond = 15f;

    private float _currentStamina;

    public float CurrentStamina => _currentStamina;
    public float MaxStamina => _maxStamina;

    private void Awake()
    {
        _currentStamina = _maxStamina;
    }

    public bool TrySpend(float amount)
    {
        if (amount < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (_currentStamina < amount)
        {
            return false;
        }

        _currentStamina -= amount;
        return true;
    }

    public void Recover(float deltaTime)
    {
        _currentStamina = Mathf.MoveTowards(
            _currentStamina,
            _maxStamina,
            _recoveryPerSecond * deltaTime);
    }
}

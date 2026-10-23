using System;
using UnityEngine;

public sealed class PlayerStamina : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _maxStamina = 100f;
    [SerializeField, Min(0f)] private float _recoveryPerSecond = 40f;

    private float _stamina;

    public float Stamina => _stamina;
    public float MaxStamina => _maxStamina;

    private void Awake()
    {
        _stamina = _maxStamina;
    }

    public bool TrySpend(float amount)
    {
        if (amount < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (_stamina < amount)
        {
            return false;
        }

        _stamina -= amount;
        return true;
    }

    public void Recover(float deltaTime)
    {
        _stamina = Mathf.MoveTowards(
            _stamina,
            _maxStamina,
            _recoveryPerSecond * deltaTime);
    }
}

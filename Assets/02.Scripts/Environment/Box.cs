using UnityEngine;

public class Box : MonoBehaviour, IDamagable
{
    [SerializeField] private float _health = 30f;

    public void TakeDamage(float damage)
    {
        _health -= damage;
    }
}

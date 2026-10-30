using Lean.Pool;
using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 사라지는 패턴
public class Bomb : MonoBehaviour
{
    private const float ExplosionEffectReturnDelay = 1.5f;

    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private AudioClip _explosionClip;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _explosionDamage = 100f;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }

    // 다른 콜라이더와 충돌하는 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        // 1. 폭발 이펙트 풀링 재생 및 지연 회수
        if (_explosionEffectPrefab != null)
        {
            ParticleSystem effect = LeanPool.Spawn(_explosionEffectPrefab, transform.position, Quaternion.identity);
            effect.Play();
            LeanPool.Despawn(effect.gameObject, ExplosionEffectReturnDelay);
        }
        AudioSource.PlayClipAtPoint(_explosionClip, transform.position);
        ApplyAreaDamage();

        // 2. 폭탄 본체 풀 반환
        LeanPool.Despawn(gameObject);
    }

    private void ApplyAreaDamage()
    {
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        foreach (Collider col in colliders)
        {
            if (!col.TryGetComponent(out IDamagable damagable))
            {
                continue;
            }

            Vector3 hitPoint = col.ClosestPoint(transform.position);
            if (Physics.Linecast(transform.position, hitPoint, LayerMask.GetMask("Wall")))
            {
                continue;
            }

            DamageInfo damageInfo = new()
            {
                Value = _explosionDamage,
                HitPoint = hitPoint,
                HitNormal = (transform.position - hitPoint).normalized
            };
            damagable.TakeDamage(damageInfo);
        }
    }
}

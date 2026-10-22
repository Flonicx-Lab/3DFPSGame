using System.Collections;
using UnityEngine;

// 체력이 0이 되면 위로 튀어오르며 폭발하는 드럼통
[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("스텟")]
    [SerializeField] private float _health = 30f;
    [SerializeField] private float _launchForce = 20f;
    [SerializeField] private float _spinForce = 5f;
    [SerializeField] private float _destroyDelay = 3f;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void TakeDamage(float damage)
    {
        if (_health <= 0f)
        {
            return;
        }

        _health -= damage;

        if (_health <= 0f)
        {
            StartCoroutine(Explode_Coroutine());
        }
    }

    private IEnumerator Explode_Coroutine()
    {
        // 1. 폭발 이펙트를 생성한다.
        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        // 2. 위로 튀어오르며 빙글빙글 돈다.
        _rb.AddForce(Vector3.up * _launchForce, ForceMode.Impulse);
        _rb.AddTorque(Random.insideUnitSphere * _spinForce, ForceMode.Impulse);

        // 3. 딜레이 후 사라진다.
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}

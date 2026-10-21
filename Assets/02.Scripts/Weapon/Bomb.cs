using UnityEngine;

// 무언가에 부딪히면 폭발 이펙트를 남기고 사라지는 패턴
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;
    [SerializeField] private int _maxBomb = 10;

    // 다른 콜라이더와 충돌하는 순간 실행되는 메서드
    private void OnCollisionEnter(Collision collision)
    {
        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        // Todo. 오브젝트 풀링 적용
        Destroy(gameObject);
    }
}

using System.Collections;
using Lean.Pool;
using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    private const float HitEffectReturnDelay = 1.0f;

    [Header("스텟")]
    [SerializeField] private ParticleSystem _hitEffectPrefab;
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireInterval = 0.1f;
    [SerializeField] private int _maxAmmo = 50;
    [SerializeField] private float _reloadDuration = 1.5f;

    private float _fireTimer = 0f;
    private int _currentAmmo;
    private Coroutine _reloadRoutine;

    private void Start()
    {
        _currentAmmo = _maxAmmo;
    }

    private void Update()
    {
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 수동 재장전 입력 받기
        if (Input.GetButtonDown("Reload") && _currentAmmo < _maxAmmo && _reloadRoutine == null)
        {
            _reloadRoutine = StartCoroutine(ReloadRoutine());
        }

        // 2. 마우스 왼쪽 버튼 입력 받기 (재장전 중이 아니고 탄약이 있을 때만 발사)
        if (Input.GetButton("Fire1") && _fireTimer <= 0f && _currentAmmo > 0)
        {
            _currentAmmo--;
            _fireTimer = _fireInterval;
            Shoot();

            if (_reloadRoutine != null)
            {
                StopCoroutine(_reloadRoutine);
                Debug.Log("재장전 취소!");
                _reloadRoutine = null;
            }
        }
    }

    private void Shoot()
    {
        Transform cameraTransform = Camera.main.transform;
        // 2. 레이(광선)을 생성하고 발사될 위치와 진행 방향 설정
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딪힌 대상의 정보를 저장할 변수 생성
        var hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딪힌 물체가 있다면 이펙트 표시
        var isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            // 5. 오브젝트 풀을 통해 피격 이펙트 생성 및 지연 반환
            ParticleSystem effect =
                LeanPool.Spawn(_hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            effect.Play();
            LeanPool.Despawn(effect.gameObject, HitEffectReturnDelay);
        }
    }

    private IEnumerator ReloadRoutine()
    {
        Debug.Log("재장전 시작!");

        yield return new WaitForSeconds(_reloadDuration); // reloadDuration 기간동안 쉰다. yield return
        _currentAmmo = _maxAmmo;

        Debug.Log("재장전 완료!");
        _reloadRoutine = null;
    }
}

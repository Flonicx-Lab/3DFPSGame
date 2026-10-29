using System;
using System.Collections;
using Lean.Pool;
using UnityEngine;
using Random = UnityEngine.Random;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    private const float HitEffectReturnDelay = 1.0f;
    private const int FireLayerIndex = 1;
    private static readonly int s_fireHash = Animator.StringToHash("Fire");

    [Header("스텟")]
    [SerializeField] private GameObject[] _muzzleFlashes;
    [SerializeField] private float _flashTime = 0.05f;
    [SerializeField] private ParticleSystem _hitEffectPrefab;
    [SerializeField] private GunLaser _laser;
    [SerializeField] private float _range = 100f;
    [SerializeField] private LayerMask _hitLayers; // 좀비 루트 캡슐 대신 뼈 히트박스에 맞도록 Zombie 레이어는 뺀다.
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireInterval = 0.1f;
    [SerializeField] private int _maxAmmo = 50;
    [SerializeField] private float _reloadDuration = 1.5f;

    private float _fireTimer = 0f;
    private int _currentAmmo;
    public int CurrentAmmo => _currentAmmo;

    public int MaxAmmo => _maxAmmo;

    private Awaitable _reloadAwaitable;
    private Animator _animator;

    private void Awake()
    {
        _currentAmmo = _maxAmmo;
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // 1. 수동 재장전 입력 받기
        if (Input.GetButtonDown("Reload") && _currentAmmo < _maxAmmo && _reloadAwaitable == null)
        {
            ReloadAsync();
        }

        // 2. 마우스 왼쪽 버튼 입력 받기 (재장전 중이 아니고 탄약이 있을 때만 발사)
        var isFireInputActive = !Cursor.visible && Input.GetButton("Fire1") && _currentAmmo > 0;
        _animator.SetBool(s_fireHash, isFireInputActive);

        if (isFireInputActive && _fireTimer <= 0f)
        {
            _currentAmmo--;
            _fireTimer = _fireInterval;
            Shoot();
            _animator.Play(s_fireHash, FireLayerIndex, 0f);

            if (_reloadAwaitable != null)
            {
                _reloadAwaitable.Cancel();
                _reloadAwaitable = null;

                Debug.Log("재장전 취소!");
            }
        }
    }

    private void Shoot()
    {
        StartCoroutine(MuzzleFalsh_Routine());

        Transform cameraTransform = Camera.main.transform;
        // 2. 레이(광선)을 생성하고 발사될 위치와 진행 방향 설정
        var ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딪힌 대상의 정보를 저장할 변수 생성
        var hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딪힌 물체가 있다면 이펙트 표시
        var isHit = Physics.Raycast(ray, out hitInfo, _range, _hitLayers);
        _laser.Show(isHit ? hitInfo.point : ray.GetPoint(_range));
        if (isHit)
        {
            // 5. 오브젝트 풀을 통해 피격 이펙트 생성 및 지연 반환
            ParticleSystem effect =
                LeanPool.Spawn(_hitEffectPrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            effect.Play();
            LeanPool.Despawn(effect.gameObject, HitEffectReturnDelay);

            var damageInfo = new DamageInfo();
            damageInfo.Value = _damage;
            damageInfo.HitPoint = hitInfo.point;
            damageInfo.HitNormal = hitInfo.normal;

            // 맞은 대상이 좀비라면 TakeDamage
            hitInfo.collider.GetComponent<IDamagable>()?.TakeDamage(damageInfo);
        }
    }

    private async Awaitable ReloadAsync()
    {
        Debug.Log("재장전 시작!");

        try
        {
            _reloadAwaitable = Awaitable.WaitForSecondsAsync(_reloadDuration); // reloadDuration 기간동안 쉰다. yield return
            await _reloadAwaitable;

            _currentAmmo = _maxAmmo;
            Debug.Log("재장전 완료!");
        }
        catch (OperationCanceledException)
        {
            // Cancel()이 호출되면 이곳으로 즉시 탈출하여 리로드 차단
        }
        finally
        {
            _reloadAwaitable = null;
        }
    }

    private IEnumerator MuzzleFalsh_Routine()
    {
        GameObject flash = _muzzleFlashes[Random.Range(0, _muzzleFlashes.Length)];
        flash.SetActive(true);
        yield return new WaitForSeconds(_flashTime);
        flash.SetActive(false);
    }
}

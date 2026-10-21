using Lean.Pool;
using UnityEngine;

public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f;
    [SerializeField] private int _maxBomb = 10;
    [SerializeField] private float _maxChargingTime = 1.5f;

    private int _currentBomb;
    private float _currentChargingTime;

    private void Awake()
    {
        _currentBomb = _maxBomb;
    }

    private void Update()
    {
        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2") && _currentBomb > 0)
        {
            Charging();
        }

        if (Input.GetButtonUp("Fire2") && _currentBomb > 0)
        {
            Shoot();
            _currentChargingTime = 0f;
        }
    }

    private void Shoot()
    {
        // 2. 오브젝트 풀을 통해 발사 위치에서 수류탄 대여
        Bomb bomb = LeanPool.Spawn(_bombPrefab, _firePoint.position, Quaternion.identity);
        _currentBomb--;

        // 3. 재사용 시 물리 속도 초기화 후 시선 방향으로 투척
        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(Camera.main.transform.forward * _throwPower, ForceMode.Impulse);
        }
    }

    private void Charging()
    {
        _currentChargingTime += Time.deltaTime;

        if (_currentChargingTime >= _maxChargingTime)
        {
            _currentChargingTime = _maxChargingTime;
        }
    }
}

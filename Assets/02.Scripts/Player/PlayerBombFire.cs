using Lean.Pool;
using UnityEngine;

// 우클릭을 누르고 있으면 폭탄을 든 채 힘을 모으고, 떼면 던지는 동작에서 손을 놓는 순간 날린다.
public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrefab;
    [SerializeField] private float _throwPower = 15f; // 차징 없이 던질 때의 힘
    [SerializeField] private float _maxThrowPower = 25f; // 최대로 차징했을 때의 힘
    [SerializeField] private int _maxBomb = 10;
    [SerializeField] private float _maxChargingTime = 1.5f;

    private int _currentBomb;
    public int CurrentBomb => _currentBomb;
    private float _currentChargingTime;
    private bool _isHolding;
    private bool _isThrowing; // 버튼을 뗀 뒤 손을 놓는 이벤트가 올 때까지
    private Animator _animator;
    private PlayerWeaponMode _weaponMode;

    private void Awake()
    {
        _currentBomb = _maxBomb;
        _animator = GetComponentInChildren<Animator>();
        _weaponMode = GetComponent<PlayerWeaponMode>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        if (_weaponMode.Mode != WeaponMode.Normal)
        {
            return;
        }

        // 1. 마우스 오른쪽 버튼을 누르면 폭탄을 들고 차징을 시작한다.
        if (Input.GetButtonDown("Fire2") && _currentBomb > 0 && !_isThrowing)
        {
            _isHolding = true;
            _currentChargingTime = 0f;
            _animator.SetBool("IsHolding", true);
        }

        if (!_isHolding)
        {
            return;
        }

        Charging();

        // 2. 버튼을 떼면 던지는 동작으로 넘어간다. 폭탄은 동작의 이벤트가 날린다.
        if (Input.GetButtonUp("Fire2"))
        {
            _isHolding = false;
            _isThrowing = true;
            _animator.SetBool("IsHolding", false);
        }
    }

    // 던지는 동작에서 손을 놓는 프레임의 애니메이션 이벤트가 부른다.
    public void ThrowBomb()
    {
        _isThrowing = false;

        // 3. 오브젝트 풀을 통해 발사 위치에서 수류탄 대여
        Bomb bomb = LeanPool.Spawn(_bombPrefab, _firePoint.position, Quaternion.identity);
        _currentBomb--;

        // 4. 재사용 시 물리 속도 초기화 후 차징한 만큼 센 힘으로 시선 방향 투척
        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        if (rb != null)
        {
            var throwPower = Mathf.Lerp(_throwPower, _maxThrowPower, _currentChargingTime / _maxChargingTime);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(Camera.main.transform.forward * throwPower, ForceMode.Impulse);
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

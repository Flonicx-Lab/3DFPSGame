using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerStamina), typeof(PlayerHealth))]
public class PlayerMove : MonoBehaviour
{
    private const float BaseGravity = -20f;
    private const int MaxJumpCount = 2;
    private static readonly int s_moveMotionHash = Animator.StringToHash("MoveMotion");

    private int _jumpCount = 0;
    private float _yVelocity = 0f;
    private bool _runRequiresRelease = false;
    private Vector3 _currentHorizontalVelocity = Vector3.zero;

    private float _coyoteTimer = 0f;
    private float _jumpBufferTimer = 0f;

    private Animator _animator;
    private PlayerHealth _health;

    [Header("스텟")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _runSpeed = 12f;
    [SerializeField] private float _injuredMoveSpeed = 1.8f;
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField, Min(0f)] private float _runStaminaPerSecond = 20f;
    [SerializeField, Min(0f)] private float _jumpStaminaCost = 10f;

    [Header("물리 및 조작감")]
    [SerializeField] private float _groundAcceleration = 45f;
    [SerializeField] private float _groundDeceleration = 25f;
    [SerializeField, Range(0.1f, 1f)] private float _airControlMultiplier = 0.7f;
    [SerializeField] private float _fallGravityMultiplier = 2.5f;
    [SerializeField] private float _terminalVelocity = -25f;
    [SerializeField] private float _jumpCutoffMultiplier = 0.4f;

    [Header("입력 관용도")]
    [SerializeField, Min(0f)] private float _coyoteDuration = 0.1f;
    [SerializeField, Min(0f)] private float _jumpBufferDuration = 0.1f;

    [Header("컴포넌트")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private PlayerStamina _stamina;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stamina = GetComponent<PlayerStamina>();
        _health = GetComponent<PlayerHealth>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // 0. 관용도 타이머를 지난 시간만큼 줄인다.
        _coyoteTimer -= Time.deltaTime;
        _jumpBufferTimer -= Time.deltaTime;

        // 1. 키보드 입력을 받는다.
        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");

        // 2. 입력에 따른 방향을 구한다.
        Vector3 direction = new Vector3(h, 0, v).normalized;
        direction = transform.TransformDirection(direction);

        var isGrounded = _characterController.isGrounded;

        // 3. 지면 접지 상태 갱신
        if (isGrounded)
        {
            _coyoteTimer = _coyoteDuration;

            if (_yVelocity < 0f)
            {
                _yVelocity = 0f;
                _jumpCount = 0;
            }
        }

        // 4. 점프 입력 버퍼 등록
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferTimer = _jumpBufferDuration;
        }

        // 5. 점프 판정 및 실행
        var jumpedThisFrame = false;

        if (_jumpBufferTimer > 0f)
        {
            // 5-1. 지상 점프 또는 코요테 유예 점프
            if (isGrounded || _coyoteTimer > 0f)
            {
                if (_stamina.TrySpend(_jumpStaminaCost))
                {
                    _yVelocity = _jumpPower;
                    _jumpCount = 1;
                    _coyoteTimer = 0f;
                    _jumpBufferTimer = 0f;
                    jumpedThisFrame = true;
                }
            }
            // 5-2. 공중 2단 점프
            else if (_jumpCount < MaxJumpCount)
            {
                if (_stamina.TrySpend(_jumpStaminaCost))
                {
                    _yVelocity = _jumpPower;
                    _jumpCount++;
                    _jumpBufferTimer = 0f;
                    jumpedThisFrame = true;
                }
            }
        }

        // 6. 가변 점프: 상승 중에 점프 버튼에서 손을 떼면 Y속도 강제 컷오프
        if (Input.GetButtonUp("Jump") && _yVelocity > _jumpPower * _jumpCutoffMultiplier)
        {
            _yVelocity = _jumpPower * _jumpCutoffMultiplier;
        }

        // 7. 빠른 낙하: 정점 통과 후 하강 구간 중력 배가 적용
        var currentGravity = _yVelocity < 0f ? BaseGravity * _fallGravityMultiplier : BaseGravity;
        _yVelocity += currentGravity * Time.deltaTime;

        // 8. 종단 낙하 속도 제한
        if (_yVelocity < _terminalVelocity)
        {
            _yVelocity = _terminalVelocity;
        }

        // 9. 달리기 상태 판정
        var runKeyPressed = Input.GetButton("Sprint");

        if (!runKeyPressed)
        {
            _runRequiresRelease = false;
        }

        var wantsToRun = runKeyPressed
                         && isGrounded
                         && !jumpedThisFrame
                         && direction.sqrMagnitude > 0f
                         && !_runRequiresRelease;

        var isRunning = wantsToRun
                        && _stamina.TrySpend(_runStaminaPerSecond * Time.deltaTime);

        if (wantsToRun && !isRunning)
        {
            _runRequiresRelease = true;
        }

        if (!isRunning && isGrounded && !jumpedThisFrame)
        {
            _stamina.Recover(Time.deltaTime);
        }

        var targetSpeed = isRunning ? _runSpeed : _moveSpeed;

        if (_health.IsInjured)
        {
            targetSpeed = _injuredMoveSpeed;
        }

        Vector3 targetVelocity = direction * targetSpeed;

        // 10. 지상 가감속 및 공중 제어 보간
        var hasInput = direction.sqrMagnitude > 0f;
        var baseRate = hasInput ? _groundAcceleration : _groundDeceleration;
        var actualRate = isGrounded ? baseRate : baseRate * _airControlMultiplier;

        _currentHorizontalVelocity = Vector3.MoveTowards(
            _currentHorizontalVelocity,
            targetVelocity,
            actualRate * Time.deltaTime
        );

        var referenceSpeed = _health.IsInjured ? _injuredMoveSpeed : _moveSpeed;
        var moveMotion = Mathf.Clamp01(_currentHorizontalVelocity.magnitude / referenceSpeed);
        _animator.SetFloat(s_moveMotionHash, moveMotion);

        // 11. 최종 속도 합성
        Vector3 finalVelocity = _currentHorizontalVelocity;
        finalVelocity.y = _yVelocity;

        // 12. 최종 이동 실행 및 충돌 플래그 수신
        CollisionFlags collisionFlags = _characterController.Move(finalVelocity * Time.deltaTime);

        // 13. 천장 충돌 즉시 낙하 처리
        if ((collisionFlags & CollisionFlags.Above) != 0 && _yVelocity > 0f)
        {
            _yVelocity = 0f;
        }
    }
}

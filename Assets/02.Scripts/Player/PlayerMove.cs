using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerStamina))]
public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private const int MaxJumpCount = 2;

    private int _jumpCount = 0;
    private float _yVelocity = 0f; // 위아래 속도, 매 프레임마다 중력 가속도가 붙는다.
    private bool _runRequiresRelease = false;

    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _runSpeed = 12f;
    [SerializeField] private float _jumpPower = 10f;
    [SerializeField, Min(0f)] private float _runStaminaPerSecond = 20f;
    [SerializeField, Min(0f)] private float _jumpStaminaCost = 10f;

    private CharacterController _characterController;
    private PlayerStamina _stamina;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stamina = GetComponent<PlayerStamina>();
    }

    private void Update()
    {
        // 1. 키보드 입력을 받는다.
        var h = Input.GetAxisRaw("Horizontal"); // -1 ~ 1
        var v = Input.GetAxisRaw("Vertical");

        // 2. 키보드 입력에 따라 방향을 구한다.
        Vector3 direction = new Vector3(h, 0, v).normalized; // 정규화 -> 대각선 문제 제거
        direction = transform.TransformDirection(direction);

        // 3. 땅에 붙어있을 때
        var isGrounded = _characterController.isGrounded;

        if (isGrounded && _yVelocity < 0f)
        {
            _yVelocity = 0f;
            _jumpCount = 0;
        }

        // 4. 점프상태
        var jumpedThisFrame = false;

        if (Input.GetButtonDown("Jump")
            && _jumpCount < MaxJumpCount
            && _stamina.TrySpend(_jumpStaminaCost))
        {
            _yVelocity = _jumpPower;
            _jumpCount++;
            jumpedThisFrame = true;
        }

        _yVelocity += Gravity * Time.deltaTime;

        // 5. 달리기상태
        var runKeyPressed = Input.GetKey(KeyCode.LeftShift);

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

        var currentSpeed = isRunning ? _runSpeed : _moveSpeed;

        // 6. 최종 속도를 구한다.
        Vector3 velocity = direction * currentSpeed;
        velocity.y = _yVelocity;

        // 7. 방향과 속력에 따라 이동한다.
        _characterController.Move(velocity * Time.deltaTime);
    }
}

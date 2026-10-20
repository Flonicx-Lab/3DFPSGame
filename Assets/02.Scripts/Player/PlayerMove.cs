using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private float _yVelocity = 0f; // 위아래 속도, 매 프레임마다 중력 가속도가 붙는다.

    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 키보드 입력을 받는다.
        var h = Input.GetAxisRaw("Horizontal"); // -1 ~ 1
        var v = Input.GetAxisRaw("Vertical");

        // 2. 키보드 입력에 따라 방향을 구한다.
        var direction = new Vector3(h, 0, v).normalized; // 정규화 -> 대각선 문제 제거
        direction = transform.TransformDirection(direction);

        // 로컬 좌표계: 한 대상을 중심
        // 월드 좌표계: 세상을 중심

        // 3. 중력을 누적한다.
        _yVelocity += Gravity * Time.deltaTime;

        // 4. 속도를 구한다.
        var velocity = direction * _moveSpeed;
        velocity.y = _yVelocity;

        // 5. 방향과 속력에 따라 이동한다.
        // transform.position += direction * _moveSpeed * Time.deltaTime;

        _characterController.Move(velocity * Time.deltaTime);
    }
}
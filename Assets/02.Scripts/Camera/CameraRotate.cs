using UnityEngine;

// 역할: 마우스를 움직이면 카메라를 그 방향으로 회전하고 싶다.
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;
    private float _accumulationX = 0f; // 좌우 회전 각도
    private float _accumulationY = 0f; // 위아래 회전 각도

    private void Update()
    {
        // 1. 마우스 입력을 받는다.
        var mouseX = Input.GetAxis("Mouse X");
        var mouseY = Input.GetAxis("Mouse Y");

        // 2. 마우스 입력 만큼 회전값을 누적한다.
        var deltaTime = Time.deltaTime;
        _accumulationX += mouseX * _rotationSpeed * deltaTime;
        _accumulationY += -mouseY * _rotationSpeed * deltaTime;

        // 3. 누적하기 전에 내 변수에서 먼저 -90 ~ 90도로 제한
        _accumulationY = Mathf.Clamp(_accumulationY, -90, 90f);

        // 4. 누적한 회전값을 카메라에 적용한다.
        transform.eulerAngles = new Vector3(_accumulationY, _accumulationX, 0);
    }
}
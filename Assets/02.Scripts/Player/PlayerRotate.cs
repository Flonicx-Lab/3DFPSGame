using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f; // 카메라 회전속력과 똑같이

    private float _accumulationX = 0f; // 좌 우 회전

    private void Update()
    {
        var mouseX = Input.GetAxis("Mouse X");
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;

        transform.eulerAngles = new Vector3(0f, _accumulationX, 0f);
    }
}
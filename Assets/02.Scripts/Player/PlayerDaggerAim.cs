using UnityEngine;

namespace FpsGame.Player
{
    // 단검 타격점이 카메라 중앙 광선에 놓이도록 상체 방향을 맞춘다.
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerDaggerAim : MonoBehaviour
    {
        [SerializeField] private Transform _upperBody;
        [SerializeField] private Vector3 _strikePoint = new Vector3(-0.54f, 0.29f, -0.44f);

        private Transform _camera;

        private void Awake()
        {
            _camera = Camera.main.transform;
        }

        private void LateUpdate()
        {
            var upperBodyPosition = _upperBody.position;
            var strikePosition = _upperBody.TransformPoint(_strikePoint);
            var strikeDirection = strikePosition - upperBodyPosition;
            var radius = strikeDirection.magnitude;
            var cameraToUpperBody = _camera.position - upperBodyPosition;
            var projection = Vector3.Dot(cameraToUpperBody, _camera.forward);
            var discriminant = projection * projection - cameraToUpperBody.sqrMagnitude + radius * radius;

            if (discriminant < 0f)
            {
                return;
            }

            var distance = -projection + Mathf.Sqrt(discriminant);
            var targetPosition = _camera.position + distance * _camera.forward;
            _upperBody.rotation = Quaternion.FromToRotation(
                strikeDirection,
                targetPosition - upperBodyPosition) * _upperBody.rotation;
        }
    }
}

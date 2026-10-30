using UnityEngine;

namespace FpsGame.Player
{
    // 단검의 바로 쥐기 자세와 역수 쥐기 자세를 전환한다.
    public sealed class DaggerGrip : MonoBehaviour
    {
        [SerializeField] private Vector3 _reverseLocalPosition;
        [SerializeField] private Vector3 _reverseLocalEulerAngles;

        private Vector3 _forwardLocalPosition;
        private Quaternion _forwardLocalRotation;

        private void Awake()
        {
            _forwardLocalPosition = transform.localPosition;
            _forwardLocalRotation = transform.localRotation;
        }

        public void SetReverse(bool isReverse)
        {
            Vector3 localPosition = isReverse ? _reverseLocalPosition : _forwardLocalPosition;
            Quaternion localRotation = isReverse
                ? Quaternion.Euler(_reverseLocalEulerAngles)
                : _forwardLocalRotation;

            transform.SetLocalPositionAndRotation(localPosition, localRotation);
        }
    }
}

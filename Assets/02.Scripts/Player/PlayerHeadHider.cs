using UnityEngine;

namespace FpsGame.Player
{
    // 활성화 중에는 머리를 숨기고 비활성화되면 원래 크기로 되돌린다.
    public sealed class PlayerHeadHider : MonoBehaviour
    {
        [SerializeField] private Transform _head;

        private Vector3 _originalLocalScale;

        private void Awake()
        {
            _originalLocalScale = _head.localScale;
        }

        private void LateUpdate()
        {
            _head.localScale = Vector3.zero;
        }

        private void OnDisable()
        {
            _head.localScale = _originalLocalScale;
        }
    }
}

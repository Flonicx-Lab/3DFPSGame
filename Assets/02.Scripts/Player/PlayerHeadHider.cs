using UnityEngine;

namespace FpsGame.Player
{
    // 로컬 플레이어의 머리가 1인칭 화면에 렌더링되지 않게 숨긴다.
    public sealed class PlayerHeadHider : MonoBehaviour
    {
        [SerializeField] private Transform _head;

        private void LateUpdate()
        {
            _head.localScale = Vector3.zero;
        }
    }
}

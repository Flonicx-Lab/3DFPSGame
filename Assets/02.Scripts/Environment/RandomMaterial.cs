using UnityEngine;

namespace FpsGame.Environment
{
    // 생성될 때 Renderer에 머티리얼 목록 중 하나를 무작위로 적용한다.
    public sealed class RandomMaterial : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material[] _materials;

        private void Awake()
        {
            _renderer.sharedMaterial = _materials[Random.Range(0, _materials.Length)];
        }
    }
}

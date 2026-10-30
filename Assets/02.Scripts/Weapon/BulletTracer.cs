using Lean.Pool;
using UnityEngine;

namespace FpsGame.Weapon
{
    // 예광탄의 월드 이동과 풀 반환을 맡는다.
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class BulletTracer : MonoBehaviour
    {
        [SerializeField] private float _speed = 200f;

        private TrailRenderer _trailRenderer;
        private Vector3 _end;
        private bool _isMoving;

        private void Awake()
        {
            _trailRenderer = GetComponent<TrailRenderer>();
        }

        private void Update()
        {
            if (!_isMoving)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, _end, _speed * Time.deltaTime);
            if (transform.position == _end)
            {
                _isMoving = false;
                LeanPool.Despawn(gameObject, _trailRenderer.time);
            }
        }

        public void Launch(Vector3 end)
        {
            _end = end;
            transform.LookAt(_end);
            _trailRenderer.Clear();
            _isMoving = true;
        }
    }
}

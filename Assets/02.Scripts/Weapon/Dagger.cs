using UnityEngine;

namespace FpsGame.Weapon
{
    // 단검의 공격 수치와 베기 및 찌르기 피해 판정을 맡는다.
    public sealed class Dagger : MonoBehaviour
    {
        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _stabDamageMultiplier = 1.5f;
        [SerializeField] private float _stabReach = 1.5f;
        [SerializeField] private float _slashReach = 2f;
        [SerializeField] private float _slashAngle = 90f;
        [SerializeField] private LayerMask _hitLayers = 3575;

        public void Slash(Vector3 origin, Vector3 forward)
        {
            FanAttack.Hit(origin, forward, _slashReach, _slashAngle, _hitLayers, _damage);
        }

        public void Stab(Ray ray)
        {
            if (!Physics.Raycast(ray, out RaycastHit hitInfo, _stabReach, _hitLayers) ||
                !hitInfo.collider.TryGetComponent(out IDamagable damagable))
            {
                return;
            }

            var damageInfo = new DamageInfo
            {
                Value = _damage * _stabDamageMultiplier,
                HitPoint = hitInfo.point,
                HitNormal = hitInfo.normal,
            };
            damagable.TakeDamage(damageInfo);
        }
    }
}

using UnityEngine;
using UnityEngine.Serialization;

namespace FpsGame.Weapon
{
    // 단검의 부채꼴 공격과 찌르기 피해 판정을 맡는다.
    public sealed class Dagger : MonoBehaviour
    {
        private const int MaxColliderCount = 64;
        private static readonly Collider[] s_colliders = new Collider[MaxColliderCount];
        private static readonly IDamagable[] s_damagables = new IDamagable[MaxColliderCount];

        [SerializeField] private float _damage = 20f;
        [SerializeField] private float _stabDamageMultiplier = 1.5f;
        [SerializeField] private float _stabReach = 1.5f;
        [FormerlySerializedAs("_slashReach")]
        [SerializeField] private float _fanAttackReach = 2f;
        [FormerlySerializedAs("_slashAngle")]
        [SerializeField] private float _fanAttackAngle = 90f;
        [SerializeField] private LayerMask _hitLayers = 3575;

        public void FanAttack(Vector3 origin, Vector3 forward)
        {
            Vector3 horizontalForward = forward;
            horizontalForward.y = 0f;
            var hasHorizontalForward = horizontalForward.sqrMagnitude > Mathf.Epsilon;
            if (hasHorizontalForward)
            {
                horizontalForward.Normalize();
            }

            var minimumDot = Mathf.Cos(_fanAttackAngle * 0.5f * Mathf.Deg2Rad);
            var colliderCount = Physics.OverlapSphereNonAlloc(origin, _fanAttackReach, s_colliders, _hitLayers);
            var damagableCount = 0;

            for (var i = 0; i < colliderCount; i++)
            {
                Collider collider = s_colliders[i];
                if (!collider.TryGetComponent(out IDamagable damagable) ||
                    ContainsDamagable(damagable, damagableCount))
                {
                    continue;
                }

                Vector3 hitPoint = collider.ClosestPoint(origin);
                Vector3 toHitPoint = hitPoint - origin;
                var containsOrigin = toHitPoint.sqrMagnitude <= Mathf.Epsilon;
                Vector3 horizontalDirection = toHitPoint;
                horizontalDirection.y = 0f;
                var isWithinAngle = hasHorizontalForward &&
                    (horizontalDirection.sqrMagnitude <= Mathf.Epsilon ||
                    Vector3.Dot(horizontalForward, horizontalDirection.normalized) >= minimumDot);
                if (!containsOrigin && !isWithinAngle)
                {
                    continue;
                }

                s_damagables[damagableCount] = damagable;
                damagableCount++;

                var damageInfo = new DamageInfo
                {
                    Value = _damage,
                    HitPoint = hitPoint,
                    HitNormal = (origin - hitPoint).normalized,
                };
                damagable.TakeDamage(damageInfo);
            }
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

        private static bool ContainsDamagable(IDamagable damagable, int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (ReferenceEquals(s_damagables[i], damagable))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

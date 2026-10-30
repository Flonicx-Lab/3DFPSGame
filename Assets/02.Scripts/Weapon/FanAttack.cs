using UnityEngine;

namespace FpsGame.Weapon
{
    // 기준점 앞의 수평 부채꼴 범위에 있는 대상마다 피해를 한 번 준다.
    public static class FanAttack
    {
        private const int MaxColliderCount = 64;
        private static readonly Collider[] s_colliders = new Collider[MaxColliderCount];
        private static readonly IDamagable[] s_damagables = new IDamagable[MaxColliderCount];

        public static void Hit(
            Vector3 origin,
            Vector3 forward,
            float radius,
            float angle,
            LayerMask hitLayers,
            float damage)
        {
            Vector3 horizontalForward = forward;
            horizontalForward.y = 0f;
            bool hasHorizontalForward = horizontalForward.sqrMagnitude > Mathf.Epsilon;
            if (hasHorizontalForward)
            {
                horizontalForward.Normalize();
            }

            float minimumDot = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
            int colliderCount = Physics.OverlapSphereNonAlloc(origin, radius, s_colliders, hitLayers);
            int damagableCount = 0;

            for (int i = 0; i < colliderCount; i++)
            {
                Collider collider = s_colliders[i];
                if (!collider.TryGetComponent(out IDamagable damagable) ||
                    ContainsDamagable(damagable, damagableCount))
                {
                    continue;
                }

                Vector3 hitPoint = collider.ClosestPoint(origin);
                Vector3 toHitPoint = hitPoint - origin;
                bool containsOrigin = toHitPoint.sqrMagnitude <= Mathf.Epsilon;
                Vector3 horizontalDirection = toHitPoint;
                horizontalDirection.y = 0f;
                bool isWithinAngle = hasHorizontalForward &&
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
                    Value = damage,
                    HitPoint = hitPoint,
                    HitNormal = (origin - hitPoint).normalized,
                };
                damagable.TakeDamage(damageInfo);
            }
        }

        private static bool ContainsDamagable(IDamagable damagable, int count)
        {
            for (int i = 0; i < count; i++)
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

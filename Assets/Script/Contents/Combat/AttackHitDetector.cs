using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 공격 이펙트에 부착되어 히트 감지 및 데미지 처리
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AttackHitDetector : MonoBehaviour
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private bool isTrigger = true;

        private LayerMask enemyLayer;
        private HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        private UserCombat ownerCombat;

        private void Awake()
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = isTrigger;
            }
        }

        public void Initialize(UserCombat combat, float attackDamage, LayerMask layer)
        {
            ownerCombat = combat;
            damage = attackDamage;
            enemyLayer = layer;
            hitTargets.Clear();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            $"🔍 [AttackHitDetector] OnTriggerEnter2D: {collision.gameObject.name}".DLog();

            if (!IsEnemyLayer(collision.gameObject))
            {
                $"🔍 [AttackHitDetector] 레이어 불일치: {collision.gameObject.layer}".DLog();
                return;
            }

            var damageable = collision.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = collision.GetComponentInParent<IDamageable>();
            }
            if (damageable == null)
            {
                damageable = collision.transform.root.GetComponent<IDamageable>();
            }

            if (damageable != null && !hitTargets.Contains(damageable))
            {
                hitTargets.Add(damageable);
                $"⚔️ [AttackHitDetector] 히트 감지: {damageable.GetTransform().name}, 데미지: {damage}".DLog();
                ownerCombat?.OnHitDetected(damageable, damage, collision.ClosestPoint(transform.position));
            }
            else if (damageable == null)
            {
                $"🔍 [AttackHitDetector] IDamageable을 찾을 수 없음: {collision.gameObject.name}".DLog();
            }
        }

        private bool IsEnemyLayer(GameObject obj)
        {
            return (enemyLayer.value & (1 << obj.layer)) != 0;
        }

        public void ResetHitTargets()
        {
            hitTargets.Clear();
        }
    }
}

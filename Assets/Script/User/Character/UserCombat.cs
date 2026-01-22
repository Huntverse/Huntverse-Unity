using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 플레이어 전투 시스템 - 공격, 히트 감지, 데미지 처리
    /// </summary>
    public class UserCombat : MonoBehaviour
    {
        [Header("COMBAT SETTINGS")]
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private LayerMask enemyLayer;

        public async UniTask<VfxHandle> SpawnAttackVfx(string vfxKey, Vector3 position, Quaternion rotation, Vector3? scale = null)
        {
            if (VfxManager.Shared == null)
            {
                $"⚔️ [UserCombat] VfxManager.Shared가 null!".DError();
                return null;
            }

            var playerScale = transform.localScale;
            var vfxScale = scale ?? new Vector3(playerScale.x, 1f, 1f);

            var vfxHandle = await VfxManager.Shared.PlayOneShot(
                vfxKey,
                position,
                rotation,
                this.transform,
                vfxScale
            );

            if (vfxHandle != null && vfxHandle.IsVaild)
            {
                SetupHitDetector(vfxHandle.vfxObject, position);
            }

            return vfxHandle;
        }

        private void SetupHitDetector(VfxObject vfxObject, Vector3 hitPosition)
        {
            var hitDetector = vfxObject.GetComponent<AttackHitDetector>();
            if (hitDetector == null)
            {
                var collider = vfxObject.GetComponent<Collider2D>();
                if (collider == null)
                {
                    var boxCollider = vfxObject.gameObject.AddComponent<BoxCollider2D>();
                    boxCollider.size = new Vector2(1f, 1f);
                    boxCollider.isTrigger = true;
                    $"⚔️ [UserCombat] Collider2D 추가: {vfxObject.gameObject.name}".DLog();
                }
                hitDetector = vfxObject.gameObject.AddComponent<AttackHitDetector>();
                $"⚔️ [UserCombat] AttackHitDetector 추가: {vfxObject.gameObject.name}".DLog();
            }

            hitDetector.Initialize(this, baseDamage, enemyLayer);
            $"⚔️ [UserCombat] HitDetector 초기화 완료, EnemyLayer: {enemyLayer.value}".DLog();
        }

        /// <summary>
        /// 히트 감지 시 호출되는 콜백
        /// </summary>
        public void OnHitDetected(IDamageable target, float damage, Vector3 hitPosition)
        {
            if (target == null)
            {
                $"⚔️ [UserCombat] OnHitDetected: target이 null".DError();
                return;
            }

            $"⚔️ [UserCombat] 데미지 적용: {target.GetTransform().name}, 데미지: {damage}".DLog();
            target.TakeDamage(damage, hitPosition);
            SpawnDamageText(damage, hitPosition).Forget();
        }

        private async UniTaskVoid SpawnDamageText(float damage, Vector3 position)
        {   

            var prefab = await AbLoader.Shared.LoadInstantiateAsync(ResourceKeyConst.Kp_DamageText_Vfx);

            if (prefab != null)
            {
                var damageText = prefab.GetComponent<DamageText>();
                damageText.Initialize(damage, position);
            }
        }

        public void SetBaseDamage(float damage)
        {
            baseDamage = damage;
        }
    }
}

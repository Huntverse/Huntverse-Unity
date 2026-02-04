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

        /// <summary>키 + 위치 + 시간 구간으로 스폰 후 Hit 감지 부착. 부모가 Animator 정규화 시간 알 때 사용.</summary>
        public async UniTask<VfxHandle> SpawnVfx(string key, Vector3 position, Quaternion rotation, Transform parent, float startTime, float endTime)
        {
            if (VfxManager.Shared == null) return null;
            var handle = await VfxManager.Shared.PlayOneShot(key, position, rotation, parent, startTime, endTime);
            if (handle != null && handle.IsVaild)
                SetupHitDetectorFor(handle.vfxObject);
            return handle;
        }

        /// <summary>스폰된 VFX에 Hit 감지 부착. VfxManager.PlayOneShot 후 호출.</summary>
        public void SetupHitDetectorFor(VfxObject vfxObject)
        {
            if (vfxObject == null) return;
            SetupHitDetector(vfxObject, vfxObject.transform.position);
        }

        private void SetupHitDetector(VfxObject vfxObject, Vector3 hitPosition)
        {
            var hitDetector = vfxObject.GetComponent<AttackHitDetector>();
            if (hitDetector == null)
            {
                if (vfxObject.GetComponent<Collider2D>() == null)
                {
                    var boxCollider = vfxObject.gameObject.AddComponent<BoxCollider2D>();
                    boxCollider.size = new Vector2(1f, 1f);
                    boxCollider.isTrigger = true;
                }
                hitDetector = vfxObject.gameObject.AddComponent<AttackHitDetector>();
            }
            hitDetector.Initialize(this, baseDamage, enemyLayer);
        }

        /// <summary>
        /// 히트 감지 시 호출되는 콜백
        /// </summary>
        public void OnHitDetected(IDamageable target, float damage, Vector3 hitPosition)
        {
            if (target == null) return;
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

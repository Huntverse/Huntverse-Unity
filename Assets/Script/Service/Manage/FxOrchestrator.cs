using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 애니메이션 이벤트를 받아 VFX/SFX를 일관되게 재생하는 중앙 오케스트레이터.
    /// - 외부에서는 이 클래스만 통해 이펙트/사운드를 트리거한다.
    /// </summary>
    public class FxOrchestrator : MonoBehaviourSingleton<FxOrchestrator>
    {
        [SerializeField] private FxEventTable fxEventTable;

        protected override bool DontDestroy => true;

        /// <summary>
        /// 애니메이션 이벤트 진입점. Animator가 붙은 모델에서 호출된다.
        /// </summary>
        public async UniTask OnAnimEvent(Animator animator, string eventId, Transform spawnRoot)
        {
            if (fxEventTable == null || animator == null || string.IsNullOrEmpty(eventId))
                return;

            var config = fxEventTable.GetConfig(eventId);
            if (config == null)
                return;

            var ownerCombat = animator.GetComponentInParent<UserCombat>();

            // VFX
            if (config.vfxEntries != null && VfxManager.Shared != null)
            {
                foreach (var v in config.vfxEntries)
                {
                    if (string.IsNullOrEmpty(v.vfxKey)) continue;

                    var root = spawnRoot != null ? spawnRoot : animator.transform;
                    var pos = root.position + root.rotation * v.offset;
                    var rot = root.rotation * Quaternion.Euler(v.rotationOffsetEuler);

                    var handle = await VfxManager.Shared.PlayOneShot(v.vfxKey, pos, rot, root);
                    if (handle != null && handle.IsVaild && v.attachHit && ownerCombat != null)
                    {
                        ownerCombat.SetupHitDetectorFor(handle.vfxObject);
                    }
                }
            }

            // SFX
            if (config.sfxEntries != null && AudioManager.Shared != null)
            {
                foreach (var s in config.sfxEntries)
                {
                    var key = AudioKeyConst.GetSfxKey(s.audioType);
                    if (string.IsNullOrEmpty(key)) continue;
                    AudioManager.Shared.PlaySfx(key, s.volumeScale);
                }
            }
        }
    }
}


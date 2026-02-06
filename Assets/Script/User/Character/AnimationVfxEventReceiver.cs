using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// Animator가 붙은 모델(astera@model 등)에 붙임. Animation Event를 FxOrchestrator로 전달.
    /// 애니 클립에서는 VfxSpawn/FxEvent(string eventId) 형태로 호출하면 된다.
    /// </summary>
    public class AnimationVfxEventReceiver : MonoBehaviour
    {
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        /// <summary>기존 VfxSpawn(string) 이벤트용.</summary>
        public void VfxSpawn(string eventId)
        {
            FxEvent(eventId);
        }

        /// <summary>새로운 FxEvent(string) 이벤트용.</summary>
        public void FxEvent(string eventId)
        {
            if (FxOrchestrator.Shared == null) return;
            if (_animator == null) _animator = GetComponent<Animator>();
            FxOrchestrator.Shared.OnAnimEvent(_animator, eventId, transform).Forget();
        }
    }
}

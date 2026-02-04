using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 무기에 붙임. 직접 넣어둔 Trail을 끄고 켜기만 함.
    /// 애니 클립마다 해당 프레임에 이벤트: OnTrail("Start") / OnTrail("End") 또는 OnTrailBegin / OnTrailEnd.
    /// </summary>
    public class SwordTrailEffect : MonoBehaviour
    {
        [SerializeField] private TrailRenderer trailRenderer;

        private void Awake()
        {
            if (trailRenderer == null)
                trailRenderer = GetComponentInChildren<TrailRenderer>();
            if (trailRenderer != null)
                trailRenderer.emitting = false;
        }

        /// <summary>애니 이벤트: 휘두름 시작 프레임에 호출.</summary>
        public void OnTrailBegin()
        {
            if (trailRenderer != null) trailRenderer.emitting = true;
        }

        /// <summary>애니 이벤트: 휘두름 끝 프레임에 호출.</summary>
        public void OnTrailEnd()
        {
            if (trailRenderer != null) trailRenderer.emitting = false;
        }

        /// <summary>애니 이벤트에서 Start/End 한 번에 처리. 함수명 OnTrail, 인자 "Start" 또는 "End".</summary>
        public void OnTrail(string type)
        {
            if (trailRenderer == null) return;
            if (type == "Start" || type == "Begin")
                trailRenderer.emitting = true;
            else if (type == "End")
                trailRenderer.emitting = false;
        }
    }
}

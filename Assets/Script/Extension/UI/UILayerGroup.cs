using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 이 GameObject와 하위의 모든 GameObject를 지정된 Layer에 등록합니다.
    /// Screen이나 Panel 같은 큰 단위에 붙여서 사용합니다.
    /// </summary>
    public class UILayerGroup : MonoBehaviour
    {
        [SerializeField] private UILayer layer = UILayer.None;
        public UILayer Layer => layer;

        private void Awake()
        {
            if (layer != UILayer.None && UIManager.Shared != null)
            {
                UIManager.Shared.RegisterGroup(this);
            }
        }

        private void OnDestroy()
        {
            if (layer != UILayer.None && UIManager.Shared != null)
            {
                UIManager.Shared.UnregisterGroup(this);
            }
        }
    }
}
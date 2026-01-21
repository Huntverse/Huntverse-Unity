using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Hunt
{

    public class HudStagePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI stageNameText;
        private CinemachineCamera cinCam;
        private void Start()
        {
            UpdateStagePanel(0);
        }
        public void UpdateStagePanel(uint mapId)
        {
            stageNameText.text = BindKeyConst.GetMapNameByMapId(mapId);
            cinCam.Target.TrackingTarget = GameSession.Shared?.LocalPlayer.transform;
        }


    }
}

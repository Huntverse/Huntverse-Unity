using TMPro;
using UnityEngine;
namespace hunt
{
    public class GameChannelField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI channelNameText;
        [SerializeField] private TextMeshProUGUI congestionText;
        [SerializeField] private TextMeshProUGUI myCharCountText;

        // Color 매핑 필요
        private string GetCongestionString(int value)
        {
            return value switch
            {
                0 => "원활",
                1 => "보통",
                2 => "혼잡",
                3 => "포화",
                _ => "보통" // 데이터 누락 및 알수없을 떄
            };
        }

        public void Bind(ChannelModel model)
        {
            channelNameText.text = model.ChannelName;
            congestionText.text = GetCongestionString(model.Congestion);
            myCharCountText.text = model.MyCharacterCount.ToString();
        }
    }
}

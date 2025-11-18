using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hunt
{
    public class GameChannelField : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI channelNameText;
        [SerializeField] private TextMeshProUGUI congestionText;
        [SerializeField] private TextMeshProUGUI myCharCountText;
        [SerializeField] private Button channelButton;

        private ChannelModel channelModel;

        private void Awake()
        {
            if (channelButton != null)
            {
                channelButton.onClick.AddListener(OnChannelClicked);
            }
        }

        private void OnDestroy()
        {
            if (channelButton != null)
            {
                channelButton.onClick.RemoveListener(OnChannelClicked);
            }
        }

        // Color 
        private string GetCongestionString(int value)
        {
            return value switch
            {
                0 => "쾌적",
                1 => "원활",
                2 => "보통",
                3 => "혼잡",
                _ => "보통" 
            };
        }

        public void Bind(ChannelModel model)
        {
            channelModel = model;
            channelNameText.text = model.ChannelName;
            congestionText.text = GetCongestionString(model.Congestion);
            myCharCountText.text = model.MyCharacterCount.ToString();
        }

        private void OnChannelClicked()
        {
            if (channelModel == null) return;
            
            CharacterCreateController.Shared?.UpdateCharacterSlots(channelModel.ChannelName, channelModel.MyCharacterCount);
        }
    }
}

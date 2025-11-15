using UnityEngine;
using UnityEngine.UI;

namespace hunt
{
    [RequireComponent(typeof(Button))]
    public class ButtonAudioEvent : MonoBehaviour
    {
        [SerializeField] private AudioType sfxType = AudioType.SFX_CHANNEL_SELECT;
        [SerializeField] private float volumeScale = 1.0f;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }

        private void OnButtonClick()
        {
            string audioKey = AudioConst.GetSfxKey(sfxType);
            AudioHelper.Shared.PlaySfx(audioKey, volumeScale);
        }
    }
}


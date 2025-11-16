using Cysharp.Threading.Tasks;
using UnityEngine;

namespace hunt
{

    public class MainMenuScreen : MonoBehaviour
    {
        [Header("HUDS")]
        [SerializeField] Canvas mainHud;
        [SerializeField] Canvas characterSelectHud;


        private async void Start()
        {
            await UniTask.WaitUntil(() => AudioHelper.Shared);
            AudioHelper.Shared.PlayBgm(AudioConst.GetSfxKey(AudioType.BGM_MAIN));
            OnViewMainHud();
        }

        public void OnViewCharacterSelectHud()
        {
            if (mainHud.gameObject.activeSelf) mainHud.gameObject.SetActive(false);
            if (!characterSelectHud.gameObject.activeSelf) characterSelectHud.gameObject.SetActive(true);
        }

        public void OnViewMainHud()
        {
            if (characterSelectHud.gameObject.activeSelf) characterSelectHud.gameObject.SetActive(false);
            if (!mainHud.gameObject.activeSelf) mainHud.gameObject.SetActive(true);
        }
        public void EnterChracterSelect()
        {
            if (ButtonClickCountEvent.SelectedOnce)
            {
                OnViewCharacterSelectHud();
            }

        }
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit(); 
#endif
        }

    }
}

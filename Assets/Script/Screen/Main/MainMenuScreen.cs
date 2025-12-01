using Cysharp.Threading.Tasks;
using UnityEngine;

namespace hunt
{

    public class MainMenuScreen : MonoBehaviour
    {
        [Header("HUDS")]
        [SerializeField] GameObject mainHud;
        [SerializeField] GameObject characterSelectHud;

        private async void Start()
        {
            await UniTask.WaitUntil(() => AudioHelper.Shared);
            AudioHelper.Shared.PlayBgm(AudioConst.GetSfxKey(AudioType.BGM_MAIN));
            OnViewMainHud();
        }

        public void OnViewCharacterSelectHud()
        {
            if (mainHud.activeSelf) mainHud.SetActive(false);
            if (!characterSelectHud.activeSelf) characterSelectHud.SetActive(true);
        }

        public void OnViewMainHud()
        {
            if (characterSelectHud.activeSelf) characterSelectHud.SetActive(false);
            if (!mainHud.activeSelf) mainHud.SetActive(true);
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

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace hunt
{

    public class MainMenuScreen : MonoBehaviour
    {
        private async void Start()
        {
            await UniTask.WaitUntil(() => AudioHelper.Shared);
            AudioHelper.Shared.PlayBgm(AudioConst.GetSfxKey(AudioType.BGM_MAIN));
        }


    }
}

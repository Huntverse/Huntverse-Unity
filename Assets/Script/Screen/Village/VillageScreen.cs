using Cysharp.Threading.Tasks;
using Hunt;
using UnityEngine;

public class VillageScreen : MonoBehaviour
{
    private async void Start()
    {
        await UniTask.WaitUntil(() => AudioHelper.Shared);
        AudioHelper.Shared.PlayBgm(AudioKeyConst.GetSfxKey(Hunt.AudioType.BGM_VILLAGE));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

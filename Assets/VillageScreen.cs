using Cysharp.Threading.Tasks;
using hunt;
using UnityEngine;

public class VillageScreen : MonoBehaviour
{
    private async void Start()
    {
        await UniTask.WaitUntil(() => AudioHelper.Shared);
        AudioHelper.Shared.PlayBgm(AudioConst.GetSfxKey(hunt.AudioType.BGM_VILLAGE));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

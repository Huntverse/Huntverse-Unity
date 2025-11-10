using Cysharp.Threading.Tasks;
using hunt;
using UnityEngine;

public class SystemBoot : MonoBehaviourSingleton<SystemBoot>
{
    protected override bool DontDestroy => base.DontDestroy;
    protected override void Awake()
    {
        base.Awake();
        Initialize().Forget();
    }

    bool isInit = false;
    private async UniTaskVoid Initialize()
    {
        Debug.Log($"[Boot] : Initializing...");

        await UniTask.WaitUntil(() => UserAuth.Shared != null);
        Debug.Log($"[Boot] : UserAuth Ready!");

        await UniTask.WaitUntil(() => SteamManager.Initialized);
        Debug.Log($"[Boot] : SteamManager Initialized!");

        UserAuth.Shared.Initialize();

        isInit = true;
        Debug.Log($"[Boot] : Initialize Success");
        if (isInit)
        {
            SceneLoadHelper.Shared?.LoadSceneSingleMode(HuntKeyConst.Ks_Mainmenu);
        }
    }
    private void Start()
    {
      
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}

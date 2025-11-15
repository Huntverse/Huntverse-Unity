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
        $"[Boot] : Initializing...".DLog();

        // ContentsDownloader 대기 및 리소스 다운로드
        await UniTask.WaitUntil(() => ContentsDownloader.Shared != null);
        $"[Boot] : ContentsDownloader Ready!".DLog();
        
        bool downloadSuccess = await ContentsDownloader.Shared.ResourceDownLoad();
        if (!downloadSuccess)
        {
            $"[Boot] : Resource Download Failed!".DError();
            return;
        }
        $"[Boot] : Resource Download Complete!".DLog();

        await UniTask.WaitUntil(() => UserAuth.Shared != null);
        $"[Boot] : UserAuth Ready!".DLog();

        await UniTask.WaitUntil(() => SteamManager.Initialized);
        $"[Boot] : SteamManager Initialized!".DLog();

        UserAuth.Shared.Initialize();

        isInit = true;
        $"[Boot] : Initialize Success".DLog();
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

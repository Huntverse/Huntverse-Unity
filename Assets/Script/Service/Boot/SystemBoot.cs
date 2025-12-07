using System;
using Cysharp.Threading.Tasks;
using Hunt;
using UnityEngine;

public class SystemBoot : MonoBehaviourSingleton<SystemBoot>
{
    [Header("LogIn Window")]
    [SerializeField] private Canvas LogInCanvas;

    public bool isSystemContinue = false;
    protected override bool DontDestroy => base.DontDestroy;
    protected override void Awake()
    {
        base.Awake();
        LogInCanvas.gameObject.SetActive(false);
        Initialize().Forget();
    }

    bool isInit = false;
    private async UniTaskVoid Initialize()
    {
        $"[Boot] : Initializing...".DLog();

        // ContentsDownloader 대기 및 리소스 다운로드
        await UniTask.WaitUntil(() => ContentsDownloader.Shared != null);
        $"[Boot] : ContentsDownloader Ready!".DLog();
        
        bool downloadSuccess = await ContentsDownloader.Shared.StartDownload();
        if (!downloadSuccess)
        {
            $"[Boot] : Resource Download Failed!".DError();
            return;
        }
        $"[Boot] : Resource Download Complete!".DLog();

        await UniTask.WaitUntil(() => UserAuth.Shared != null);
        $"[Boot] : UserAuth Ready!".DLog();

        $"[Boot] : Waiting SteamManager Initialized...".DLog();
        int steamWaitSeconds = 0;
        while (!SteamManager.Initialized)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            steamWaitSeconds++;
            if (steamWaitSeconds % 5 == 0)
            {
                $"[Boot] : SteamManager not ready yet ({steamWaitSeconds}s). SteamAPI_Init 실패 여부 확인 필요".DLog();
            }
        }
        $"[Boot] : SteamManager Initialized after {steamWaitSeconds}s".DLog();

        UserAuth.Shared.Initialize();

        isInit = true;
        $"[Boot] : Initialize Success".DLog();
        if (isInit)
        {
            if (!isSystemContinue)
            {
                ContentsDownloader.Shared.loadingCanvas.gameObject.SetActive(false);
                LogInCanvas.gameObject.SetActive(true);
            }
            else
            {
                SceneLoadHelper.Shared?.LoadSceneSingleMode(ResourceKeyConst.Ks_Mainmenu,false);
            }
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

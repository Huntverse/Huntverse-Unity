using System;
using Cysharp.Threading.Tasks;
using Hunt;
using Hunt.Net;
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
    private async UniTask Initialize()
    {
        $"[Boot] : Initializing...".DLog();

        // Contetns Download
        await UniTask.WaitUntil(() => ContentsDownloader.Shared != null);
        $"[Boot] : ContentsDownloader Ready!".DLog();
        
        bool downloadSuccess = await ContentsDownloader.Shared.StartDownload();
        if (!downloadSuccess)
        {
            $"[Boot] : Resource Download Failed!".DError();
            return;
        }
        $"[Boot] : Resource Download Complete!".DLog();

        // Network Manager
        await UniTask.WaitUntil(() => NetworkManager.Shared != null);
        $"[Boot] : NetworkManger Ready!".DLog();

        // GameSession
        await UniTask.WaitUntil(() => GameSession.Shared != null && GameSession.Shared.IsInitialized);
        $"[Boot] : GameSession Ready!".DLog();

        bool loginServerConnected = await GameSession.Shared.ConnectionToLoginServer();
        if (!loginServerConnected)
        {
            $"[Boot] : LoginServer Connection Fail".DError();
            //return;
        }
        $"[Boot] : LoginServer Connection Success!".DLog();

        // Steam User
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

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }
}

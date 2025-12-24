using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Hunt;
using Hunt.Net;
using UnityEngine;

public class SystemBoot : MonoBehaviourSingleton<SystemBoot>
{
    [Header("LogIn Window")]
    [SerializeField] private Canvas LogInCanvas;

    public bool isSystemContinue = false;
    private bool loginServerConnected;
    public bool LoginServerConnected => loginServerConnected;
    
    protected override bool DontDestroy => base.DontDestroy;
    
    protected override void Awake()
    {
        base.Awake();
        loginServerConnected = false;
        LogInCanvas.gameObject.SetActive(false);
        
        Initialize().Forget();
    }
    private void CleanupInitialization()
    {
        if (initCts != null && !initCts.IsCancellationRequested)
        {
            Debug.Log("[Boot] 초기화 작업 취소 중...");
            try
            {
                initCts.Cancel();
                initCts.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Boot] CancellationTokenSource 정리 중 에러: {e.Message}");
            }
            finally
            {
                initCts = null;
            }
        }
        
        loginServerConnected = false;
        isInitializing = false;
        isInit = false;
        Debug.Log("[Boot] 초기화 상태 리셋 완료");
    }

    private bool isInit = false;
    private bool isInitializing = false;
    private CancellationTokenSource initCts;
    
    private async UniTask Initialize()
    {
        Debug.Log($"[Boot] Initialize 호출됨 - isInitializing: {isInitializing}, isInit: {isInit}");
        
        if (isInitializing)
        {
            Debug.LogWarning("[Boot] 이미 초기화 중입니다. 중복 호출 무시");
            return;
        }
        
        isInitializing = true;
        
        // 기존 CTS가 있으면 정리
        if (initCts != null)
        {
            Debug.Log("[Boot] 기존 CancellationTokenSource 정리");
            try { initCts.Dispose(); } catch { }
            initCts = null;
        }
        
        initCts = new CancellationTokenSource();
        var token = initCts.Token;
        Debug.Log("[Boot] 새 CancellationTokenSource 생성됨");
        
        try
        {
            $"[Boot] : Initializing...".DLog();

            // Contetns Download
            await UniTask.WaitUntil(() => ContentsDownloader.Shared != null, cancellationToken: token);
            $"[Boot] : ContentsDownloader Ready!".DLog();
            
            bool downloadSuccess = await ContentsDownloader.Shared.StartDownload();
            if (!downloadSuccess)
            {
                $"[Boot] : Resource Download Failed!".DError();
                return;
            }
            $"[Boot] : Resource Download Complete!".DLog();

            // Network Manager
            await UniTask.WaitUntil(() => NetworkManager.Shared != null, cancellationToken: token);
            $"[Boot] : NetworkManger Ready!".DLog();

            // GameSession
            await UniTask.WaitUntil(() => GameSession.Shared != null && GameSession.Shared.IsInitialized, cancellationToken: token);
            $"[Boot] : GameSession Ready!".DLog();

            loginServerConnected = await GameSession.Shared.ConnectionToLoginServer();
            if (!loginServerConnected)
            {
                $"[Boot] : LoginServer Connection Fail".DError();
            }
            else
            {
                $"[Boot] : LoginServer Connection Success!".DLog();
            }

            // Steam User
            await UniTask.WaitUntil(() => UserAuth.Shared != null, cancellationToken: token);
            $"[Boot] : UserAuth Ready!".DLog();

            $"[Boot] : Waiting SteamManager Initialized...".DLog();
            int steamWaitSeconds = 0;
            while (!SteamManager.Initialized && !token.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: token);
                steamWaitSeconds++;
                if (steamWaitSeconds % 5 == 0)
                {
                    $"[Boot] : SteamManager not ready yet ({steamWaitSeconds}s). SteamAPI_Init 실패 여부 확인 필요".DLog();
                }
            }
            
            if (token.IsCancellationRequested)
            {
                $"[Boot] : 초기화가 취소되었습니다".DLog();
                return;
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
        catch (OperationCanceledException)
        {
            $"[Boot] : 초기화가 취소되었습니다".DLog();
        }
        catch (Exception e)
        {
            $"[Boot] : 초기화 중 에러 발생: {e.Message}".DError();
        }
        finally
        {
            isInitializing = false;
        }
    }

    protected override void OnDestroy()
    {
        CleanupInitialization();
        
        base.OnDestroy();
    }
}

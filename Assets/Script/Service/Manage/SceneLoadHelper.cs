using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Hunt
{
    /// <summary>
    /// 씬 로딩/언로딩 흐름을 단일 진입점으로 관리하는 Helper.
    /// - 주소 키 기반 싱글/애디티브 로드
    /// - 로딩 캔버스 + 페이드 연출
    /// - CancellationToken을 통한 작업 취소
    /// </summary>
    public class SceneLoadHelper : MonoBehaviourSingleton<SceneLoadHelper>
    {

        private CancellationTokenSource cts;
        private SceneInstance curScene;
        private string curSceneKey;
        private bool deferFadeOut;
        private string lastCancellationReason;

        /// <summary> 현재 Single 로드된 씬 키 (메인=Env 동일 시 재로드 스킵용) </summary>
        public string CurrentSceneKey => curSceneKey;
        /// <summary> 현재 Single 로드된 씬 인스턴스 </summary>
        public SceneInstance CurrentScene => curScene;

        [Header("Loading Indicator")]
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        [SerializeField] private float minLoadingDuration = 0.5f;
        [SerializeField] private float fadeDuration = 0.7f;

        protected override bool DontDestroy => base.DontDestroy;
        protected override void Awake()
        {
            base.Awake();

            if (loadingCanvas != null)
            {
                // 씬 시작 시에는 로딩 캔버스를 항상 비활성/투명 상태로 두고, 필요 시에만 연출을 건다.
                loadingCanvas.gameObject.SetActive(false);
                if (loadingCanvasGroup != null)
                {
                    loadingCanvasGroup.alpha = 0f;
                }
            }
        }

        private void Start()
        {
            cts = new CancellationTokenSource();
        }
    
        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 씬 전환 중 앱 종료 등 수명 종료 시, 진행 중인 비동기 작업을 최대한 안전하게 정리.
            if (cts != null)
            {
                try
                {
                    lastCancellationReason = "OnDestroy(씬/오브젝트 해제)";
                    cts.Cancel();
                    cts.Dispose();
                }
                catch (Exception ex)
                {
                    this.DError($"[SceneLoadHelper] OnDestroy에서 cts 정리 중 에러: {ex.Message}");
                }
                finally
                {
                    cts = null;
                }
            }
        }

        public async UniTask LoadSceneSingleMode(string key, bool isfadeactive = true, bool deferFadeOutUntilManually = false)
        {
            // 단독 로드는 curScene을 관리하는 모드로 공통 코어 사용.
            await LoadSceneCore(
                key,
                LoadSceneMode.Single,
                isfadeactive,
                deferFadeOutUntilManually,
                manageCurrentScene: true,
                logContext: "싱글 씬 로드"
            );
        }

        /// <summary> LoadSceneSingleMode에서 지연시킨 페이드아웃을 수동 완료 </summary>
        public async UniTask CompleteDeferredFadeOut()
        {
            if (!deferFadeOut) return;
            deferFadeOut = false;
            if (loadingCanvasGroup != null && cts != null && !cts.IsCancellationRequested)
            {
                await UIEffect.FadeOut(loadingCanvasGroup, cts.Token, fadeDuration);
            }
            ShowLoadingIndicator(false);
        }


        public async UniTask<SceneInstance> LoadSceneAdditiveMode(string key, bool isfadeactive = true, bool deferFadeOutUntilManually = false)
        {
            // Additive 로드는 curScene을 건드리지 않는 모드로 공통 코어 사용.
            return await LoadSceneCore(
                key,
                LoadSceneMode.Additive,
                isfadeactive,
                deferFadeOutUntilManually,
                manageCurrentScene: false,
                logContext: "Additive 씬 로드"
            );
        }
        public async UniTask UnloadSceneAdditive(SceneInstance scene)
        {
            if (!scene.Scene.IsValid())
                return;

            CancelCurrentOps("Additive 씬 언로드");
            await Addressables.UnloadSceneAsync(scene);
        }

        /// <summary>
        /// 싱글/애디티브 공용 씬 로드 코어.
        /// - 모드에 따라 curScene 관리 여부만 분리하고, 나머지 로딩/페이드/예외 처리는 한 곳에서 처리한다.
        /// </summary>
        private async UniTask<SceneInstance> LoadSceneCore(
            string key,
            LoadSceneMode mode,
            bool isfadeactive,
            bool deferFadeOutUntilManually,
            bool manageCurrentScene,
            string logContext
        )
        {
            // 이전에 진행 중이던 씬 로드/언로딩 작업이 남아있다면 먼저 정리하고, 새로운 토큰으로 교체.
            CancelCurrentOps("새 씬 로드 시작으로 이전 작업 대체");

            float loadStartTime = Time.realtimeSinceStartup;
            deferFadeOut = deferFadeOutUntilManually;

            try
            {
                // 로딩 화면 노출: 필요한 경우에만 페이드 인 연출.
                ShowLoadingIndicator(true);
                if (isfadeactive && loadingCanvasGroup != null)
                {
                    await UIEffect.FadeIn(loadingCanvasGroup, cts.Token, fadeDuration);
                }

                // 싱글 모드에서만 기존 curScene을 Addressables 기준으로 언로드.
                if (manageCurrentScene && curScene.Scene.IsValid())
                {
                    $"[SceneLoadHelper] 기존 씬 언로드 시작: {curScene.Scene.name}".DLog();
                    await Addressables.UnloadSceneAsync(curScene).ToUniTask(cancellationToken: cts.Token);
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token); // 언로드 완료 대기
                    $"[SceneLoadHelper] 기존 씬 언로드 완료".DLog();
                }

                // 새 씬 로드 + 활성화까지 대기.
                $"[SceneLoadHelper] {logContext} 시작: {key}".DLog();
                var handle = Addressables.LoadSceneAsync(key, mode);
                var loadedScene = await handle.ToUniTask(cancellationToken: cts.Token);

                await UniTask.WaitUntil(() => loadedScene.Scene.isLoaded, cancellationToken: cts.Token);
                $"[SceneLoadHelper] {logContext} 완료: {loadedScene.Scene.name}".DLog();

                // 너무 빠른 로딩으로 인해 로딩 화면이 "반짝" 보이는 느낌을 줄이기 위한 최소 노출 시간.
                float elapsedTime = Time.realtimeSinceStartup - loadStartTime;
                if (elapsedTime < minLoadingDuration)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(minLoadingDuration - elapsedTime), cancellationToken: cts.Token);
                }

                if (isfadeactive && !deferFadeOut)
                {
                    if (loadingCanvasGroup != null)
                    {
                        await UIEffect.FadeOut(loadingCanvasGroup, cts.Token, fadeDuration);
                    }
                    ShowLoadingIndicator(false);
                }
                else if (!isfadeactive)
                {
                    ShowLoadingIndicator(false);
                }

                if (manageCurrentScene)
                {
                    curScene = loadedScene;
                    curSceneKey = key;
                }

                return loadedScene;
            }
            catch (OperationCanceledException)
            {
                var reason = string.IsNullOrEmpty(lastCancellationReason) ? "취소 요청됨" : lastCancellationReason;
                this.DError($"[{logContext}] 작업 취소됨 — 사유: {reason}");
                ShowLoadingIndicator(false);
                throw;
            }
            catch (Exception ex)
            {
                this.DError($"[{logContext}] 중 오류 발생: {ex.Message}");
                ShowLoadingIndicator(false);
                throw;
            }
        }

        /// <param name="reason">취소 사유(로그용). null이면 로그에만 기본 문구 사용.</param>
        private void CancelCurrentOps(string reason = null)
        {
            lastCancellationReason = reason;
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
        }


        private void ShowLoadingIndicator(bool show)
        {
            if (loadingCanvas != null)
            {
                loadingCanvas.gameObject.SetActive(show);
            }
        }
        /// <summary> Boot 씬으로 이동 (로그아웃 처리) </summary>
        public async UniTask LoadToLogOut()
        {
            if (cts == null)
            {
                cts = new CancellationTokenSource();
            }
            else
            {
                CancelCurrentOps("로그아웃(Boot 씬 전환)");
            }

            float loadStartTime = Time.realtimeSinceStartup;

            try
            {
                ShowLoadingIndicator(true);
                if (loadingCanvasGroup != null)
                {
                    await UIEffect.FadeIn(loadingCanvasGroup, cts.Token, fadeDuration);
                }

                if (curScene.Scene.IsValid())
                {
                    try
                    {
                        await Addressables.UnloadSceneAsync(curScene).ToUniTask(cancellationToken: cts.Token);
                        await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        this.DError($"씬 언로드 중 에러: {ex.Message}");
                    }
                }

                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    try
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        if (scene.isLoaded && scene.name != "DontDestroyOnLoad")
                        {
                            await SceneManager.UnloadSceneAsync(scene);
                            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.DError($"씬 언로드 중 에러: {ex.Message}");
                    }
                }

                curScene = default;
                curSceneKey = null;
                var loadOp = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

                if (loadOp == null)
                {
                    this.DError("Boot 씬 로드 실패");
                    throw new Exception("Boot 씬 로드 실패");
                }

                while (!loadOp.isDone)
                {
                    if (cts == null || cts.Token.IsCancellationRequested)
                        break;
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token);
                }

                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token);

                if (cts != null && !cts.Token.IsCancellationRequested)
                {
                    float elapsedTime = Time.realtimeSinceStartup - loadStartTime;
                    if (elapsedTime < minLoadingDuration)
                    {
                        await UniTask.Delay(TimeSpan.FromSeconds(minLoadingDuration - elapsedTime), cancellationToken: cts.Token);
                    }

                    if (loadingCanvasGroup != null)
                    {
                        await UIEffect.FadeOut(loadingCanvasGroup, cts.Token, fadeDuration);
                    }
                }

                ShowLoadingIndicator(false);
            }
            catch (OperationCanceledException)
            {
                "[SceneLoadHelper] Boot 씬 로드가 취소되었습니다".DWarnning();
                ShowLoadingIndicator(false);
            }
            catch (Exception ex)
            {
                $"[SceneLoadHelper] Boot 씬 로드 중 오류: {ex.Message}".DError();
                ShowLoadingIndicator(false);

                try
                {
                    SceneManager.LoadScene(0);
                }
                catch (Exception fallbackEx)
                {
                    $"[SceneLoadHelper] 폴백 로드 실패: {fallbackEx.Message}".DError();
                }
            }
        }
    }
}

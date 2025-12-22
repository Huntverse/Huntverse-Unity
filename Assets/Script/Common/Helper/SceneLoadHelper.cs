using Cysharp.Threading.Tasks;
using System;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Hunt
{
    public class SceneLoadHelper : MonoBehaviourSingleton<SceneLoadHelper>
    {
        // async job stop/cancel
        private CancellationTokenSource cts;
        private SceneInstance curScene;

        [Header("Loading Indicator")]
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        [SerializeField] private float minLoadingDuration = 0.5f; // 최소 로딩 표시 시간 (깜빡임 방지)
        [SerializeField] private float fadeDuration = 0.7f; // 페이드 인/아웃 시간

        protected override bool DontDestroy => true;
        protected override void Awake()
        {
            base.Awake();

            if (loadingCanvas != null)
            {
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
            cts.Cancel();
            cts.Dispose();
        }

        public async UniTask LoadSceneSingleMode(string key, bool isfadeactive = true)
        {
            CancelCurrentOps();

            float loadStartTime = Time.realtimeSinceStartup;

            try
            {
                // 1. 페이드 인: 로딩 화면 표시
                ShowLoadingIndicator(true);
                if (isfadeactive)
                {
                    await FadeIn(cts.Token);
                }

                // 2. 기존 씬 언로드
                if (curScene.Scene.IsValid())
                {
                    $"[SceneLoadHelper] 기존 씬 언로드 시작: {curScene.Scene.name}".DLog();
                    await Addressables.UnloadSceneAsync(curScene).ToUniTask(cancellationToken: cts.Token);
                    await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, cts.Token); // 언로드 완료 대기
                    $"[SceneLoadHelper] 기존 씬 언로드 완료".DLog();
                }

                // 3. 새 씬 로드
                $"[SceneLoadHelper] 새 씬 로드 시작: {key}".DLog();
                var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Single);
                curScene = await handle.ToUniTask(cancellationToken: cts.Token);

                // 씬 활성화 대기
                await UniTask.WaitUntil(() => curScene.Scene.isLoaded, cancellationToken: cts.Token);
                $"[SceneLoadHelper] 새 씬 로드 완료: {curScene.Scene.name}".DLog();

                // 최소 로딩 시간 보장 (너무 빠른 전환으로 인한 깜빡임 방지)
                float elapsedTime = Time.realtimeSinceStartup - loadStartTime;
                if (elapsedTime < minLoadingDuration)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(minLoadingDuration - elapsedTime), cancellationToken: cts.Token);
                }

                // 4. 페이드 아웃: 로딩 화면 숨김
                if (isfadeactive)
                {
                    await FadeOut(cts.Token);
                }
                ShowLoadingIndicator(false);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[SceneLoadHelper] 씬 로드가 취소되었습니다.");
                ShowLoadingIndicator(false);
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneLoadHelper] 씬 로드 중 오류 발생: {ex.Message}");
                ShowLoadingIndicator(false);
                throw;
            }
        }

        public async UniTask<SceneInstance> LoadSceneAdditiveMode(string key)
        {
            CancelCurrentOps();

            var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive);
            var scene = await handle.ToUniTask(cancellationToken: cts.Token);
            return scene;
        }

        public async UniTask UnloadSceneAdditive(SceneInstance scene)
        {
            if (!scene.Scene.IsValid())
                return;

            CancelCurrentOps();
            await Addressables.UnloadSceneAsync(scene);
        }

        private void CancelCurrentOps()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
        }

        private void ShowLoadingIndicator(bool show)
        {
            if (loadingCanvas != null)
            {
                loadingCanvas.gameObject.SetActive(show);
                $"[SceneLoadHelper] 로딩 인디케이터 {(show ? "활성화" : "비활성화")}".DLog();
            }

        }

        /// <summary>
        /// 페이드 인: 로딩 화면을 부드럽게 표시
        /// </summary>
        private async UniTask FadeIn(CancellationToken token)
        {
            if (loadingCanvasGroup == null)
            {
                Debug.LogWarning("[SceneLoadHelper] CanvasGroup이 없어 페이드 효과를 적용할 수 없습니다.");
                return;
            }


            float elapsed = 0f;
            loadingCanvasGroup.alpha = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Time.timeScale 영향 받지 않음
                loadingCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            loadingCanvasGroup.alpha = 1f;

        }

        /// <summary>
        /// 페이드 아웃: 로딩 화면을 부드럽게 숨김
        /// </summary>
        private async UniTask FadeOut(CancellationToken token)
        {
            if (loadingCanvasGroup == null)
            {

                return;
            }


            float elapsed = 0f;
            loadingCanvasGroup.alpha = 1f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Time.timeScale 영향 받지 않음
                loadingCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            loadingCanvasGroup.alpha = 0f;
        }
    }
}

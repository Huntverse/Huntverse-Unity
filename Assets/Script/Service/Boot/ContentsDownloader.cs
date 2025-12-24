using Cysharp.Threading.Tasks;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;   
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hunt
{
    public class ContentsDownloader : MonoBehaviourSingleton<ContentsDownloader>
    {
        public Canvas loadingCanvas;
        private LoadingIndicator loadingIndicator;
        private string envConfigFileName = "env_contents.json";

        public float DownloadProgress { get; private set; }

        private CcdEnvConfig cachedConfig;
        private bool envConfigLoadAttempted;

        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            if (loadingCanvas != null)
            {
                loadingIndicator = loadingCanvas.GetComponent<LoadingIndicator>();
                UpdateLoadingUI(0f);
            }
            base.Awake();
        }

        /// <summary>
        /// 외부에서 호출하는 진입점
        /// </summary>
        public async UniTask<bool> StartDownload()
        {
            try
            {
                "📦 [Downloader] Start!!".DLog();

                var config = LoadEnvConfig();
                if (config == null)
                {
                    "📦 [Downloader] Env config Load Fail".DError();
                    return false;
                }
                UpdateLoadingUI(0f);

                if (string.IsNullOrWhiteSpace(config.remoteCatalogUrl))
                {
                    "📦 [Downloader] remoteCatalogUrl missing (env_contents.json)".DError();
                    return false;
                }

                if (string.IsNullOrWhiteSpace(config.downloadLabel))
                {
                    "📦 [Downloader] downloadLabel missing (env_contents.json)".DError();
                    return false;
                }

                // 0. CCD 런타임 프로퍼티 세팅 (RemoteLoadPath 안의 {CcdManager.*} 치환용)
                ApplyCcdRuntimeProperties(config);
                UpdateLoadingUI(0.1f);

                // 1. Remote 카탈로그 로드
                if (!await LoadRemoteCatalog(config.remoteCatalogUrl))
                    return false;
                UpdateLoadingUI(0.2f);

                // 2. Catalog 업데이트
                if (!await UpdateCatalog())
                    return false;
                UpdateLoadingUI(0.3f);

                // 3. Addressables 다운로드 (라벨 기준 -> default 라벨을 가지고있어야만 다운로드가 가능한 에셋)
                if (!await DownloadAddressablesByLabel(config.downloadLabel))
                    return false;
                UpdateLoadingUI(1f);

                "📦 [Downloader] All Complete!".DLog();
                return true;
            }
            catch (Exception e)
            {
                $"📦 [Downloader] ERROR: {e}".DError();
                return false;
            }
        }

        #region Catalog

        private async UniTask<bool> LoadRemoteCatalog(string catalogUrl)
        {
            if (string.IsNullOrWhiteSpace(catalogUrl))
            {
                "📦 [Downloader] remoteCatalogUrl missing (env_contents.json)".DError();
                return false;
            }

            // 절대 URL인지 확인하고, 상대 경로라면 절대 URL로 변환
            string absoluteCatalogUrl = catalogUrl;
            if (!Uri.IsWellFormedUriString(catalogUrl, UriKind.Absolute))
            {
                // 상대 경로인 경우, env_contents.json의 remoteCatalogUrl을 그대로 사용
                // 하지만 Addressables가 Profile의 Remote.LoadPath를 사용하지 않도록 절대 URL로 만들어야 함
                // catalogUrl이 이미 전체 URL이어야 하므로, 그대로 사용
                absoluteCatalogUrl = catalogUrl;
            }

            $"📦 [Downloader] Loading catalog from: {absoluteCatalogUrl}".DLog();
            var catalogHandle = Addressables.LoadContentCatalogAsync(absoluteCatalogUrl, true);
            await catalogHandle.Task;

            if (!catalogHandle.IsValid() || catalogHandle.Status != AsyncOperationStatus.Succeeded)
            {
                string errorMsg = catalogHandle.IsValid() ? catalogHandle.OperationException?.ToString() : "Invalid operation handle";
                $"📦 [Downloader] Failed to load catalog - {errorMsg}".DError();
                if (catalogHandle.IsValid())
                {
                    Addressables.Release(catalogHandle);
                }
                return false;
            }
            Addressables.Release(catalogHandle);
            return true;
        }

        private async UniTask<bool> UpdateCatalog()
        {
            "📦 [Downloader] Checking catalog updates...".DLog();

            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle.Task;

            if (checkHandle.Status != AsyncOperationStatus.Succeeded)
            {
                $"📦 [Downloader] Catalog check failed : {checkHandle.OperationException}".DError();
                
                Addressables.Release(checkHandle);
                return false;
            }

            var catalogs = checkHandle.Result;
            Addressables.Release(checkHandle);

            if (catalogs == null)
            {
                "📦 [Downloader] Catalog list is null.".DError();
                return false;
            }

            if (catalogs.Count == 0)
            {
                "📦 [Downloader] Already catalog updates".DLog();
                return true;
            }

            $"📦 [Downloader] Found {catalogs.Count} catalog updates".DLog();

            var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
            await updateHandle.Task;

            if (updateHandle.Status != AsyncOperationStatus.Succeeded)
            {
                $"📦 [Downloader] Catalog update failed : {updateHandle.OperationException}".DError();
                Addressables.Release(updateHandle);
                return false;
            }

            "📦 [Downloader] Catalog update success".DLog();
            Addressables.Release(updateHandle);

            return true;
        }

        #endregion

        #region Download

        private async UniTask<bool> DownloadAddressablesByLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                "📦 [Downloader] downloadLabel 비어있음".DError();
                return false;
            }

            $"📦 [Downloader] Calc download size for label: {label}".DLog();

            var sizeHandle = Addressables.GetDownloadSizeAsync(label);
            await sizeHandle.Task;

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                $"📦 [Downloader] GetDownloadSize failed for label: {label} - {sizeHandle.OperationException}".DError();
                
                Addressables.Release(sizeHandle);
                return false;
            }

            long size = sizeHandle.Result;
            Addressables.Release(sizeHandle);

            if (size <= 0)
            {
                $"📦 [Downloader] No download needed for label '{label}'.".DLog();
                return true;
            }

            $"📦 [Downloader] Download size for '{label}': {size / (1024f * 1024f):F2} MB".DLog();

            var downloadHandle = Addressables.DownloadDependenciesAsync(label, true);

            while (!downloadHandle.IsDone)
            {
                DownloadProgress = downloadHandle.PercentComplete;
                UpdateLoadingUI(DownloadProgress);
                await UniTask.Yield();
            }

            if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                $"📦 [Downloader] Download FAILED for label '{label}' - {downloadHandle.OperationException}".DError();
                Addressables.Release(downloadHandle);
                return false;
            }

            $"📦 [Downloader] Download Complete for '{label}'".DLog();
            Addressables.Release(downloadHandle);
            UpdateLoadingUI(1f);
            return true;
        }

        #endregion

        #region CCD Runtime Properties

        private void ApplyCcdRuntimeProperties(CcdEnvConfig config = null)
        {
            config ??= LoadEnvConfig();
            if (config == null)
            {
                Debug.LogError("📦 [Downloader] Env config not found or invalid. Unable to set CCD runtime properties.");
                return;
            }

            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.EnvironmentId", config.environmentId);
            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.EnvironmentName", config.environmentName);
            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.BucketId", config.bucketId);
            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.BucketName", config.bucketName);
            AddressablesRuntimeProperties.SetPropertyValue("CcdManager.Badge", config.badge);

            "CCD Runtime Properties Set:".DLog();
            $"Env   = {AddressablesRuntimeProperties.EvaluateString("{CcdManager.EnvironmentName}")}".DLog();
            $"Bucket= {AddressablesRuntimeProperties.EvaluateString("{CcdManager.BucketId}")}".DLog();
            $"Badge = {AddressablesRuntimeProperties.EvaluateString("{CcdManager.Badge}")}".DLog();
        }

        private CcdEnvConfig LoadEnvConfig()
        {
            if (cachedConfig != null || envConfigLoadAttempted)
                return cachedConfig;

            envConfigLoadAttempted = true;

            if (string.IsNullOrWhiteSpace(envConfigFileName))
            {
                "📦 [Downloader] Env config filename is empty. Skipping config load.".DError();
                return null;
            }

            string configPath = Path.Combine(Application.streamingAssetsPath, "aa",envConfigFileName);

            if (!File.Exists(configPath))
            {
                $"📦 [Downloader] Env config not found at {configPath}".DError();
                return null;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                cachedConfig = JsonUtility.FromJson<CcdEnvConfig>(json);
                if (cachedConfig == null)
                {
                    $"📦 [Downloader] Failed to parse env config at {configPath}".DError();
                }
                else
                {
                    $"📦 [Downloader] Env config loaded from {configPath}".DLog();
                }
            }
            catch (Exception e)
            {
                $"📦 [Downloader] Failed to read env config. Path: {configPath}, Error: {e.Message}".DError();
            }

            return cachedConfig;
        }

        private void UpdateLoadingUI(float normalizedValue)
        {
            loadingIndicator?.UpdateProgress(Mathf.Clamp01(normalizedValue));
        }

        [Serializable]
        private class CcdEnvConfig
        {
            public string environmentId;
            public string environmentName;
            public string bucketId;
            public string bucketName;
            public string badge;
            public string remoteCatalogUrl;
            public string downloadLabel;
        }

        #endregion
    }
}

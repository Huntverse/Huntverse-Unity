using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace hunt
{
    public class ContentsDownloader : MonoBehaviourSingleton<ContentsDownloader>
    {
        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            base.Awake();
        }

        public async UniTask<bool> ResourceDownLoad()
        {
            try
            {
                "📦 [ContentsDownloader] Addressables Initialize Start...".DLog();
                
                // 1. Addressables 초기화
                var initHandle = Addressables.InitializeAsync();
                
                while (!initHandle.IsDone)
                {
                    await UniTask.Yield();
                }
                
                if (initHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    $"📦 [ContentsDownloader] Addressables Initialize Failed! Status: {initHandle.Status}".DError();
                    if (initHandle.OperationException != null)
                    {
                        $"📦 [ContentsDownloader] Exception: {initHandle.OperationException.Message}".DError();
                    }
                    return false;
                }
                
                "📦 [ContentsDownloader] Addressables Initialize Success!".DLog();

                // 2. 카탈로그 업데이트 확인
                "📦 [ContentsDownloader] Checking for catalog updates...".DLog();
                var checkHandle = Addressables.CheckForCatalogUpdates(false);
                
                while (!checkHandle.IsDone)
                {
                    await UniTask.Yield();
                }

                if (checkHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    List<string> catalogs = checkHandle.Result;
                    
                    if (catalogs != null && catalogs.Count > 0)
                    {
                        $"📦 [ContentsDownloader] Found {catalogs.Count} catalog updates".DLog();
                        
                        // 3. 카탈로그 업데이트
                        var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                        
                        while (!updateHandle.IsDone)
                        {
                            await UniTask.Yield();
                        }
                        
                        if (updateHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            "📦 [ContentsDownloader] Catalog update success!".DLog();
                        }
                        else
                        {
                            $"📦 [ContentsDownloader] Catalog update failed! Status: {updateHandle.Status}".DError();
                        }
                        
                        Addressables.Release(updateHandle);
                    }
                    else
                    {
                        "📦 [ContentsDownloader] No catalog updates available".DLog();
                    }
                }
                else
                {
                    $"📦 [ContentsDownloader] Catalog check failed! Status: {checkHandle.Status}".DError();
                }
                
                Addressables.Release(checkHandle);

                // 4. 모든 리소스 다운로드 사이즈 확인 및 다운로드
                "📦 [ContentsDownloader] Checking all resource locations...".DLog();
                
                // 모든 locator의 키를 가져오기
                var locators = Addressables.ResourceLocators;
                var locatorList = locators.ToList();
                $"📦 [ContentsDownloader] Found {locatorList.Count} locator(s)".DLog();
                
                long totalDownloadSize = 0;
                List<object> keysToDownload = new List<object>();
                int totalKeyCount = 0;

                foreach (var locator in locatorList)
                {
                    var keysList = locator.Keys.ToList();
                    $"📦 [ContentsDownloader] Locator: {locator} has {keysList.Count} keys".DLog();
                    
                    foreach (var key in keysList)
                    {
                        totalKeyCount++;
                        $"📦 [ContentsDownloader] Checking key [{totalKeyCount}]: {key}".DLog();
                        
                        var sizeHandle = Addressables.GetDownloadSizeAsync(key);
                        
                        while (!sizeHandle.IsDone)
                        {
                            await UniTask.Yield();
                        }

                        if (sizeHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            if (sizeHandle.Result > 0)
                            {
                                float sizeMB = sizeHandle.Result / (1024f * 1024f);
                                totalDownloadSize += sizeHandle.Result;
                                keysToDownload.Add(key);
                                $"📦 [ContentsDownloader] ✅ Need download: {key} ({sizeMB:F2} MB)".DLog();
                            }
                            else
                            {
                                $"📦 [ContentsDownloader] ✓ Already cached: {key}".DLog();
                            }
                        }
                        else
                        {
                            $"📦 [ContentsDownloader] ❌ Check failed: {key} - Status: {sizeHandle.Status}".DError();
                        }

                        Addressables.Release(sizeHandle);
                    }
                }

                $"📦 [ContentsDownloader] Total keys checked: {totalKeyCount}".DLog();
                
                if (totalDownloadSize > 0)
                {
                    float sizeMB = totalDownloadSize / (1024f * 1024f);
                    $"📦 [ContentsDownloader] ========================================".DLog();
                    $"📦 [ContentsDownloader] Total download size: {sizeMB:F2} MB".DLog();
                    $"📦 [ContentsDownloader] Resources to download: {keysToDownload.Count}".DLog();
                    $"📦 [ContentsDownloader] ========================================".DLog();
                    
                    // 5. 모든 리소스 다운로드
                    $"📦 [ContentsDownloader] Target Path: {Application.persistentDataPath}".DLog();
                    "📦 [ContentsDownloader] Starting download...".DLog();

                    int currentIndex = 0;
                    foreach (var key in keysToDownload)
                    {
                        currentIndex++;
                        $"📦 [ContentsDownloader] [{currentIndex}/{keysToDownload.Count}] Downloading: {key}".DLog();
                        
                        var downloadHandle = Addressables.DownloadDependenciesAsync(key);
                        
                        float lastProgress = 0f;
                        while (!downloadHandle.IsDone)
                        {
                            float progress = downloadHandle.PercentComplete;
                            if (progress - lastProgress >= 0.1f) // 10%마다 로그
                            {
                                $"📦 [ContentsDownloader]    Progress: {progress * 100:F1}%".DLog();
                                lastProgress = progress;
                            }
                            await UniTask.Yield();
                        }
                        
                        if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                        {
                            $"📦 [ContentsDownloader] ✅ [{currentIndex}/{keysToDownload.Count}] Complete: {key}".DLog();
                        }
                        else
                        {
                            $"📦 [ContentsDownloader] ❌ [{currentIndex}/{keysToDownload.Count}] Failed: {key} - Status: {downloadHandle.Status}".DError();
                        }
                        
                        Addressables.Release(downloadHandle);
                    }

                    "📦 [ContentsDownloader] ========================================".DLog();
                    $"📦 [ContentsDownloader] All {keysToDownload.Count} resources downloaded!".DLog();
                    "📦 [ContentsDownloader] ========================================".DLog();
                }
                else
                {
                    "📦 [ContentsDownloader] ========================================".DLog();
                    "📦 [ContentsDownloader] No resources to download".DLog();
                    "📦 [ContentsDownloader] All resources already cached in PersistentDataPath".DLog();
                    "📦 [ContentsDownloader] ========================================".DLog();
                }
                
                "📦 [ContentsDownloader] Resource download complete!".DLog();
                return true;
            }
            catch (Exception ex)
            {
                $"📦 [ContentsDownloader] Error: {ex.Message}".DError();
                $"📦 [ContentsDownloader] StackTrace: {ex.StackTrace}".DError();
                return false;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
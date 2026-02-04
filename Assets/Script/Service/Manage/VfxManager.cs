using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Hunt
{
    /// <summary>한 구간(정규화 시간 start~end)에 스폰할 VFX 키. 메소드 인자로 전달.</summary>
    [Serializable]
    public class VfxSpan
    {
        public float startNormalized;
        public float endNormalized;
        public string vfxKey;
    }

    [Serializable]
    public class ClipVfxEntry
    {
        public string clipName;
        public List<VfxSpan> spans = new List<VfxSpan>();
    }

    public class VfxManager : MonoBehaviourSingleton<VfxManager>
    {
        [Header("VFX SETTINGS")]
        [SerializeField] private int maxVfxPoolCount = 500;
        [Header("VFX 오버라이드 (선택): 클립 이벤트 대신 수동 지정")]
        [SerializeField] private List<ClipVfxEntry> clipVfxOverrides = new List<ClipVfxEntry>();
        
        /// <summary>런타임 캐싱: 클립 이름 → VFX 구간. 클립 이벤트 자동 읽기 + 오버라이드 적용.</summary>
        private Dictionary<string, List<VfxSpan>> _clipSpansCache = new Dictionary<string, List<VfxSpan>>(StringComparer.OrdinalIgnoreCase);

        // 키별 프리팹 캐시
        private readonly Dictionary<string, GameObject> prefabCache = new();
        private readonly Dictionary<string, VfxObject> vfxObjectPrefabs = new();
        // 키별 프리팹의 원본 scale 저장
        private readonly Dictionary<string, Vector3> prefabOriginalScales = new();
        
        // 키별 독립적인 풀 관리 (프리팹별로 구분)
        private readonly Dictionary<string, ObjectPool<VfxObject>> pools = new();
        
        protected override bool DontDestroy => true;

        #region 프리팹 로드

        private async UniTask<VfxObject> GetOrLoadVfxObject(string key)
        {
            if(vfxObjectPrefabs.TryGetValue(key, out var cachedPrefab))
            {
                return cachedPrefab;
            }

            var bundleKey = key.ToLower();
            var prefab = await AbLoader.Shared.LoadAssetAsync<GameObject>(bundleKey);

            if (prefab == null)
            {
                $"🎆 [VfxManager] 프리팹 로드 실패: {key}".DError();
                return null;
            }

            prefabCache[key] = prefab;
            
            // 프리팹의 원본 scale 저장
            prefabOriginalScales[key] = prefab.transform.localScale;

            var vfxObj = prefab.GetComponent<VfxObject>();
            if (vfxObj == null)
            {
                vfxObj = prefab.AddComponent<VfxObject>();
            }

            vfxObjectPrefabs[key] = vfxObj;
            return vfxObj;
        }

        #endregion

        #region 풀 관리

        private ObjectPool<VfxObject> GetPool(string key, VfxObject prefab)
        {
            if (!pools.TryGetValue(key, out var pool))
            {
                pool = new ObjectPool<VfxObject>(
                    createFunc: () => CreatePooledItem(prefab, key),
                    actionOnGet: (obj) => 
                    {
                        // 원본 scale로 복원
                        if (prefabOriginalScales.TryGetValue(key, out var originalScale))
                        {
                            obj.transform.localScale = originalScale;
                        }
                        obj.gameObject.SetActive(true);
                    },
                    actionOnRelease: (obj) => 
                    {
                        // 모든 자식 객체 포함하여 비활성화
                        SetActiveRecursively(obj.gameObject, false);
                        obj.transform.SetParent(this.transform);
                        // 원본 scale로 복원
                        if (prefabOriginalScales.TryGetValue(key, out var originalScale))
                        {
                            obj.transform.localScale = originalScale;
                        }
                    },
                    actionOnDestroy: (obj) => Destroy(obj.gameObject),
                    collectionCheck: true,
                    defaultCapacity: 10,
                    maxSize: maxVfxPoolCount
                );
                pools[key] = pool;
            }
            
            return pool;
        }

        private VfxObject CreatePooledItem(VfxObject prefab, string key)
        {
            var instance = Instantiate(prefab);
            instance.transform.SetParent(this.transform);
            // 모든 자식 객체 포함하여 비활성화
            SetActiveRecursively(instance.gameObject, false);
            return instance;
        }

        #endregion

        #region Public API

        public async UniTask PreloadAsync(string key, int preloadCount = 0)
        {
            var vfxObj = await GetOrLoadVfxObject(key);
            if (vfxObj == null)
            {
                return;
            }
            
            var pool = GetPool(key, vfxObj);
            
            if (preloadCount > 0)
            {
                var preloadList = new List<VfxObject>();
                for(int i = 0; i < preloadCount; i++)
                {
                    var instance = pool.Get();
                    preloadList.Add(instance);
                }

                foreach(var instance in preloadList)
                {
                    pool.Release(instance);
                }

            }
            else
            {
            }
        }

        public async UniTask<VfxHandle> PlayOneShot(string key, Vector3 pos, Quaternion rot, Transform parent = null, Vector3? scale = null)
        {
            var vfxObj = await GetOrLoadVfxObject(key);
            if (vfxObj == null)
            {
                $"🎆 [VfxManager] PlayOneShot 실패 - VfxObject 없음: {key}".DError();
                return null;
            }

            var pool = GetPool(key, vfxObj);
            var vfxInstance = pool.Get();
            
            if (vfxInstance == null)
            {
                $"🎆 [VfxManager] PlayOneShot 실패 - 풀에서 인스턴스 가져오기 실패: {key}".DError();
                return null;
            }

            var spawnPos = vfxObj.SpawnPosition;
            var spawnOffset = new Vector3(spawnPos.x, spawnPos.y, spawnPos.z);

            if (parent != null)
            {
                vfxInstance.transform.SetParent(parent);
                vfxInstance.transform.localPosition = spawnOffset;
                vfxInstance.transform.localRotation = Quaternion.Inverse(parent.rotation) * rot;
            }
            else
            {
                vfxInstance.transform.position = pos + rot * spawnOffset;
                vfxInstance.transform.rotation = rot;
            }
            
            if (prefabOriginalScales.TryGetValue(key, out var originalScale))
            {
                var finalScale = originalScale;
                if (scale.HasValue)
                    finalScale.x = scale.Value.x;
                vfxInstance.transform.localScale = finalScale;
            }
            else
            {
                var finalScale = scale.HasValue ? new Vector3(scale.Value.x, 1f, 1f) : Vector3.one;
                vfxInstance.transform.localScale = finalScale;
            }

            vfxInstance.Init(() =>
            {
                pool.Release(vfxInstance);
            });

            return new VfxHandle(vfxInstance);
        }

        /// <summary>특정 구간(key, startTime, endTime) 스폰. start/end는 호출 시 인자로 전달.</summary>
        public async UniTask<VfxHandle> PlayOneShot(string key, Vector3 pos, Quaternion rot, Transform parent, float startTime, float endTime)
        {
            return await PlayOneShot(key, pos, rot, parent);
        }

        /// <summary>현재 재생 중인 클립의 VFX 구간. 런타임 캐싱 + 클립 이벤트 자동 읽기.</summary>
        public List<VfxSpan> GetSpansForCurrentClip(Animator animator)
        {
            if (animator == null) return null;
            var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo == null || clipInfo.Length == 0) return null;
            var clip = clipInfo[0].clip;
            if (clip == null) return null;
            
            return GetSpansForClip(clip);
        }

        /// <summary>클립명으로 VFX 구간 조회. 오버라이드 우선 → 캐시 확인 → 클립 이벤트 자동 읽기 + 캐싱.</summary>
        public List<VfxSpan> GetSpansForClip(AnimationClip clip)
        {
            if (clip == null) return null;
            string clipName = clip.name;
            
            // 1. 오버라이드 확인 (Inspector에서 수동 지정한 것)
            if (clipVfxOverrides != null)
            {
                foreach (var e in clipVfxOverrides)
                {
                    if (string.Equals(e.clipName, clipName, System.StringComparison.OrdinalIgnoreCase) && e.spans != null && e.spans.Count > 0)
                        return e.spans;
                }
            }
            
            // 2. 캐시 확인
            if (_clipSpansCache.TryGetValue(clipName, out var cached))
                return cached;
            
            // 3. 클립 이벤트 자동 읽기 + 캐싱
            var spans = ReadSpansFromClipEvents(clip);
            _clipSpansCache[clipName] = spans ?? new List<VfxSpan>();
            return spans;
        }

        /// <summary>클립 이벤트 자동 읽기. 함수명 "VfxSpawn", stringParameter=vfxKey, time=초 → 정규화 시간.</summary>
        private List<VfxSpan> ReadSpansFromClipEvents(AnimationClip clip)
        {
            if (clip == null || clip.events == null || clip.events.Length == 0) return null;
            float length = clip.length;
            if (length <= 0f) return null;
            
            var list = new List<VfxSpan>();
            foreach (var ev in clip.events)
            {
                if (ev.functionName != "VfxSpawn" && ev.functionName != "OnVfxSpawn") continue;
                if (string.IsNullOrEmpty(ev.stringParameter)) continue;
                
                float normalized = ev.time / length;
                list.Add(new VfxSpan 
                { 
                    startNormalized = normalized, 
                    endNormalized = normalized, 
                    vfxKey = ev.stringParameter 
                });
            }
            return list.Count > 0 ? list : null;
        }

        /// <summary>캐시 초기화. 클립 변경 시 다시 읽도록.</summary>
        public void ClearClipCache()
        {
            _clipSpansCache.Clear();
        }

        public void Release(string key)
        {
            if(prefabCache.TryGetValue(key, out var prefab))
            {
                // 풀 정리
                if (pools.TryGetValue(key, out var pool))
                {
                    pools.Remove(key);
                }
                
                AbLoader.Shared.ReleaseAsset(key.ToLower());
                prefabCache.Remove(key);
                vfxObjectPrefabs.Remove(key);
                prefabOriginalScales.Remove(key);
            }
        }

        public void ReleaseAll()
        {
            var keys = new List<string>(prefabCache.Keys);
            foreach (var key in keys)
            {
                Release(key);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 재귀적으로 GameObject와 모든 자식 객체의 활성화 상태를 설정
        /// </summary>
        private void SetActiveRecursively(GameObject obj, bool active)
        {
            if (obj == null) return;
            
            obj.SetActive(active);
            
            // 모든 자식 객체도 재귀적으로 처리
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                SetActiveRecursively(obj.transform.GetChild(i).gameObject, active);
            }
        }

        #endregion
    }
}

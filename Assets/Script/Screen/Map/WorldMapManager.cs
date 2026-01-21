using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hunt
{
    /// <summary> 맵 Env 로드 및 전환 관리 </summary>
    public class WorldMapManager : MonoBehaviourSingleton<WorldMapManager>
    {
        private FieldTransitionInfo? currentTransition;
        
        protected override bool DontDestroy => true;

        /// <summary> 맵 Env 로드 </summary>
        public async UniTask<GameObject> LoadMapEnv(Transform envRoot, uint mapId, SceneType sceneType)
        {
            this.DLog($"MapId : {mapId} , SceneType : {sceneType}");
            if (envRoot == null)
            {
                "[WorldMapManager] EnvRoot가 null".DError();
                return null;
            }

           
            foreach (Transform child in envRoot)
            {
                Destroy(child.gameObject);
            }
            await UniTask.Yield();

       
            string envKey = GetEnvKey(mapId, sceneType);
            var envPrefab = await AbLoader.Shared.LoadInstantiateAsync(envKey);
            envPrefab.SetActive(true);
            envPrefab.transform.SetParent(envRoot);
            if (envPrefab == null)
            {
                $"[WorldMapManager] Env 로드 실패: {envKey}".DError();
                return null;
            }

            $"[WorldMapManager] 맵 Env 로드 완료: {mapId}".DLog();
            
            return envPrefab;
        }

        /// <summary> 씬 타입별 Env 키 생성 </summary>
        private string GetEnvKey(uint mapId, SceneType sceneType)
        {
            return sceneType switch
            {
                SceneType.FieldDungeon => $"fielddungeon_{mapId}@scene_env",
                SceneType.Village => $"village_{mapId}@scene_env",
                SceneType.Town => $"town_{mapId}@scene_env",
                _ => $"map_{mapId}@scene_env"
            };
        }

        #region Field Transition

        /// <summary> 필드 전환 정보 저장 </summary>
        public void SetTransitionInfo(FieldTransitionInfo info)
        {
            currentTransition = info;
            $"[WorldMapManager] 전환 정보 저장: {info.entryDirection} → {info.spawnDirection}".DLog();
        }

        /// <summary> 전환 정보 가져오고 초기화 </summary>
        public FieldTransitionInfo? GetAndClearTransitionInfo()
        {
            var info = currentTransition;
            currentTransition = null;
            return info;
        }

        /// <summary> 전환 정보 존재 여부 </summary>
        public bool HasTransitionInfo => currentTransition.HasValue;

        #endregion
    }
}

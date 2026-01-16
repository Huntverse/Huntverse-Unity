using Cysharp.Threading.Tasks;
using Hunt.Common;
using Unity.Cinemachine;
using UnityEngine;

namespace Hunt
{
    /// <summary> 필드 던전 씬 관리 </summary>
    public class FieldDungeonScreen : MonoBehaviour
    {
        [SerializeField] private Transform envRoot;
        [SerializeField] private CinemachineCamera cinemaCam;
        private void Awake()
        {
            InGameService.OnMapChangeResponse += OnMapChangeResponse;
        }

        private void OnDestroy()
        {
            InGameService.OnMapChangeResponse -= OnMapChangeResponse;
        }

        private async void Start()
        {
            await UniTask.WaitUntil(() => AudioManager.Shared);
            AudioManager.Shared.PlayBgm(AudioKeyConst.GetSfxKey(AudioType.BGM_FIELD));
            
            uint currentMapId = GameSession.Shared?.CurrentMapId ?? 0;
            if (currentMapId == 0 || currentMapId < 27000 || currentMapId >= 28000)
            {
                currentMapId = 27001;
            }
            
            Vector3 initialPos =  Vector3.zero;
            var player = await GameSession.Shared.SpawnLocalPlayer(initialPos);
            
            if (player != null && cinemaCam != null)
            {
                cinemaCam.Target.TrackingTarget = player.transform;
            }
            else
            {
                this.DError("플레이어 스폰 실패 또는 카메라 없음.");
                return;
            }
            await WorldMapManager.Shared.LoadMapEnv(envRoot, currentMapId, SceneType.FieldDungeon);
            GameSession.Shared?.SetCurrentMap(currentMapId);
            
            RefreshHUD();
            
            PositionPlayerAtPortal();
        }

        /// <summary> 서버 맵 변경 응답 처리 </summary>
        private void OnMapChangeResponse(ErrorType errorType, uint newMapId)
        {
            if (errorType != ErrorType.ErrNon)
            {
                $"[FieldDungeonScreen] 맵 변경 실패: {errorType}".DError();
                return;
            }
            
            $"[FieldDungeonScreen] 맵 변경 승인: {newMapId}".DLog();
            
            // 씬 타입 확인
            var targetSceneType = GameSession.Shared.GetSceneTypeByMapId(newMapId);
            
            if (targetSceneType == SceneType.FieldDungeon)
            {
                // 같은 씬 타입 -> Env만 교체
                LoadNewEnv(newMapId).Forget();
            }
            else
            {
                // 다른 씬 타입 -> 씬 전환
                LoadNewScene(newMapId).Forget();
            }
        }

        /// <summary> HUD 강제 업데이트 </summary>
        private void RefreshHUD()
        {
            if (InGameHud.Shared != null)
            {
                var panels = new MonoBehaviour[] 
                { 
                    InGameHud.Shared.SettingPanel,
                    InGameHud.Shared.CharStatPanel,
                    InGameHud.Shared.CharInventoryPanel
                };

                foreach (var panel in panels)
                {
                    if (panel != null && panel.gameObject.activeSelf)
                    {
                        panel.gameObject.SetActive(false);
                        panel.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary> 플레이어를 포털 위치에 배치 </summary>
        private void PositionPlayerAtPortal()
        {
            var transitionInfo = WorldMapManager.Shared?.GetAndClearTransitionInfo();
            
            if (transitionInfo.HasValue)
            {
                $"[FieldDungeonScreen] 포털 위치로 이동: {transitionInfo.Value.spawnDirection}".DLog();
                GameSession.Shared?.MovePlayerToPortal(transitionInfo.Value.spawnDirection);
            }
        }

        /// <summary> 같은 씬 내에서 Env만 교체 </summary>
        private async UniTaskVoid LoadNewEnv(uint mapId)
        {
            await WorldMapManager.Shared.LoadMapEnv(envRoot, mapId, SceneType.FieldDungeon);
            GameSession.Shared?.SetCurrentMap(mapId);
            
            // 포털 위치로 이동
            PositionPlayerAtPortal();
        }

        /// <summary> 다른 씬으로 전환 (Village 등) </summary>
        private async UniTaskVoid LoadNewScene(uint mapId)
        {
            string sceneName = GameSession.Shared.GetSceneNameByMapId(mapId);
            await SceneLoadHelper.Shared.LoadSceneSingleMode(sceneName, isfadeactive: true);
            GameSession.Shared?.SetCurrentMap(mapId);
        }
    }
}


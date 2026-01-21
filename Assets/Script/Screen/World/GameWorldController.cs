using System.Collections.Generic;
using UnityEngine;
namespace Hunt
{
    public class GameWorldController : MonoBehaviourSingleton<GameWorldController>
    {
        [Header("Channel Field")]
        [SerializeField] private List<GameWorldField> gameWorldFields;

        protected override bool DontDestroy => false;
        
        protected override void Awake()
        {
            base.Awake();
            
            if (gameWorldFields == null || gameWorldFields.Count == 0)
            {
                $"[GameWorldController] ❌ gameChannelFields가 null이거나 비어있습니다! Inspector에서 할당하세요.".DError();
            }
            else
            {
                $"[GameWorldController] ✅ Awake - gameChannelFields 개수: {gameWorldFields.Count}".DLog();
            }
        }
        
        private void Start()
        {
            $"[GameWorldController] Start() 호출됨".DLog();
            
            if (GameSession.Shared == null)
            {
                $"[GameWorldController] ❌ GameSession.Shared가 null입니다!".DError();
                return;
            }
            
            if (GameSession.Shared.CachedWorldList == null)
            {
                $"[GameWorldController] ⚠️ CachedWorldList가 null입니다. 아직 로그인 응답이 안 왔거나, Dev 모드입니다.".DWarnning();
                return;
            }
            
            $"[GameWorldController] ✅ GameSession에서 캐싱된 월드 리스트 로드: {GameSession.Shared.CachedWorldList.worlds?.Count ?? 0}개".DLog();
            OnRecvWorldViewUpdate(GameSession.Shared.CachedWorldList);
        }

        public void OnRecvWorldViewUpdate(WorldListRequest res)
        {
            $"[GameWorldController] 🌍 OnRecvWorldViewUpdate 호출됨".DLog();
            
            if (res?.worlds == null)
            {
                $"[GameWorldController] ❌ res.worlds가 null입니다!".DError();
                return;
            }
            
            if (gameWorldFields == null)
            {
                $"[GameWorldController] ❌ gameWorldFields가 null입니다!".DError();
                return;
            }
            
            $"[GameWorldController] 월드 개수: {res.worlds.Count}, 필드 개수: {gameWorldFields.Count}".DLog();
            
            for (int i = 0; i < res.worlds.Count && i < gameWorldFields.Count; i++)
            {
                if (gameWorldFields[i] == null)
                {
                    $"[GameWorldController] ❌ gameWorldsFields[{i}]가 null입니다!".DError();
                    continue;
                }
                
                var model = res.worlds[i];
                $"[GameWorldController] [{i}] Bind 시작: {model.worldName}, Count: {model.myCharCount}".DLog();
                gameWorldFields[i].Bind(model);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}

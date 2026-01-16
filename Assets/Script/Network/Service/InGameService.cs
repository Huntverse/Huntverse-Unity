using Cysharp.Threading.Tasks;
using Hunt.Common;
using Hunt.Net;
using System;

namespace Hunt
{
    /// <summary> 인게임 서버 통신 서비스 </summary>
    public class InGameService
    {
        private readonly NetworkManager networkManager;
        
        public static event Action<ErrorType, uint> OnMapChangeResponse;
        
        public InGameService(NetworkManager networkManager = null)
        {
            this.networkManager = networkManager ?? NetworkManager.Shared;
        }

        /// <summary> 맵 변경 응답 처리 </summary>
        public static void NotifyMapChangeResponse(ErrorType errorType, uint newMapId)
        {
            $"[InGameService] 맵 변경 응답 수신: {errorType}, MapId: {newMapId}".DLog();
            NotifyMapChangeResponseAsync(errorType, newMapId).Forget();
        }

        private static async UniTaskVoid NotifyMapChangeResponseAsync(ErrorType errorType, uint newMapId)
        {
            await UniTask.SwitchToMainThread();
            OnMapChangeResponse?.Invoke(errorType, newMapId);
        }

        /// <summary> 맵 변경 요청 </summary>
        public void ReqMapChange(uint targetMapId)
        {
            // TODO: 프로토콜 정의 후 실제 구현
            // var req = new MapChangeReq { TargetMapId = targetMapId };
            // networkManager.SendToGame(MsgId.MapChangeReq, req);
            
            $"[InGameService] 맵 변경 요청: {targetMapId}".DLog();
            
            // 임시: 테스트용 즉시 성공 응답
            NotifyMapChangeResponse(ErrorType.ErrNon, targetMapId);
        }
    }
}

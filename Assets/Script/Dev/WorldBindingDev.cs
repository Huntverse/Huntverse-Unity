using UnityEngine;
using System.Collections.Generic;

namespace Hunt.dev
{
    public class WorldBindingDev : MonoBehaviour
    {
        [SerializeField] private bool forceUseDevData = false;
        
        void Start()
        {
            // 서버에 연결되어 있고 강제 사용이 아니면 Dev 데이터를 사용하지 않음
            if (SystemBoot.Shared != null && SystemBoot.Shared.LoginServerConnected && !forceUseDevData)
            {
                $"[Dev] 서버 연결됨 - Dev 데이터 스킵".DLog();
                return;
            }
            
            var dummy = new WorldListRequest
            {
                channels = new List<WorldModel>
                {
                    new WorldModel { worldName = "그라시아", congestion = 1, myCharCount = 0 },
                    new WorldModel { worldName = "라비올래", congestion = 3, myCharCount = 1 },
                    new WorldModel { worldName = "카탄", congestion = 2, myCharCount = 3 }
                }
            };
            
            $"[Dev] 채널 리스트 생성: {dummy.channels.Count}개 (Dev 모드)".DLog();
            
            if (GameWorldController.Shared != null)
            {
                GameWorldController.Shared.OnRecvWorldViewUpdate(dummy);
            }
            else
            {
                "[Channel] GameChannelController.Shared is null!".DError();
            }
        }
    }
}

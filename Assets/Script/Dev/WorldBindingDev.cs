using UnityEngine;
using System.Collections.Generic;

namespace Hunt.dev
{
    public class WorldBindingDev : MonoBehaviour
    {
        void Start()
        {
            var dummy = new WorldListRequest
            {
                channels = new List<WorldModel>
                {
                    new WorldModel { worldName = "그라시아", congestion = 1, myCharCount = 0 },
                    new WorldModel { worldName = "라비올래", congestion = 3, myCharCount = 1 },
                    new WorldModel { worldName = "카탄", congestion = 2, myCharCount = 3 }
                }
            };
            
            $"[Dev] 채널 리스트 생성: {dummy.channels.Count}개".DLog();
            
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

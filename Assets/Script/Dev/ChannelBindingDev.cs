using UnityEngine;
using System.Collections.Generic;

namespace Hunt.dev
{
    public class ChannelBindingDev : MonoBehaviour
    {
        void Start()
        {
            var dummy = new ChannelListRequest
            {
                channels = new List<ChannelModel>
                {
                    new ChannelModel { channelName = "그라시아", congestion = 1, myCharacterCount = 0 },
                    new ChannelModel { channelName = "라비올래", congestion = 3, myCharacterCount = 1 },
                    new ChannelModel { channelName = "카탄", congestion = 2, myCharacterCount = 3 }
                }
            };
            
            $"[Dev] 채널 리스트 생성: {dummy.channels.Count}개".DLog();
            
            if (GameChannelController.Shared != null)
            {
                GameChannelController.Shared.OnRecvChannelViewUpdate(dummy);
            }
            else
            {
                "[Channel] GameChannelController.Shared is null!".DError();
            }
        }
    }
}

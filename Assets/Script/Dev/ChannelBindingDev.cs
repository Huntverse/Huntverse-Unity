using UnityEngine;
using System.Collections.Generic;
namespace hunt.dev
{

    public class ChannelBindingDev : MonoBehaviour
    {
        void Start()
        {
            var dummy = new ChannelListRequest
            {
                channels = new List<ChannelInfoPayload>
                {
                    new(){ channelName = "그라시아", congestion = 1, myCharacterCount=0 },
                    new(){ channelName = "라비올래", congestion = 3, myCharacterCount=1 },
                    new(){ channelName = "카탄", congestion = 2, myCharacterCount=3 }
                }
            };
            Debug.Log($"dummy :{dummy.channels}");
            if (GameChannelController.Shared != null)
            {
                GameChannelController.Shared.OnRecvChannelViewUpdate(dummy);
            }
            else
            {
                Debug.LogError("[Channel] GameChannelController.Shared is null! Make sure the GameObject is active.");
            }
        }
    }
       
}

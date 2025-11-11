using UnityEngine;

namespace hunt
{

    public class ChannelModel
    {
        public string ChannelName;
        public int Congestion;
        public int MyCharacterCount;

        // payload 
        public static ChannelModel FromPayload(ChannelInfoPayload p)
        {
            return new ChannelModel
            {
                ChannelName = p.channelName,
                Congestion = p.congestion,
                MyCharacterCount = p.myCharacterCount
            };
        }
        
    }
}


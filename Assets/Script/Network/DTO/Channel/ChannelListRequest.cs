using System;
using System.Collections.Generic;
namespace hunt
{
    [Serializable]
    public class ChannelInfoPayload
    {
        public string channelName;
        public int congestion;
        public int myCharacterCount;
        public List<CharacterInfoPayload> characters;
    }

    [Serializable]
    public class ChannelListRequest
    {
        public List<ChannelInfoPayload> channels;
    }
}

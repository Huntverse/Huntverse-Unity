using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
namespace Hunt
{

    public class ChannelModel
    {
        [JsonProperty("channelname")] public string ChannelName;
        [JsonProperty("congestion")] public int Congestion;
        [JsonProperty("charactercount")] public int MyCharacterCount;
        [JsonIgnore] public List<CharacterModel> Characters { get; private set; } = new();
        
        // payload 
        public static ChannelModel FromPayload(ChannelInfoPayload p)
        {
            var model = new ChannelModel
            {
                ChannelName = p.channelName,
                Congestion = p.congestion,
                MyCharacterCount = p.myCharacterCount
            };

            model.SetCharacters(p.characters);
            return model;
        }

        public void SetCharacters(IEnumerable<CharacterInfoPayload> payloads)
        {
            Characters = payloads?.Select(CharacterModel.FromPayload).ToList() ?? new List<CharacterModel>();
            if (Characters.Count > 0)
            {
                MyCharacterCount = Characters.Count(character => character.IsCreated);
            }
        }
        
    }
}


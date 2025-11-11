using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace hunt
{
    public class GameChannelController : MonoBehaviourSingleton<GameChannelController>
    {
        [Header("Channel Field")]
        [SerializeField] private List<GameChannelField> gameChannelFields;
        protected override bool DontDestroy => base.DontDestroy;
        
        protected override void Awake()
        {
            base.Awake(); 
        }

        public void OnRecvChannelViewUpdate(ChannelListRequest res)
        {
            Debug.Log($"[Channel] OnRecvChannelViewUpdate");
            
            if (res?.channels == null || gameChannelFields == null) return;
            
            for (int i = 0; i < res.channels.Count && i < gameChannelFields.Count; i++)
            {
                if (gameChannelFields[i] == null) continue;
                var model = ChannelModel.FromPayload(res.channels[i]);
                Debug.Log($"[Channel] model : {model}");
                gameChannelFields[i].Bind(model);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}

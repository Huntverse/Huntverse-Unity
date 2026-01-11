using System.Collections.Generic;
using UnityEngine;
namespace Hunt
{
    public class GameWorldController : MonoBehaviourSingleton<GameWorldController>
    {
        [Header("Channel Field")]
        [SerializeField] private List<GameWorldField> gameChannelFields;

        protected override bool DontDestroy => false;
        
        protected override void Awake()
        {
            base.Awake(); 
        }

        public void OnRecvWorldViewUpdate(WorldListRequest res)
        {
            this.DLog($"OnRecvWorldViewUpdate");
            
            if (res?.channels == null || gameChannelFields == null) return;
            
            for (int i = 0; i < res.channels.Count && i < gameChannelFields.Count; i++)
            {
                if (gameChannelFields[i] == null) continue;
                var model = res.channels[i];
                this.DLog($"model: {model.worldName}, Count: {model.myCharCount}");
                gameChannelFields[i].Bind(model);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}

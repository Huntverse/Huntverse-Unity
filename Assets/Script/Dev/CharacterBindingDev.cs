using UnityEngine;
using System.Collections.Generic;
namespace hunt.dev
{

    public class CharacterBindingDev : MonoBehaviour
    {
        void Start()
        {
            var dummy = new CharacterFieldListRequst
            {
                chfields = new List<CharacterInfoPayload>
                {
                    new(){ professiontype = ProfessionType.Worrior, name = "공안의 그림자", level = 50 },
                    new(){ professiontype = ProfessionType.Magician, name = "헌터버스", level = 35 },
                    new(){ professiontype = ProfessionType.Tanker, name = "pj002321", level = 42 }
                }
            };
            Debug.Log($"dummy character count: {dummy.chfields.Count}");
            if (CharacterCreateController.Shared != null)
            {
                CharacterCreateController.Shared.OnRecvCharacterFieldViewUpdate(dummy);
            }
            else
            {
                Debug.LogError("[Character] CharacterCreateController.Shared is null! Make sure the GameObject is active.");
            }
        }
    }
       
}


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
                    new(){ professiontype = ProfessionType.Worrior, name = "공안의 그림자", level = 50,savepoint="일루네스의 상념정"},
                    new(){ professiontype = ProfessionType.Magician, name = "헌터버스", level = 35,savepoint="서광잔영의 숲" },
                    new(){ professiontype = ProfessionType.Tanker, name = "pj002321", level = 42 ,savepoint="레미나의 잠경촌"}
                }
            };
            $"dummy character count: {dummy.chfields.Count}".DLog();
            if (CharacterCreateController.Shared != null)
            {
                CharacterCreateController.Shared.OnRecvCharacterFieldViewUpdate(dummy);
            }
            else
            {
                "[Character] CharacterCreateController.Shared is null! Make sure the GameObject is active.".DError();
            }
        }
    }

}


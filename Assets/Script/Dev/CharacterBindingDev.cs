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
                    // ATT, DEF, SDP, LUK, AGI
                    new(){ professiontype = ProfessionType.Sword, name = "미미짱", level = 50,savepoint="일루네스의 상념정",stats=new float[]{0.5f,0.5f,0.6f,0.4f,0.6f} },
                    new(){ professiontype = ProfessionType.Archer, name = "헌터버스", level = 35,savepoint="서광잔영의 숲",stats=new float[]{0.4f,0.3f,0.7f,0.7f,0.8f} },
                    new(){ professiontype = ProfessionType.Fighter, name = "pj002321", level = 42 ,savepoint="레미나의 잠경촌", stats = new float[] { 0.6f, 0.8f, 0.3f, 0.5f, 0.4f }}
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


using UnityEngine;
using System.Collections.Generic;
namespace Hunt.dev
{
    /// <summary>
    /// 채널별 캐릭터 리스트를 더미 데이터로 전송하는 Dev 스크립트
    /// 실전과 같은 구조: 채널 이름을 키로 사용
    /// </summary>
    public class CharacterBindingDev : MonoBehaviour
    {
        void Start()
        {
            // 채널 "그라시아"의 캐릭터 리스트
            var graciaCharacters = new CharacterFieldListRequst
            {
                chfields = new List<CharacterInfoPayload>
                {

                }
            };

            // 채널 "라비올래"의 캐릭터 리스트
            var rabiolleCharacters = new CharacterFieldListRequst
            {
                chfields = new List<CharacterInfoPayload>
                {
                    new(){ professiontype = ProfessionType.Sword, name = "shuehj", level = 50, savepoint="일루네스의 상념정", stats=new float[]{0.5f,0.5f,0.6f,0.4f,0.6f} },
                }
            };

            // 채널 "카탄"의 캐릭터 리스트
            var katanCharacters = new CharacterFieldListRequst
            {
                chfields = new List<CharacterInfoPayload>
                {

                    new(){ professiontype = ProfessionType.Archer, name = "Pent12", level = 15, savepoint="일루네스의 상념정", stats=new float[]{0.5f,0.5f,0.6f,0.4f,0.6f} },
                    new(){ professiontype = ProfessionType.Fighter, name = "헌터버스", level = 55, savepoint="서광잔영의 숲", stats=new float[]{0.4f,0.3f,0.7f,0.7f,0.8f} },
                    new(){ professiontype = ProfessionType.Archer, name = "pj002321", level = 32, savepoint="레미나의 잠경촌", stats = new float[] { 0.6f, 0.8f, 0.3f, 0.5f, 0.4f }}
                }
            };

            if (CharacterCreateController.Shared != null)
            {
                // 실전처럼 채널 이름을 키로 사용하여 캐릭터 리스트 캐싱
                CharacterCreateController.Shared.OnRecvCharacterFieldViewUpdate("그라시아", graciaCharacters);
                CharacterCreateController.Shared.OnRecvCharacterFieldViewUpdate("라비올래", rabiolleCharacters);
                CharacterCreateController.Shared.OnRecvCharacterFieldViewUpdate("카탄", katanCharacters);
                
                $"✅ [Dev] Cached character lists for channels: 그라시아({graciaCharacters.chfields.Count}), 라비올래({rabiolleCharacters.chfields.Count}), 카탄({katanCharacters.chfields.Count})".DLog();
            }
            else
            {
                "[Character] CharacterCreateController.Shared is null! Make sure the GameObject is active.".DError();
            }
        }
    }
}


using UnityEngine;
using System.Collections.Generic;
using Hunt.Game;

namespace Hunt.dev
{
    /// <summary>
    /// 채널별 캐릭터 리스트를 더미 데이터로 전송하는 Dev 스크립트
    /// Payload 제거, CharacterModel만 직접 사용
    /// </summary>
    public class CharacterBindingDev : MonoBehaviour
    {
        void Start()
        {
            // 채널 "그라시아"의 캐릭터 리스트
            var graciaCharacters = new List<CharacterModel>
            {
                // 비어있음 - 캐릭터 없는 채널
            };

            // 채널 "라비올래"의 캐릭터 리스트
            var rabiolleCharacters = new List<CharacterModel>
            {
                new CharacterModel
                {
                    worldId = 1,
                    charId = 1001,
                    name = "shuehj",
                    classtype = ClassType.Sword,
                    level = 50,
                    mapId = 1,  // 일루네스의 상념정
                    stats = new List<StatInfo>
                    {
                        new StatInfo { Type = 0, Point = 50 },  // ATT
                        new StatInfo { Type = 1, Point = 30 },  // DEF
                        new StatInfo { Type = 2, Point = 60 },  // SPD
                        new StatInfo { Type = 3, Point = 40 },  // LUK
                        new StatInfo { Type = 4, Point = 55 }   // AGI
                    }
                }
            };

            // 채널 "카탄"의 캐릭터 리스트
            var katanCharacters = new List<CharacterModel>
            {
                new CharacterModel
                {
                    worldId = 1,
                    charId = 2001,
                    name = "Pent12",
                    classtype = ClassType.Archer,
                    level = 15,
                    mapId = 1,  // 일루네스의 상념정
                    stats = new List<StatInfo>
                    {
                        new StatInfo { Type = 0, Point = 45 },
                        new StatInfo { Type = 1, Point = 25 },
                        new StatInfo { Type = 2, Point = 70 },
                        new StatInfo { Type = 3, Point = 35 },
                        new StatInfo { Type = 4, Point = 60 }
                    }
                },
                new CharacterModel
                {
                    worldId = 1,
                    charId = 2002,
                    name = "헌터버스",
                    classtype = ClassType.Fighter,
                    level = 55,
                    mapId = 2,  // 서광잔영의 숲
                    stats = new List<StatInfo>
                    {
                        new StatInfo { Type = 0, Point = 80 },
                        new StatInfo { Type = 1, Point = 70 },
                        new StatInfo { Type = 2, Point = 40 },
                        new StatInfo { Type = 3, Point = 50 },
                        new StatInfo { Type = 4, Point = 45 }
                    }
                },
                new CharacterModel
                {
                    worldId = 1,
                    charId = 2003,
                    name = "pj002321",
                    classtype = ClassType.Archer,
                    level = 32,
                    mapId = 0,  // 레미나의 잠경촌
                    stats = new List<StatInfo>
                    {
                        new StatInfo { Type = 0, Point = 60 },
                        new StatInfo { Type = 1, Point = 40 },
                        new StatInfo { Type = 2, Point = 55 },
                        new StatInfo { Type = 3, Point = 65 },
                        new StatInfo { Type = 4, Point = 50 }
                    }
                }
            };

            if (CharacterCreateController.Shared != null)
            {
                // ✅ CharacterModel 리스트를 직접 전달
                CharacterCreateController.Shared.OnRecvCharacterList("그라시아", graciaCharacters);
                CharacterCreateController.Shared.OnRecvCharacterList("라비올래", rabiolleCharacters);
                CharacterCreateController.Shared.OnRecvCharacterList("카탄", katanCharacters);

                // MapId 테스트
                foreach (var character in katanCharacters)
                {
                    string mapName = BindKeyConst.GetMapNameByMapId(character.mapId);
                    $"[Dev] {character.name}: MapId {character.mapId} -> {mapName}".DLog();
                }
            }
            else
            {
                "[Character] CharacterCreateController.Shared is null!".DError();
            }
        }
    }
}
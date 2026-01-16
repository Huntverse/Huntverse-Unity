using System.Collections.Generic;
using System;
using UnityEngine;
using Hunt.Common;
using Hunt.Login;
using System.Linq;
namespace Hunt.Net
{
    //: MonoBehaviourSingleton<MsgDispatcherBase>
    class MsgDispatcherBase
    {
        //delegate: byte[], int => payload, offset, len
        private Dictionary<Hunt.Common.MsgId, Action<byte[], int, int>> m_handlers;

        public MsgDispatcherBase()
        {
            m_handlers = new Dictionary<Common.MsgId, Action<byte[], int, int>>();
        }

        public virtual bool Init()
        {
            return false;
        }

        protected bool AddHandler(Hunt.Common.MsgId type, Action<byte[], int, int> handle)
        {
            Debug.Assert(!m_handlers.ContainsKey(type));//duplicate Key
            if (m_handlers.ContainsKey(type))
            {
                return false;
            }
            m_handlers.Add(type, handle);
            return true;
        }

        public bool GetHandler(Hunt.Common.MsgId type, out Action<byte[], int, int> outHandle)

        {
            return m_handlers.TryGetValue(type, out outHandle);
        }

        public Action<byte[], int, int> GetHandler(Hunt.Common.MsgId type)
        {
            m_handlers.TryGetValue(type, out var outHandle);
            return outHandle;
        }
    }

    /*
        공용적인 패킷에 대한 핸들러 집합
    */
    class CommonMsgDispatcher : MsgDispatcherBase
    {
        public CommonMsgDispatcher()
        {
        }
        public override bool Init()
        {
            return true;
        }
    }

    /*
        로그인 관련 패킷에 대한 핸들러 집합 
    */
    class LoginMsgDispatcher : MsgDispatcherBase
    {
        public LoginMsgDispatcher()
        {
        }

        public override bool Init()
        {
            AddHandler(MsgId.LoginTestAns, OnLoginTestAns);
            AddHandler(MsgId.LoginAns, OnLoginAns);
            AddHandler(MsgId.SelectWorldAns, OnSelectWorldAns);
            AddHandler(MsgId.CreateAccountAns, OnCreateAccountAns);
            AddHandler(MsgId.CreateCharAns, OnCreateCharAns);
            AddHandler(MsgId.ConfirmIdAns, OnConfirmIdAns);
            AddHandler(MsgId.ConfirmNameAns, OnConfirmNameAns);
            return true;
        }

        static void OnLoginTestAns(byte[] payload, int offset, int len)
        {
            var testAns = LoginTestAns.Parser.ParseFrom(payload, offset, len);
            Debug.Log($"Recv: {testAns.Data}");
        }

        static void OnLoginAns(byte[] payload, int offset, int len)
        {
            $"[MsgDispatcher] OnLoginAns 핸들러 호출됨".DLog();
            var loginAns = LoginAns.Parser.ParseFrom(payload, offset, len);

            if (loginAns.ErrType == ErrorType.ErrNon)
            {
                $"[MsgDispatcher] 로그인 성공: {loginAns.ErrType}".DLog();
                $"[MsgDispatcher] CharInfos 개수: {loginAns.CharInfos?.Count ?? 0}".DLog();

                var worldModels = new Dictionary<uint, WorldModel>();

                // 서버에서 받은 CharInfos로 월드 생성
                if (loginAns.CharInfos != null && loginAns.CharInfos.Count > 0)
                {
                    foreach (var charByWorld in loginAns.CharInfos)
                    {
                        string worldName = BindKeyConst.GetWorldNameByWorldId(charByWorld.WorldId);
                        worldModels[charByWorld.WorldId] = new WorldModel
                        {
                            worldName = worldName,
                            congestion = 1, // 기본값
                            myCharCount = (int)charByWorld.CharCnt
                        };
                        $"[MsgDispatcher] 월드 생성: ID={charByWorld.WorldId}, Name={worldName}, CharCnt={charByWorld.CharCnt}".DLog();
                    }
                }
                else
                {
                    // ⚠️ 서버가 CharInfos를 안 보낸 경우 - Fallback으로 기본 월드 생성
                    $"[MsgDispatcher] ⚠️ CharInfos 없음 - Fallback: 기본 월드 생성".DWarnning();
                    worldModels[1] = new WorldModel { worldName = "그라시아", congestion = 1, myCharCount = 0 };
                    worldModels[2] = new WorldModel { worldName = "라비올래", congestion = 2, myCharCount = 0 };
                    worldModels[3] = new WorldModel { worldName = "카탄", congestion = 1, myCharCount = 0 };
                }

                $"[MsgDispatcher] 월드 생성 완료: {worldModels.Count}개".DLog();

                var worldListReq = new WorldListRequest
                {
                    channels = new List<WorldModel>(worldModels.Values)
                };
                
                $"[MsgDispatcher] WorldListRequest 생성: {worldListReq.channels?.Count ?? 0}개".DLog();

                // GameSession에 월드 리스트 저장 (MainMenu 씬 로드 전이므로)
                GameSession.Shared?.SetWorldList(worldListReq);
                
                // GameWorldController가 있으면 바로 전달 (Dev 모드 등)
                if (GameWorldController.Shared != null)
                {
                    $"[MsgDispatcher] ✅ GameWorldController로 월드 리스트 즉시 전달: {worldListReq.channels.Count}개".DLog();
                    GameWorldController.Shared.OnRecvWorldViewUpdate(worldListReq);
                }
                else
                {
                    $"[MsgDispatcher] GameWorldController 아직 없음 - GameSession에 캐싱됨".DLog();
                }
                
                // ✅ 모든 월드의 캐릭터 정보 자동 요청
                foreach (var worldId in worldModels.Keys)
                {
                    var selectWorldReq = new SelectWorldReq { WorldId = worldId };
                    Hunt.Net.NetworkManager.Shared?.SendToLogin(Hunt.Common.MsgId.SelectWorldReq, selectWorldReq);
                    $"[MsgDispatcher] 월드 캐릭터 정보 요청: WorldId={worldId}".DLog();
                }
            }
            else
            {
                if (loginAns.ErrType == ErrorType.ErrDupLogin)
                {
                    $"[MsgDispatcher] 로그인 실패 - 중복 로그인: {loginAns.ErrType}".DLog();
                }
                if (loginAns.ErrType == ErrorType.ErrDb)
                {
                    $"[MsgDispatcher] 로그인 실패 - DB 에러: {loginAns.ErrType}".DError();
                }
            }

            Hunt.LoginService.NotifyLoginResponse(loginAns.ErrType);
        }

        static void OnSelectWorldAns(byte[] payload, int offset, int len)
        {
            var selectWorldAns = SelectWorldAns.Parser.ParseFrom(payload, offset, len);
            $"[MsgDispatcher] OnSelectWorldAns Recv: ErrType={selectWorldAns.ErrType}".DLog();
            $"[MsgDispatcher] OnSelectWorldAns SimpleCharInfosLen: {selectWorldAns.SimpleCharInfos?.Count ?? 0}".DLog();

            if (selectWorldAns.ErrType != ErrorType.ErrNon)
            {
                $"[MsgDispatcher] ❌ SelectWorld 실패: {selectWorldAns.ErrType}".DError();
                return;
            }

            if (selectWorldAns.SimpleCharInfos == null || selectWorldAns.SimpleCharInfos.Count == 0)
            {
                $"[MsgDispatcher] ℹ️ 캐릭터 없음 (빈 캐시는 이미 초기화됨)".DLog();
                return;
            }

            // 캐릭터 정보를 GameSession에 저장
            var charList = new List<SimpleCharacterInfo>(selectWorldAns.SimpleCharInfos);
            GameSession.Shared?.SetCharacterList(charList);

            // 월드별로 캐릭터 분류 및 캐싱
            var charsByWorld = new Dictionary<string, List<CharModel>>();

            foreach(var charInfo in selectWorldAns.SimpleCharInfos)
            {
                uint worldId = charInfo.WorldId;
                
                // ⚠️ 서버가 WorldId=0을 보내는 경우 기본 월드(그라시아=1)로 고정
                if (worldId == 0)
                {
                    worldId = 1; // 그라시아
                    $"[MsgDispatcher] ⚠️ WorldId=0 감지! 그라시아(WorldId=1)로 설정".DWarnning();
                }
                
                string worldName = BindKeyConst.GetWorldNameByWorldId(worldId);
                $"[MsgDispatcher] 캐릭터 정보: Name={charInfo.Name}, OriginalWorldId={charInfo.WorldId}, FixedWorldId={worldId}, WorldName={worldName}, ClassType={charInfo.ClassType}, CharId={charInfo.CharId}".DLog();
                
                if (!charsByWorld.ContainsKey(worldName))
                {
                    charsByWorld[worldName] = new List<CharModel>();
                }
                
                // CharModel 생성 시 수정된 worldId 사용
                var charModel = CharModel.FromCharacterInfo(charInfo);
                charModel.worldId = worldId;
                charsByWorld[worldName].Add(charModel);
            }

            foreach(var kvp in charsByWorld)
            {
                // CharacterSetupController에 캐릭터 리스트 전달
                CharacterSetupController.Shared?.OnRecvCharacterList(kvp.Key, kvp.Value);
                
                // GameSession에도 저장 (씬 전환 후에도 유지)
                if (GameSession.Shared != null)
                {
                    if (!GameSession.Shared.CachedCharactersByWorld.ContainsKey(kvp.Key))
                    {
                        GameSession.Shared.CachedCharactersByWorld[kvp.Key] = new List<CharModel>();
                    }
                    
                    foreach (var charModel in kvp.Value)
                    {
                        bool exists = GameSession.Shared.CachedCharactersByWorld[kvp.Key].Any(c => c.charId == charModel.charId);
                        if (!exists)
                        {
                            GameSession.Shared.CachedCharactersByWorld[kvp.Key].Add(charModel);
                        }
                    }
                    
                    // 월드의 myCharCount 업데이트
                    int totalCount = GameSession.Shared.CachedCharactersByWorld[kvp.Key].Count;
                    if (GameSession.Shared.CachedWorldList?.channels != null)
                    {
                        var world = GameSession.Shared.CachedWorldList.channels.Find(w => w.worldName == kvp.Key);
                        if (world != null)
                        {
                            world.myCharCount = totalCount;
                            $"[MsgDispatcher] 🔄 월드 카운트 업데이트: {kvp.Key} → {totalCount}개".DLog();
                        }
                    }
                }
                
                $"[MsgDispatcher] ✅ 캐릭터 캐싱 업데이트: {kvp.Key} - {kvp.Value.Count}개".DLog();
            }
        }
        static void OnCreateAccountAns(byte[] payload, int offset, int len)
        {
            $"[MsgDispatcher] OnCreateAccountAns 핸들러 호출됨".DLog();
            var createAccountAns = CreateAccountAns.Parser.ParseFrom(payload, offset, len);

            if (createAccountAns.ErrType == ErrorType.ErrNon)
            {
                $"[MsgDispatcher] 계정 생성 성공: {createAccountAns.ErrType}".DLog();
            }
            else
            {
                if (createAccountAns.ErrType == ErrorType.ErrDupId)
                {
                    $"[MsgDispatcher] 계정 생성 실패 - ID중복: {createAccountAns.ErrType}".DLog();
                }
                if (createAccountAns.ErrType == ErrorType.ErrDb)
                {
                    $"[MsgDispatcher] 계정 생성 실패 - DB에러: {createAccountAns.ErrType}".DError();
                }
            }

            Hunt.LoginService.NotifyCreateAccountResponse(createAccountAns.ErrType);
        }

        static void OnCreateCharAns(byte[] payload, int offset, int len)
        {
            var createCharAns = CreateCharAns.Parser.ParseFrom(payload, offset, len);
            if (createCharAns.ErrType == ErrorType.ErrNon)
            {
                Debug.Log($"OnCreateCharAns Recv: {createCharAns.ErrType}, {createCharAns.CharInfo.Name}, {createCharAns.CharInfo.CharId}, {createCharAns.CharInfo.WorldId}, {createCharAns.CharInfo.ClassType}");
            }
            else
            {
                if (createCharAns.ErrType == ErrorType.ErrDupNickName)
                {
                    Debug.Log($"OnCreateCharAns Recv: [Error:{createCharAns.ErrType}], 닉네임 중복");

                }
                if (createCharAns.ErrType == ErrorType.ErrDb)
                {
                    Debug.Log($"OnCreateCharAns Recv: [Error:{createCharAns.ErrType}], DB 에러");
                }
            }

            Hunt.LoginService.NotifyCreateCharResponse(createCharAns.ErrType, createCharAns.CharInfo);
        }

        static void OnConfirmIdAns(byte[] payload, int offset, int len)
        {
            $"[MsgDispatcher] OnConfirmIdAns 핸들러 호출됨".DLog();
            var ans = ConfirmIdAns.Parser.ParseFrom(payload, offset, len);

            if (ans.ErrType == ErrorType.ErrNon)
            {
                $"[MsgDispatcher] 아이디 중복확인 응답: ErrType={ans.ErrType}, IsDup={ans.IsDup}".DLog();
            }
            else
            {
                if (ans.ErrType == ErrorType.ErrDb)
                {
                    $"[MsgDispatcher] 아이디 중복확인 실패 - DB 에러: {ans.ErrType}".DError();
                }
            }

            Hunt.LoginService.NotifyConfirmIdResponse(ans.ErrType, ans.IsDup);
        }

        static void OnConfirmNameAns(byte[] payload, int offset, int len)
        {
            var ans = ConfirmNameAns.Parser.ParseFrom(payload, offset, len);
            if (ans.ErrType == ErrorType.ErrNon)
            {
                Debug.Log($"OnConfirmNameAns Recv: {ans.ErrType}, [isDup: {ans.IsDup}]");
            }
            else
            {
                if (ans.ErrType == ErrorType.ErrDb)
                {
                    Debug.Log($"OnConfirmNameAns Recv: [Error:{ans.ErrType}], DB 에러");
                }
            }
            Hunt.LoginService.NotifyConfirmNameResponse(ans.ErrType, ans.IsDup);
        }
    }

    /*
        이동 전투와 같은 게임 관련 패킷입니다.
    */

    class GameMsgDispatcher : MsgDispatcherBase
    {
        public GameMsgDispatcher()
        {
        }

        public override bool Init()
        {
            return true;
        }
    }

    /*
    치트 패킷에 대한 핸들러 집합 (모든 몬스터 죽이기, 테스트 하기 위한 패킷)
*/
    class CheatMsgDispatcher : MsgDispatcherBase
    {
        public CheatMsgDispatcher()
        {
        }

        public override bool Init()
        {
            return true;
        }
    }
}


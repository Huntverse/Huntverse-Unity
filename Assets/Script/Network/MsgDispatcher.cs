using System.Collections.Generic;
using System;
using UnityEngine;
using Hunt.Common;
using Hunt.Login;

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
            var loginAns = LoginAns.Parser.ParseFrom(payload, offset, len);
            if (loginAns.ErrType == ErrorType.ErrNon)
            {
                Debug.Log($"OnLoginAns Recv: {loginAns.ErrType}");
            }
            else
            {
                if (loginAns.ErrType == ErrorType.ErrDupLogin)
                {
                    Debug.Log($"OnLoginAns Recv: {loginAns.ErrType}, 중복 로그인");
                }
                if (loginAns.ErrType == ErrorType.ErrDb)
                {
                    Debug.Log($"OnLoginAns Recv: {loginAns.ErrType}, DB 에러");
                }
            }
            
            Hunt.LoginService.NotifyLoginResponse(loginAns.ErrType);
        }

        static void OnSelectWorldAns(byte[] payload, int offset, int len)
        {
            var selectWorldAns = SelectWorldAns.Parser.ParseFrom(payload, offset, len);
            Debug.Log($"OnSelectWorldAns Recv: {selectWorldAns.ErrType}");
            Debug.Log($"OnSelectWorldAns SimpleCharInfosLen: {selectWorldAns.SimpleCharInfos.Count}");
            foreach (var simpleChar in selectWorldAns.SimpleCharInfos)
            {
                //simpleChar.ClassType;
                //simpleChar.Level;
                //simpleChar.MapId;
            }
        }
        static void OnCreateAccountAns(byte[] payload, int offset, int len)
        {
            var createAccountAns = CreateAccountAns.Parser.ParseFrom(payload, offset, len);
            if (createAccountAns.ErrType == ErrorType.ErrNon)
            {
                Debug.Log($"OnCreateAccountAns Recv: {createAccountAns.ErrType}");
            }
            else
            {
                if (createAccountAns.ErrType == ErrorType.ErrDupId)
                {
                    Debug.Log($"OnCreateAccountAns Recv: {createAccountAns.ErrType}, ID중복");
                }
                if (createAccountAns.ErrType == ErrorType.ErrDb)
                {
                    Debug.Log($"OnCreateAccountAns Recv: {createAccountAns.ErrType}, DB에러"); //얘는 모든 Ans에 대해서 그냥 팝업으로 DB에러 발생했습니다 띄우면 될듯?

                }

            }

            Hunt.LoginService.NotifyLoginResponse(createAccountAns.ErrType);
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

            Hunt.LoginService.NotifyCreateCharResponse(createCharAns.ErrType);
        }

        static void OnConfirmIdAns(byte[] payload, int offset, int len)
        {
            var ans = ConfirmIdAns.Parser.ParseFrom(payload, offset, len);
            if (ans.ErrType == ErrorType.ErrNon)
            {
                Debug.Log($"OnConfirmIdAns Recv: {ans.ErrType}, [isDup: {ans.IsDup}]");
            }
            else
            {
                if (ans.ErrType == ErrorType.ErrDb)
                {
                    Debug.Log($"OnConfirmIdAns Recv: [Error:{ans.ErrType}], DB 에러");
                }
            }
            Hunt.LoginService.NotifyLoginResponse(ans.ErrType);
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
            Hunt.LoginService.NotifyCreateCharResponse(ans.ErrType);
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


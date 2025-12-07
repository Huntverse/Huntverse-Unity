using System.Collections.Generic;
using System;
using UnityEngine;
using JetBrains.Annotations;
using UnityEngine.XR;

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
            return true;
        }

        static void OnLoginTestAns(byte[] payload, int offset, int len)
        {
            var testReq = LoginTestAns.Parser.ParseFrom(payload, offset, len);
            Debug.Log($"Recv: {testReq.Data}");
        }

        static void OnLoginAns(byte[] payload, int offset, int len)
        {
            var testReq = LoginAns.Parser.ParseFrom(payload, offset, len);
            Debug.Log($"OnLoginAns Recv: {testReq.ErrType}");
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


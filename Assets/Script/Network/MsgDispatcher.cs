using System.Collections.Generic;
using System;
using UnityEngine;

using Hunt.Common;
using JetBrains.Annotations;
using hunt.Net;
using Hunt.Login;


namespace hunt.Net
{
    //: MonoBehaviourSingleton<MsgDispatcherBase>
    class MsgDispatcherBase
    {
        //delegate: byte[], int => payload, offset, len
        private Dictionary<Hunt.Common.PacketType, Action<byte[], int, int>> m_handlers;

        public void AddHandler(Hunt.Common.PacketType type, Action<byte[], int, int> handle)
        {
            Debug.Assert(!m_handlers.ContainsKey(type));//duplicate Key
            m_handlers.Add(type, handle);
        }

        public bool GetHandler(Hunt.Common.PacketType type, out Action<byte[], int, int> outHandle)

        {
            return m_handlers.TryGetValue(type, out outHandle);
        }

        public Action<byte[], int, int> GetHandler(Hunt.Common.PacketType type)
        {
            m_handlers.TryGetValue(type, out var outHandle);
            return outHandle;
        }
    }

    /*
        치트 패킷에 대한 핸들러 집합 (모든 몬스터 죽이기, 테스트 하기 위한 패킷)
    */
    class CheatMsgDispacher : MonoBehaviourSingleton<CheatMsgDispacher>
    {
        private MsgDispatcherBase m_dispatcher;

        public Action<byte[], int, int> Gethandler(Hunt.Common.PacketType type)
         => m_dispatcher.GetHandler(type);

        protected override void Awake()
        {
            base.Awake();
            m_dispatcher.AddHandler(PacketType.LoginTestAns, CheatMsgDispacher.OnLoginTestReq);
        }

        static void OnLoginTestReq(byte[] payload, int offset, int len)
        {
            var testReq = LoginTestAns.Parser.ParseFrom(payload, offset, len);
            Debug.Log($"Recv: {testReq.Data}");
        }
    }

    /*
        공용적인 패킷에 대한 핸들러 집합
    */
    class CommonMsgDispacher : MonoBehaviourSingleton<CommonMsgDispacher>
    {
        private MsgDispatcherBase m_dispatcher;

        public Action<byte[], int, int> Gethandler(Hunt.Common.PacketType type)
         => m_dispatcher.GetHandler(type);

        protected override void Awake()
        {
            base.Awake();

        }
    }

    /*
        로그인 관련 패킷에 대한 핸들러 집합 
    */
    class LoginMsgDispatcher : MonoBehaviourSingleton<LoginMsgDispatcher>
    {
        private MsgDispatcherBase m_dispatcher;

        public Action<byte[], int, int> Gethandler(Hunt.Common.PacketType type)
         => m_dispatcher.GetHandler(type);

        protected override void Awake()
        {
            base.Awake();

        }
    }
    class GameMsgDispatcher : MonoBehaviourSingleton<GameMsgDispatcher>
    {
        private MsgDispatcherBase m_dispatcher;

        public Action<byte[], int, int> Gethandler(Hunt.Common.PacketType type)
         => m_dispatcher.GetHandler(type);

        protected override void Awake()
        {
            base.Awake();

        }
    }
}


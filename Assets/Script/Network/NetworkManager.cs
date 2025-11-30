using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using static hunt.Net.NetModule;
//using hunt.Net;

namespace hunt.Net
{
    public class NetworkManager : MonoBehaviourSingleton<NetworkManager>
    {
        private NetModule m_loginConnection;
        //k: ip<<32 | port, v: netModule
        private Dictionary<UInt64, NetModule> m_tcpConnections;
        private Dictionary<NetModule.ServiceType, MsgDispatcherBase> m_dispatchers;

        public NetModule MakeNetModule(ServiceType type, Action<NetModule.ERROR, string>? disconnectHandler, Action connSuccessHandler, Action<SocketException> connFailHandler)
        {
            return new NetModule(type, disconnectHandler, connSuccessHandler, connFailHandler);
        }

        protected override void Awake()
        {
            base.Awake();
            m_dispatchers = new Dictionary<NetModule.ServiceType, MsgDispatcherBase>();
            //치트는 에디터에서만 가능
#if UNITY_EDITOR
            m_dispatchers.Add(ServiceType.Cheat, new CheatMsgDispatcher());
#endif
            m_dispatchers.Add(ServiceType.Login, new LoginMsgDispatcher());
            m_dispatchers.Add(ServiceType.Common, new CommonMsgDispatcher());
            m_dispatchers.Add(ServiceType.Game, new GameMsgDispatcher());
            foreach (var dispatcher in m_dispatchers.Values)
            {
                var suc = dispatcher.Init();
                Debug.Assert(suc);
            }
        }

        public async Task<bool> ConnLoginServer(Action<NetModule.ERROR, string>? disconnectHandler, Action connSuccessHandler, Action<SocketException> connFailHandler)
        {
            m_loginConnection = new NetModule(ServiceType.Login, disconnectHandler, connSuccessHandler, connFailHandler);
            return await m_loginConnection.AsyncConn("127.0.0.1", 9000);
        }

        public bool ConnLoginServerSync(Action<NetModule.ERROR, string>? disconnectHandler, Action connSuccessHandler, Action<SocketException> connFailHandler)
        {
            m_loginConnection = new NetModule(ServiceType.Login, disconnectHandler, connSuccessHandler, connFailHandler);
            return m_loginConnection.SyncConn("127.0.0.1", 9000);
        }

        public void StartLoginServer()
        {
            m_loginConnection.Start();
        }

        public void DisConnLoginServer()
        {
            m_loginConnection.Stop();
        }

        public void SendToLogin<ProtoT>(Hunt.Common.PacketType type, ProtoT data) where ProtoT : Google.Protobuf.IMessage
            => m_loginConnection.Send(type, data);

        public bool IsExistConnection(UInt64 key)
        {
            return m_tcpConnections.ContainsKey(key);
        }

        public bool InsertNetModule(UInt64 key, NetModule module)//after conn success, start
        {
            var suc = m_tcpConnections.TryAdd(key, module);
            if (suc)
            {
                m_tcpConnections[key].Start();
            }
            return suc;
        }

        public Action<byte[], int, int> GetDispatcher(NetModule.ServiceType serviceType, Hunt.Common.PacketType packetType)
        {
            m_dispatchers.TryGetValue(serviceType, out var dispatcher);
            return dispatcher.GetHandler(packetType);
        }
    }
}

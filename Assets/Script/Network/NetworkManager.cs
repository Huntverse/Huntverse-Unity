using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
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

        public NetModule MakeNetModule(ServiceType type, Action<NetModule.ERROR, string>? disconnectHandler, Action connSuccessHandler, Action<SocketException> connFailHandler)
        {
            return new NetModule(type, disconnectHandler, connSuccessHandler, connFailHandler);
        }

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

    }
}

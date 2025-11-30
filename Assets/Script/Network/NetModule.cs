using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Google.Protobuf;
using Unity.VisualScripting;
using Hunt.Common;
using NUnit.Framework.Constraints;
using System.IO;
using Steamworks;
using Mirror.BouncyCastle.Bcpg;
using System.Collections.Generic;


//using Google.Protobuf.Serialize;

/*
 * TcpCient::Stream::Write는 thread safe가 아님
 * Send Thread 분리
 * 
 * 
 * https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.networkstream
 * 
 */

namespace hunt.Net
{
    public class NetModule
    {
        public enum ERROR
        {
            IO = 1,
            Cancel = 2,
            Socket = 3
        };

        public enum ServiceType
        {
            None = 0,
            Common = 1,
            Login = 2,
            Game = 4,
            Cheat = 8,
        }
        private static readonly int PacketHeaderSize = 2;//c++서버 sh::IO_Engine에서 TCP 패킷 길이를 의미하는 패킷 헤더의 사이즈는 ushort로 2바이트
        private static readonly int MsgIdSize = sizeof(UInt32);//c++서버 huntverse의 메세지 id는 4바이트 고정입니다.
        private TcpClient m_tcpClient;
        private SendContext m_sendContext;

        private readonly Action<SocketException>? m_connFailHandler;
        private readonly Action? m_connSuccessHandler;
        private readonly Action<NetModule.ERROR, string>? m_disconnectHandler;//서버로부터 연결이 끊긴 경우

        private NetworkStream m_stream;
        private CancellationTokenSource m_stopToken;
        private ServiceType m_type;

        private Task m_sendTask = null;
        private Task m_recvTask = null;
        private bool m_isStoped = false;

        //연결 성공,실패에 대한 호출 핸들
        public NetModule(ServiceType type, Action<NetModule.ERROR, string>? disconnectHandler, Action connSuccessHandler, Action<SocketException> connFailHandler)
        {
            Debug.Assert(connFailHandler != null, "connFailHandler is null"); // Debug 모드에서만
            Debug.Assert(connSuccessHandler != null, "connFailHandler is null"); // Debug 모드에서만
            Debug.Assert(disconnectHandler != null, "connFailHandler is null"); // Debug 모드에서만
            m_connFailHandler = connFailHandler;
            m_connSuccessHandler = connSuccessHandler;
            m_disconnectHandler = disconnectHandler;
            m_type = type;
            m_sendContext = new SendContext(BitConverter.IsLittleEndian);//바이트 오더는 실제로 send/recv에서 활용
            m_tcpClient = new TcpClient();
            m_isStoped = false;
        }

        public bool SyncConn(string ip, int port)
        {
            try
            {
                m_tcpClient.Connect(ip, port);

            }
            catch (SocketException e)
            {
                m_connFailHandler(e);
                return false;
            }
            m_connSuccessHandler();
            return true;
        }

        public async Task<bool> AsyncConn(string ip, int port)
        {
            try
            {
                await m_tcpClient.ConnectAsync(ip, port);
            }
            catch (SocketException e)
            {
                m_connFailHandler(e);
                return false;
            }
            m_connSuccessHandler();
            return true;
        }

        public void Start()
        {
            m_isStoped = false;
            m_stream = m_tcpClient.GetStream();
            m_stopToken = new CancellationTokenSource();
            m_sendTask = RunningSend(m_stopToken.Token);
            m_recvTask = RunningRecv(m_stopToken.Token);
        }

        public void Stop()
        {
            if (m_isStoped)
            {
                return;
            }
            m_isStoped = true;
            m_stopToken.Cancel();
            _ = CleanUpAsync();
        }

        private async Task CleanUpAsync()
        {
            try
            {
                // Task 완료 대기
                var tasks = new List<Task>();
                if (m_sendTask != null) tasks.Add(m_sendTask);
                if (m_recvTask != null) tasks.Add(m_recvTask);

                if (tasks.Count > 0)
                {
                    await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(2000));
                }
            }
            catch { }
            finally
            {
                // 리소스 정리
                m_stopToken?.Dispose();
                m_stopToken = null;
                m_stream?.Close();
                m_stream = null;
                m_tcpClient?.Close();
                m_tcpClient = null;
            }

            m_sendTask = null;
            m_recvTask = null;
        }

        private async Task RunningSend(CancellationToken token)
        {
            while (!token.IsCancellationRequested)//진행해도 되는지 확인
            {
                var sendData = m_sendContext.GetSendAbleData();
                if (sendData.GetLength() == 0)
                {
                    await Task.Delay(50, token);///공회전 막고 싶은데, 이거 나중에 atomic으로 최적화하자
                    continue;
                }
                try
                {
                    await m_stream.WriteAsync(sendData.GetData(), 0, (int)sendData.GetLength());
                    await m_stream.FlushAsync(token);//flush할건데, 클라가 종료나 이런 경우에 취소를 시키는 기능
                }
                catch (SocketException ex)
                {
                    // 소켓 레벨 오류 (IOException보다 구체적)
                    Debug.LogError($"소켓 오류: {ex.SocketErrorCode} - {ex.Message}");
                    m_stopToken.Cancel();//recv도 캔슬해야하니
                    m_disconnectHandler(ERROR.Socket, ex.Message);
                    break;
                }
                catch (IOException ex)
                {
                    Debug.LogError($"전송 오류: {ex.Message}");
                    m_stopToken.Cancel();//recv도 캔슬해야하니
                    m_disconnectHandler(ERROR.IO, ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    // 예상치 못한 모든 예외
                    Debug.LogError($"예상치 못한 오류: {ex.GetType().Name} - {ex.Message}");
                    if (token.IsCancellationRequested)
                    {
                        m_stopToken.Cancel();
                        m_disconnectHandler(ERROR.Cancel, ex.Message);
                    }
                    break;
                }
            }
        }

        private async Task RunningRecv(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            int remainSize = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Debug.Assert(remainSize >= 0);//항상 양수여야 함
                    var ioByte = await m_stream.ReadAsync(buffer, remainSize, buffer.Length, token);
                    if (ioByte == 0)//리시브가 0이면 연결이 끊긴
                    {
                        m_stopToken.Cancel();
                        m_disconnectHandler(ERROR.IO, "Server Disconnect");
                        break;
                    }
                    var totalByte = ioByte + remainSize;
                    var processSize = 0;
                    while (totalByte - processSize >= PacketHeaderSize)//패킷의 사이즈를 알 수 있는 패킷 헤더가 완전히 존재해야합니다
                    {
                        ushort packetSize = (ushort)((buffer[processSize] << 8) | buffer[processSize + 1]);//빅엔디안으로 들어옴
                        Debug.Log($"PacketSize: {packetSize}");//테스트만하고 삭제
                        if (packetSize <= totalByte - processSize)//현재 완료 가능한 패킷의 사이즈가 총 인풋보다 작은 경우만 완성된 패킷이 됩니다
                        {
                            var payloadSize = packetSize - PacketHeaderSize;
                            byte[] packet = new byte[payloadSize];
                            Array.Copy(buffer, processSize + 2, packet, 0, payloadSize); // 헤더 2바이트 건너뜀
                            OnRecv(packet, payloadSize);//완성된 패킷에 대한 호출
                            processSize += (packetSize);//헤더 + msgId + payload 크기 만큼 미루기
                        }
                        else
                        {
                            break;
                        }
                    }
                    remainSize = totalByte - processSize;
                    Array.Copy(buffer, processSize, buffer, 0, remainSize);
                }
                catch (SocketException e)
                {
                    m_stopToken.Cancel();
                    m_disconnectHandler(ERROR.Socket, e.Message);
                    break;
                }
                catch (IOException e)
                {
                    m_stopToken.Cancel();
                    m_disconnectHandler(ERROR.IO, e.Message);
                    break;
                }
                catch (Exception e)
                {
                    if (token.IsCancellationRequested)
                    {
                        m_stopToken.Cancel();
                        m_disconnectHandler(ERROR.Cancel, e.Message);
                    }
                    break;
                }
            }
        }

        private void OnRecv(byte[] data, int len)
        {
            //data: msgId + payload
            UInt32 packetType = 0;
            if (BitConverter.IsLittleEndian)
            {
                packetType = ((uint)data[0] << 24) |
                             ((uint)data[1] << 16) |
                             ((uint)data[2] << 8) |
                             ((uint)data[3]);
            }


            Action<byte[], int, int> handler = null;
            if ((m_type & ServiceType.Common) != ServiceType.None)
            {
                handler = NetworkManager.Shared.GetDispatcher(ServiceType.Common, (PacketType)packetType);
            }
            if ((m_type & ServiceType.Game) != ServiceType.None)
            {
                handler = NetworkManager.Shared.GetDispatcher(ServiceType.Game, (PacketType)packetType);
            }
            if ((m_type & ServiceType.Login) != ServiceType.None)
            {
                handler = NetworkManager.Shared.GetDispatcher(ServiceType.Login, (PacketType)packetType);
            }
#if UNITY_EDITOR
            if ((m_type & ServiceType.Cheat) != ServiceType.None)
            {
                handler = NetworkManager.Shared.GetDispatcher(ServiceType.Cheat, (PacketType)packetType);
            }
#endif
            Debug.Assert(handler != null);
            if (handler == null)
            {
                Debug.Log("Not Exist Handler");
            }
            handler(data, MsgIdSize, len - MsgIdSize);// header 크기만큼이 오프셋
        }

        public void Send<ProtoT>(PacketType msgId, ProtoT data) where ProtoT : Google.Protobuf.IMessage
        {
            var serData = data.ToByteArray();

            m_sendContext.Send((UInt32)msgId, serData, (UInt16)data.CalculateSize());
        }
    }
}

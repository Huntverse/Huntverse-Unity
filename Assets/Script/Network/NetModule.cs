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
            //CL = Common | Login,
            //CG = Common | Game,
            //CLG = Common | Login | Game,
            //CLC = CL | Cheat,
            //CGC = CG | Cheat,
            //CLGC = CLG | Cheat,
        }

        private TcpClient m_tcpClient;
        private SendContext m_sendContext;

        private readonly Action<SocketException>? m_connFailHandler;
        private readonly Action? m_connSuccessHandler;
        private readonly Action<NetModule.ERROR, string>? m_disconnectHandler;//서버로부터 연결이 끊긴 경우

        private NetworkStream m_stream;
        private CancellationTokenSource m_stopToken;
        private ServiceType m_type;

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
            m_stream = m_tcpClient.GetStream();
            _ = RunningSend(m_stopToken.Token);
            _ = RunningRecv(m_stopToken.Token);
        }

        private async Task RunningSend(CancellationToken token)
        {
            while (!token.IsCancellationRequested)//진행해도 되는지 확인
            {
                byte[] sendData = m_sendContext.GetSendAbleData();
                if (sendData.Length == 0)
                {
                    await Task.Delay(50, token);///공회전 막고 싶은데, 이거 나중에 atomic으로 최적화하자
                    continue;
                }
                try
                {
                    await m_stream.WriteAsync(sendData);
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
                    if (!token.IsCancellationRequested)
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
                    while (totalByte - processSize >= 2)
                    {
                        ushort packetSize = (ushort)((buffer[processSize] << 8) | buffer[processSize + 1]);//빅엔디안으로 들어옴
                        Debug.Log($"PacketSize: {packetSize}");//테스트만하고 삭제
                        if (packetSize + 2 <= totalByte - processSize)//현재 완료 가능한 패킷의 사이즈가 총 인풋보다 작은 경우만 완성된 패킷이 됩니다
                        {
                            byte[] packet = new byte[packetSize];
                            Array.Copy(buffer, processSize + 2, packet, 0, packetSize); // 헤더 2바이트 건너뜀
                            OnRecv(packet, packetSize);//완성된 패킷에 대한 호출
                            processSize += (packetSize + 2);//헤더 + msgId + payload 크기 만큼 미루기
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
                    if (!token.IsCancellationRequested)
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

            PacketHeader header = PacketHeader.Parser.ParseFrom(data); //Msg Type to MessageId
            //handler(): Message to Call ParseFrom(), Proccess To Logic

            if ((m_type & ServiceType.Common) != ServiceType.None)
            {
                Debug.Assert(CommonMsgDispacher.Shared != null);
                var handler = CommonMsgDispacher.Shared.Gethandler(header.Type);
                handler(data, header.CalculateSize(), len);// header 크기만큼이 오프셋
                return;
            }
            if ((m_type & ServiceType.Game) != ServiceType.None)
            {
                Debug.Assert(GameMsgDispatcher.Shared != null);
                var handler = GameMsgDispatcher.Shared.Gethandler(header.Type);
                handler(data, header.CalculateSize(), len);// header 크기만큼이 오프셋
                return;
            }
            if ((m_type & ServiceType.Login) != ServiceType.None)
            {
                Debug.Assert(LoginMsgDispatcher.Shared != null);
                var handler = LoginMsgDispatcher.Shared.Gethandler(header.Type);
                handler(data, header.CalculateSize(), len);// header 크기만큼이 오프셋
                return;
            }
#if UNITY_EDITOR
            if ((m_type & ServiceType.Cheat) != ServiceType.None)
            {
                Debug.Assert(CheatMsgDispacher.Shared != null);
                var handler = CheatMsgDispacher.Shared.Gethandler(header.Type);
                handler(data, header.CalculateSize(), len);// header 크기만큼이 오프셋
                return;
            }
#endif
            Debug.Assert(false, "Not exist Handler!!");
        }

        public void Send<ProtoT>(PacketType msgId, ProtoT data) where ProtoT : Google.Protobuf.IMessage
        {
            PacketHeader header = new PacketHeader();
            header.Type = msgId;
            var serHead = header.ToByteArray();

            var serData = data.ToByteArray();

            m_sendContext.Send(serHead, (UInt16)serHead.Length, serData, (UInt16)data.CalculateSize());
        }
    }
}

/*
 private void RecvThread()
        {
            try
            {
                NetworkStream stream = m_tcpClient.GetStream();
                byte[] buffer = new byte[4096];
                //stream.ReadAsync();
                while (m_recvRunning)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length); // 여기서 블로킹

                    if (bytesRead == 0)
                    {
                        // 서버가 소켓 정리 (정상 종료)
                        m_disconnectHandler();
                        break;
                    }

                    // 받은 데이터 처리
                    OnRecv(buffer, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RecvThread 예외: {ex.Message}");
            }
            finally
            {
                m_recvRunning = false;
            }
        }

        private void SendThread()
        {
            try
            {
                NetworkStream stream = m_tcpClient.GetStream();
                while (m_sendRunning)
                {
                    var sendData = m_sendContext.GetSendData();//한번에 모아서 Send 합니다.
                    stream.Write(sendData, 0, sendData.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"SendThread 예외: {ex.Message}");
                m_disconnectHandler();//Send도 연결이 끊겼을 때, 알아챌 수 있지 않나?
            }
            finally
            {
                m_sendRunning = false;
            }
        } 
 */
using Hunt.Login;
using Hunt.Net;
using System;
using System.Net.Sockets;
using UnityEngine;

namespace Hunt
{
    /// <summary>
    /// 인증 관련 네트워크 요청/응답을 처리하는 핸들러
    /// NetworkManager를 주입받아 사용 (테스트 및 확장성 향상)
    /// </summary>
    public class AuthReqHandler
    {
        private readonly NetworkManager networkManager;
        public static event Action<LoginAns> OnLoginResponse;
        public AuthReqHandler(NetworkManager networkManager = null)
        {
            this.networkManager = networkManager ?? NetworkManager.Shared;
        }

        /// <summary> 로그인 응답을 처리하고 이벤트 발생 (MsgDispatcher에서 호출) </summary>
        public static void NotifyLoginResponse(LoginAns ans)
        {
            $"[AuthReqHandler] 로그인 응답 수신: {ans.ErrType}".DLog();
            OnLoginResponse?.Invoke(ans);
        }

        public void ReqAuthVaild(string id, string pw)
        {
            var req = new LoginReq { Id = id, Pw = pw };
            $"[AuthReqHandler] 로그인 요청: ID={id}".DLog();
            networkManager.SendToLogin(Hunt.Common.MsgId.LoginReq, req);
        }

        public void ReqCreateAuthVaild(string id, string pw)
        {
            // TODO: CreateAccountReq 구현 후 활성화
            var req = new CreateAccountReq { Id = id, Pw = pw };
            networkManager.SendToLogin(Hunt.Common.MsgId.CreateAccountReq, req);
            $"[AuthReqHandler] 계정 생성 요청: ID={id}".DLog();
        }

        public void ReqIdDuplicate(string id)
        {
            // TODO: IdDuplicateReq 구현 후 활성화
            var req = new ConfirmIdReq{ Id = id };
           networkManager.SendToLogin(Hunt.Common.MsgId.ConfirmIdReq, req);
            $"[AuthReqHandler] 아이디 중복확인 요청: ID={id}".DLog();
        }

        /// <summary>
        /// 캐릭터 생성 시 닉네임 중복 체크
        /// </summary>
        public void ReqNicknameDuplicate(string nickname)
        {
            // TODO: NicknameDuplicateReq 구현 후 활성화
            var req = new ConfirmNameReq{ Name = nickname };
            networkManager.SendToLogin(Hunt.Common.MsgId.ConfirmNameReq, req);
            $"[AuthReqHandler] 닉네임 중복확인 요청: Nickname={nickname}".DLog();
        }

        /// <summary>
        /// 로그인 서버에 연결 시도
        /// </summary>
        /// <param name="onSuccess">연결 성공 콜백</param>
        /// <param name="onFail">연결 실패 콜백</param>
        /// <returns>연결 성공 여부</returns>
        public bool ConnectToServer(Action onSuccess, Action<SocketException> onFail)
        {
            $"[AuthReqHandler] 서버 연결 시도".DLog();
            bool connected = networkManager.ConnLoginServerSync(
                (e, msg) => { $"[AuthReqHandler] 연결 끊김: {msg}".DLog(); },
                onSuccess,
                onFail
            );

            if (connected)
            {
                StartServer();
            }

            return connected;
        }

        /// <summary> 서버 통신 시작 (연결 성공 후 호출) </summary>
        public void StartServer()
        {
            $"[AuthReqHandler] 서버 통신 시작".DLog();
            networkManager.StartLoginServer();
        }

        /// <summary> 서버 연결 해제 </summary>
        public void DisConnectToServer()
        {
            $"[AuthReqHandler] 서버 연결 해제".DLog();
            networkManager.DisConnLoginServer();
        }
    }
}

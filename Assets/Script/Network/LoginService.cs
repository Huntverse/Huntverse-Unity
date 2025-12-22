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
    public class LoginService
    {
        private readonly NetworkManager networkManager;
        public static event Action<LoginAns> OnLoginResponse;
        public LoginService(NetworkManager networkManager = null)
        {
            this.networkManager = networkManager ?? NetworkManager.Shared;
        }

        /// <summary> 로그인 응답을 처리하고 이벤트 발생 (MsgDispatcher에서 호출) </summary>
        public static void NotifyLoginResponse(LoginAns ans)
        {
            $"[LoginService] 로그인 응답 수신: {ans.ErrType}".DLog();
            OnLoginResponse?.Invoke(ans);
        }

        public void ReqAuthVaild(string id, string pw)
        {
            var req = new LoginReq { Id = id, Pw = pw };
            $"[LoginService] 로그인 요청: ID={id}".DLog();
            networkManager.SendToLogin(Hunt.Common.MsgId.LoginReq, req);
        }

        public void ReqCreateAuthVaild(string id, string pw)
        {
            var req = new CreateAccountReq { Id = id, Pw = pw };
            networkManager.SendToLogin(Hunt.Common.MsgId.CreateAccountReq, req);
            $"[LoginService] 계정 생성 요청: ID={id}".DLog();
        }

        public void ReqIdDuplicate(string id)
        {
            var req = new ConfirmIdReq{ Id = id };
           networkManager.SendToLogin(Hunt.Common.MsgId.ConfirmIdReq, req);
            $"[LoginService] 아이디 중복확인 요청: ID={id}".DLog();
        }

        /// <summary>
        /// 캐릭터 생성 시 닉네임 중복 체크
        /// </summary>
        public void ReqNicknameDuplicate(string nickname)
        {
            var req = new ConfirmNameReq{ Name = nickname };
            networkManager.SendToLogin(Hunt.Common.MsgId.ConfirmNameReq, req);
            $"[LoginService] 닉네임 중복확인 요청: Nickname={nickname}".DLog();
        }
    }
}

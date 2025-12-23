using Hunt.Common;
using Hunt.Login;
using Hunt.Net;
using System;

namespace Hunt
{
    /// <summary>
    /// 인증 관련 네트워크 요청/응답을 처리하는 핸들러
    /// NetworkManager를 주입받아 사용 (테스트 및 확장성 향상)
    /// </summary>
    public class LoginService
    {
        private readonly NetworkManager networkManager;
        public static event Action<ErrorType> OnLoginResponse;
        public static event Action<ErrorType> OnCreateCharResponse;
        public LoginService(NetworkManager networkManager = null)
        {
            this.networkManager = networkManager ?? NetworkManager.Shared;
        }

        /// <summary> 로그인 응답을 처리하고 이벤트 발생 (MsgDispatcher에서 호출) </summary>
        public static void NotifyLoginResponse(ErrorType t)
        {
            $"[LoginService] 계정 응답 수신: {t}".DLog();
            OnLoginResponse?.Invoke(t);
            
        }
        public static void NotifyCreateCharResponse(ErrorType t)
        {
            $"[LoginService] 캐릭터 이름 응답 수신: {t}".DLog();
            OnCreateCharResponse?.Invoke(t);
        }

        public void ReqAuthVaild(string id, string pw)
        {
            var req = new LoginReq { Id = id, Pw = pw };
            $"[LoginService] 로그인 요청: ID={id} PW={pw}".DLog();
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
        public void ReqCreateChar(string nickname)
        {
            var req = new CreateCharReq { Name = nickname };
            networkManager.SendToLogin(Hunt.Common.MsgId.CreateCharReq, req);
            $"[LoginService] 캐릭터 생성 요청: Nickname={nickname}".DLog();
        }

    }
}

using UnityEngine;

namespace Hunt
{
    public enum AUTH_NOTI_TYPE
    {
        SERVER_CON_SUCCESS,SERVER_CON_FAIL,
        FAIL_PW_DUP, FAIL_INPUT, FAIL_ID_EXIST, FAIL_VAILD,
        SUCCESS_VAILD, SUCCESS_ID_EXIST
    }

    public static class NotiConst
    {
        public static readonly Color COLOR_WARNNING = Hex("CC8E8E");
        public static readonly Color COLOR_SUCCESS = Hex("83DB4E");
        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out var color);
            return color;
        }

        public static string GetAuthNotiMsg(AUTH_NOTI_TYPE type)
        {
            return type switch
            {
                AUTH_NOTI_TYPE.FAIL_PW_DUP => "비밀번호가 틀렸습니다. 다시 한 번 확인해 주세요.",
                AUTH_NOTI_TYPE.FAIL_INPUT=> "특수문자(#, -, 공백)는 사용할 수 없습니다.",
                AUTH_NOTI_TYPE.FAIL_ID_EXIST => "이미 사용 중인 아이디입니다.",
                AUTH_NOTI_TYPE.FAIL_VAILD => "해당 계정을 찾을 수 없습니다.",
                AUTH_NOTI_TYPE.SUCCESS_VAILD => "환영합니다, 헌터님.",
                AUTH_NOTI_TYPE.SUCCESS_ID_EXIST => "사용가능한 아이디입니다.",
                AUTH_NOTI_TYPE.SERVER_CON_FAIL=>"서버 연결에 실패했습니다.",
                AUTH_NOTI_TYPE.SERVER_CON_SUCCESS=> "서버 연결에 성공했습니다.",
                _ => string.Empty
            };
        }
    }
}

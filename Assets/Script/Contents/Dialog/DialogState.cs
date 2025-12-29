using UnityEngine;

namespace Hunt
{
    public enum DialogState
    {
        None,               // -
        Typing,             // 텍스트 타이핑 중
        WaitingForIput,     // 확인/다음 입력 대기
        ShowingChoices,     // 선택지 표시 중
        ProcessingChoice,   // 선택지 처리 중
        Completed           // 대화 종료
    }
}

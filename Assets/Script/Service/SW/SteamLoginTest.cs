using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>
/// Steam 로그인 정보를 테스트하기 위한 스크립트
/// SteamManager가 초기화되면 자동으로 Steam 사용자 정보를 가져와서 로그에 출력합니다.
/// </summary>
public class SteamLoginTest : MonoBehaviour
{
#if !DISABLESTEAMWORKS
    private bool hasLoggedInfo = false;

    private void Start()
    {
        Debug.Log("[SteamLoginTest] 테스트 시작");
    }

    private void Update()
    {
        // SteamManager가 초기화되고, 아직 정보를 출력하지 않았다면
        if (SteamManager.Initialized && !hasLoggedInfo)
        {
            LogSteamUserInfo();
            hasLoggedInfo = true;
        }
    }

    private void LogSteamUserInfo()
    {
        try
        {
            // Steam ID 가져오기
            CSteamID steamID = SteamUser.GetSteamID();
            Debug.Log($"[SteamLoginTest] ✓ Steam 로그인 성공!");
            Debug.Log($"[SteamLoginTest] Steam ID: {steamID.m_SteamID}");

            // Steam 이름 가져오기
            string personaName = SteamFriends.GetPersonaName();
            Debug.Log($"[SteamLoginTest] Steam 이름: {personaName}");

            // 계정 레벨 가져오기
            int level = SteamUser.GetPlayerSteamLevel();
            Debug.Log($"[SteamLoginTest] Steam 레벨: {level}");

            // 추가 정보
            Debug.Log($"[SteamLoginTest] 앱 소유 여부: {SteamApps.BIsSubscribed()}");
            Debug.Log($"[SteamLoginTest] VAC 밴 여부: {SteamUser.BIsBehindNAT()}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SteamLoginTest] ✗ Steam 정보 가져오기 실패: {e.Message}");
        }
    }
#else
    private void Start()
    {
        Debug.LogWarning("[SteamLoginTest] Steamworks가 비활성화되어 있습니다.");
    }
#endif
}

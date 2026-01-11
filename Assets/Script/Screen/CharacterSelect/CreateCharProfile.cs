using Cysharp.Threading.Tasks;
using Hunt.Common;
using Hunt.Game;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class CreateCharProfile : MonoBehaviour
    {
        [Header("CREATWINDOW")]
        [SerializeField] private GameObject createWindow;
        [SerializeField] private Button createButton;
        [SerializeField] private Button dupNickButton;
        [SerializeField] private TextMeshProUGUI createcharVaildText;
        [SerializeField] private TMP_InputField nickNameField;

        [Header("SETUP")]
        private ClassType professionType;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI characterNameText;
        public Image PortraitImage => portraitImage;
        public ClassType ProfessionType => professionType;
        public string characterName => BindKeyConst.GetProfessionMatchName(professionType);
        public List<StatInfo> stats = new List<StatInfo>();
        public string charStory;

        /// <summary>
        /// professionType이 변경될 때 UI를 업데이트합니다.
        /// </summary>
        public void UpdateCharacterNameUI()
        {
            if (characterNameText != null)
            {
                characterNameText.text = ConvertToVerticalText(characterName);
            }
        }

        /// <summary>
        /// 텍스트를 세로로 표시하기 위해 각 글자 사이에 줄바꿈을 추가합니다.
        /// </summary>
        private string ConvertToVerticalText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            return string.Join("\n", text.ToCharArray());
        }

        public void SetProfession(ClassType profession)
        {
            professionType = profession;
        }
        private void OnEnable()
        {
            dupNickButton.onClick.AddListener(() => ReqNickNameDuplicate());
            createButton.onClick.AddListener(() => ReqCreateChar());
            LoginService.OnCreateCharResponse += HandleNotiCreateCharResponse;
            createWindow.SetActive(false);
        }
        private void OnDisable()
        {
            dupNickButton.onClick.RemoveListener(() => ReqNickNameDuplicate());
            createButton.onClick.RemoveListener(() => ReqCreateChar());
            LoginService.OnCreateCharResponse -= HandleNotiCreateCharResponse;
            
        }
        public void OnClickCreateCharacter()
        {
            CharacterSetupController.Shared.OnCreateNewCharacter(this.professionType);
        }

        /// <summary> Request Server : Duplicate ID </summary>
        private void ReqNickNameDuplicate()
        {
            var nickName = nickNameField.text;
            if (!IsValid(nickName, createcharVaildText))
            {
                return;
            }
            GameSession.Shared?.LoginService.ReqNicknameDuplicate(nickName);
        }
        private void ReqCreateChar()
        {
            var nickName = nickNameField.text;
            GameSession.Shared?.LoginService.ReqCreateChar(nickName);

            OnClickCreateCharacter();
        }

        private bool IsValid(string nickName, TextMeshProUGUI vaildText)
        {
            char[] invalidChars = { '-', '#', ' ' };

            bool isValid = !nickName.IsNullOrEmpty() && nickName.IndexOfAny(invalidChars) == -1;

            vaildText.gameObject.SetActive(true);

            if (!isValid)
            {
                ShowNotificationText(
                           vaildText,
                           NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.FAIL_INPUT),
                           NotiConst.COLOR_WARNNING);
                $"Field Value Is Valid {false}".DError();
                return false;
            }

            return isValid;
        }

        private void HandleNotiCreateCharResponse(ErrorType t)
        {
            switch (t)
            {
                case Common.ErrorType.ErrNon:
                    ShowNotificationText(
                    createcharVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SUCCESS_DUP_NICK),
                    NotiConst.COLOR_SUCCESS);
                    break;
                case Common.ErrorType.ErrDupNickName:
                    ShowNotificationText(
                    createcharVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.DUP_NICK),
                    NotiConst.COLOR_WARNNING);
                    break;
            }

        }
        #region Effect
        private Coroutine currentFadeCoroutine;
        private void ShowNotificationText(TextMeshProUGUI textUI, string message, Color color)
        {
            if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);

            currentFadeCoroutine = StartCoroutine(CO_FadeText(textUI, message, color));
        }

        private IEnumerator CO_FadeText(TextMeshProUGUI textUI, string message, Color color)
        {
            textUI.text = message;
            textUI.color = color;
            textUI.gameObject.SetActive(true);

            // Fade In
            float a = 0f;
            while (a < 1f)
            {
                a += Time.deltaTime * 3f;
                textUI.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }

            yield return new WaitForSeconds(2f);

            while (a > 0f)
            {
                a -= Time.deltaTime * 3f;
                textUI.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }

            textUI.gameObject.SetActive(false);
        }
        #endregion
    }
}

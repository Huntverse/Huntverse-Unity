using Hunt.Login;
using Hunt.Net;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hunt
{
    public class LoginScreen : MonoBehaviour
    {
        #region Field
        [SerializeField] private TMP_InputField idInput, pwInput;
        [SerializeField] private TMP_InputField new_idInput, new_pwInput, new_pwDupInput;

        [Header("LOGIN")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button pwVisButton;
        [SerializeField] private GameObject caplockVis;
        [SerializeField] private TextMeshProUGUI loginVaildText;

        [Header("CREATE")]
        [SerializeField] private Button createConfirmButton;  // 생성창 내부 버튼 (로그인창 버튼x)
        [SerializeField] private GameObject createPanel;
        [SerializeField] private TextMeshProUGUI createVaildText;
        [SerializeField] private GameObject createCaplockVis;
        [SerializeField] private Button new_pwVisButton;
        [SerializeField] private Button id_DupButton;

        [Header("ANIMATION")]
        [SerializeField] private Animator animator;

        private class InputContext
        {
            public TMP_InputField IdField;
            public TMP_InputField PwField;
            public TextMeshProUGUI VaildText;
            public GameObject Capslock;
        }

        private InputContext GetCurrentContext()
        {
            if (createPanel.activeSelf)
            {
                return new InputContext
                {
                    IdField = new_idInput,
                    PwField = new_pwInput,
                    VaildText = createVaildText,
                    Capslock = createCaplockVis
                };
            }

            return new InputContext
            {
                IdField = idInput,
                PwField = pwInput,
                VaildText = loginVaildText,
                Capslock = caplockVis
            };
        }

        private bool isPasswordVisible = false;
        private bool isCreatePasswordVisible = false;

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int keyCode);
        private const int VK_CAPITAL = 0x14;
        #endregion
        private LoginService loginService;
        #region Life
        private void Start()
        {
            loginService = GameSession.Shared?.LoginService;

            confirmButton.onClick.AddListener(ReqAuthVaild);
            pwVisButton.onClick.AddListener(() => TogglePasswordVisibility(false));

            createConfirmButton.onClick.AddListener(ReqCreateAuthVaild);
            new_pwVisButton.onClick.AddListener(() => TogglePasswordVisibility(true));

            id_DupButton.onClick.AddListener(ReqIdDuplicate);

            idInput.onSubmit.AddListener(OnIdSubmit);
            pwInput.onSubmit.AddListener(OnPwSubmit);

            new_idInput.onSubmit.AddListener(OnIdSubmit);
            new_pwInput.onSubmit.AddListener(OnPwSubmit);

            idInput.Select();
            idInput.ActivateInputField();

            createVaildText.text = "";
            loginVaildText.text = "";

            GameSession.Shared.OnLoginResponse += HandleNotiLoginResponse;
        }
        private void Update()
        {
            HandleKeyInput();
        }
        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(ReqAuthVaild);
            createConfirmButton.onClick.RemoveListener(ReqCreateAuthVaild);
            id_DupButton.onClick.RemoveListener(ReqIdDuplicate);
            idInput.onSubmit.RemoveListener(OnIdSubmit);
            pwInput.onSubmit.RemoveListener(OnPwSubmit);
            new_idInput.onSubmit.RemoveListener(OnIdSubmit);
            new_pwInput.onSubmit.RemoveListener(OnPwSubmit);
            GameSession.Shared.OnLoginResponse -= HandleNotiLoginResponse;
        }
        #endregion
        #region INPUT

        private void HandleKeyInput()
        {
            var key = Keyboard.current;
            if (key == null) return;

            var context = GetCurrentContext();
            //context.VaildText?.gameObject.SetActive(false);

            if (key.tabKey.wasPressedThisFrame && context.IdField.isFocused)
            {
                context.PwField?.Select();
                context.PwField?.ActivateInputField();
            }

            bool isCapsLockOn = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
            context.Capslock?.SetActive(isCapsLockOn);
        }

        private void TogglePasswordVisibility(bool isCreatePanel)
        {
            if (isCreatePanel)
            {
                isCreatePasswordVisible = !isCreatePasswordVisible;
                new_pwInput.contentType = isCreatePasswordVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                new_pwDupInput.contentType = new_pwInput.contentType;

                new_pwInput.ForceLabelUpdate();
                new_pwDupInput.ForceLabelUpdate();
            }
            else
            {
                isPasswordVisible = !isPasswordVisible;
                pwInput.contentType = isPasswordVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                pwInput.ForceLabelUpdate();
            }
        }
        private void OnIdSubmit(string _)
        {
            var context = GetCurrentContext();
            context.PwField.Select();
            context.PwField.ActivateInputField();
        }
        private void OnPwSubmit(string _)
        {
            if (createPanel.activeSelf)
            {
                ReqCreateAuthVaild();
            }
            else
            {
                ReqAuthVaild();
            }
        }
        #endregion
        #region REQUEST

        /// <summary> Request Server : Duplicate ID </summary>
        private void ReqIdDuplicate()
        {
            var id = new_idInput.text; 
            if (string.IsNullOrEmpty(id))
            {
                ShowNotificationText(
                    createVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.FAIL_INPUT),
                    NotiConst.COLOR_WARNNING
                );
                return;
            }

            loginService.ReqIdDuplicate(id);
        }

        /// <summary> Request Server : Vaild Auth </summary>
        private void ReqAuthVaild()
        {
            var (id, pw) = VaildateAndReturnResult(idInput, pwInput, loginVaildText, true);
            loginService.ReqAuthVaild(id, pw);
        }

        /// <summary> Request Server : Create Auth </summary>
        private void ReqCreateAuthVaild()
        {
            // UI 검증
            var (id, pw) = VaildateAndReturnResult(new_idInput, new_pwInput, createVaildText);
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
            {
                return;
            }

            if (!IsVaildSyncPassWord())
            {
                $"비밀번호가 일치하지 않습니다.".DLog();
                return;
            }

            loginService.ReqCreateAuthVaild(id, pw);
        }
        private void HandleNotiLoginResponse(LoginAns ans)
        {
            if (ans.ErrType == Hunt.Common.ErrorType.ErrNon)
            {
                // 성공
                ShowNotificationText(
                    loginVaildText,
                    NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SUCCESS_VAILD),
                    NotiConst.COLOR_SUCCESS);
            }
            else
            {
                // 실패
                // string errorMsg = GetErrorMessage(ans.ErrType);
                ShowNotificationText(
                    loginVaildText,
                    ans.ErrType.ToString(),
                    NotiConst.COLOR_WARNNING);
            }
        }

        void OnConnectSuccess()
        {
            ShowNotificationText(
            loginVaildText,
            NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SERVER_CON_SUCCESS),
            NotiConst.COLOR_SUCCESS
            );
        }
        void OnConnectFail(SocketException e)
        {
            ShowNotificationText(
                loginVaildText,
                NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SERVER_CON_FAIL),
                NotiConst.COLOR_WARNNING
                );
        }
        #endregion
        #region VAILD
        private (string, string) VaildateAndReturnResult(
            TMP_InputField idField,
            TMP_InputField pwField,
            TextMeshProUGUI resultText,
            bool isAni = false)
        {
            if (!IsValid(idField.text, pwField.text, resultText, isAni))
            {
                return default;
            }

            return (idField.text, pwField.text);
        }

        private bool IsValid(string id, string pw, TextMeshProUGUI vaildText, bool isAni)
        {
            char[] invalidChars = { '-', '#', ' ' };

            bool isValid = !id.IsNullOrEmpty()
                           && !pw.IsNullOrEmpty()
                           && id.IndexOfAny(invalidChars) == -1
                           && pw.IndexOfAny(invalidChars) == -1;

            vaildText.gameObject.SetActive(true);

            if (isValid)
            {
                vaildText.color = NotiConst.COLOR_SUCCESS;
                animator.SetBool(AniKeyConst.k_bValid, isAni);
                $"Field Value Is Valid {true}".DLog();
            }
            else
            {
                ShowNotificationText(
                vaildText,
                NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.FAIL_INPUT),
                NotiConst.COLOR_WARNNING);
                animator.SetTrigger(AniKeyConst.k_tFail);
                $"Field Value Is Valid {false}".DError();
            }

            return isValid;
        }

        private bool IsVaildSyncPassWord()
        {
            var vaild = new_pwInput.text == new_pwDupInput.text ? true : false;
            if (!vaild)
                ShowNotificationText(
                createVaildText,
                NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.FAIL_PW_DUP),
                NotiConst.COLOR_WARNNING);

            return vaild;
        }

        #endregion
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
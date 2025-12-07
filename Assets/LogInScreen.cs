using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hunt
{
    public class LogInScreen : MonoBehaviour
    {

        #region Field
        [SerializeField] private TMP_InputField idInput;
        [SerializeField] private TMP_InputField pwInput;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button pwVisButton;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject caplockVis;
        private bool isPasswordVisible = false;
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int keyCode);
        private const int VK_CAPITAL = 0x14;
        #endregion

        private void Start()
        {

            confirmButton.onClick.AddListener(ReqAuthVaild);
            pwVisButton.onClick.AddListener(TogglePasswordVisibility);
            idInput.onSubmit.AddListener(OnIdSubmit);
            pwInput.onSubmit.AddListener(OnPwSubmit);

            idInput.Select();
            idInput.ActivateInputField();

        }
        private void Update()
        {
            HandleKeyInput();
        }
        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(ReqAuthVaild);

            idInput.onSubmit.RemoveListener(OnIdSubmit);
            pwInput.onSubmit.RemoveListener(OnPwSubmit);
        }
        private void HandleKeyInput()
        {
            var key = Keyboard.current;
            if (key == null) return;

            if (key.tabKey.wasPressedThisFrame)
            {
                if (idInput.isFocused)
                {
                    pwInput.Select();
                    pwInput.ActivateInputField();
                }
            }

            bool isCapsLockOn = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
            caplockVis.SetActive(isCapsLockOn);

        }

        private void TogglePasswordVisibility()
        {
            isPasswordVisible = !isPasswordVisible;

            pwInput.contentType =  isPasswordVisible? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
            pwInput.ForceLabelUpdate();
        }
        private void OnIdSubmit(string _)
        {
            pwInput.Select();
            pwInput.ActivateInputField();
        }
        private void OnPwSubmit(string _)
        {
            ReqAuthVaild();
        }

        private void ReqAuthVaild()
        {
            var (id, pw) = ReturnInputResult();

            // 클라에서 검증된 id, pw 서버로 요청
        }

        private (string, string) ReturnInputResult()
        {
            // 클라에서 valid 필러팅 1차 (-,#,공백)
            if (!IsValid(idInput.text, pwInput.text))
            {
                return default;
            }
            else
            {
                return (idInput.text, pwInput.text);
            }

        }

        private bool IsValid(string id, string pw)
        {
            char[] invalidChars = { '-', '#', ' ' };

            bool isValid = !id.IsNullOrEmpty()
                           && !pw.IsNullOrEmpty()
                           && id.IndexOfAny(invalidChars) == -1
                           && pw.IndexOfAny(invalidChars) == -1;

            if (isValid)
            {
                animator.SetBool(AniKeyConst.k_bValid, true); 
                $"Field Value Is Valid {true}".DLog();
            }
            else
            {
                animator.SetTrigger(AniKeyConst.k_tFail);
                $"Field Value Is Valid {false}".DError();
            }

            return isValid;
        }



    }

}
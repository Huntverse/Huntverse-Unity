using Mirror;
using Mirror.BouncyCastle.Security;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hunt
{
    public class LogInScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField idInput;
        [SerializeField] private TMP_InputField pwInput;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject caplockVisual;
        
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int keyCode);
        private const int VK_CAPITAL = 0x14;
        private void Start()
        {

            confirmButton.onClick.AddListener(ReqAuthVaild);

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
            caplockVisual.SetActive(isCapsLockOn);

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
            if (id.Contains('-') || id.Contains("#") || id.Contains(' ')
                || pw.Contains('-') || pw.Contains("#") || pw.Contains(' ')
                || id.IsNullOrEmpty() || pw.IsNullOrEmpty())
            {
                $"Field Value Is Valid {false}".DError();
                animator.SetTrigger(AniKeyConst.k_tFail);
                return false;
            }
            $"Field Value Is Valid {true}".DLog();
            return true;
        }



    }

}
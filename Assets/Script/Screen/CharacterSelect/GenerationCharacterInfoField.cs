using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Hunt
{
    public class GenerationCharacterInfoField : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_InputField idField;
        public ClassType professionType;
        public string characterName => BindKeyConst.GetProfessionMatchName(professionType);
        public List<float> stats = new List<float>(5);
        public string storyString;
        private void OnEnable()
        {
            selectButton.onClick.AddListener(() => ReqIdDuplicate());
        }
        private void OnDisable()
        {
            selectButton.onClick.RemoveListener(() => ReqIdDuplicate());

        }
        public void OnClickCreateCharacter()
        {
            CharacterCreateController.Shared.OnCreateNewCharacter(this.professionType);
        }

        /// <summary> Request Server : Duplicate ID </summary>
        private void ReqIdDuplicate()
        {
            var id = idField.text;
            // createVaildText.text = NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.FAIL_ID_EXIST);
            // createVaildText.text = NotiConst.GetAuthNotiMsg(AUTH_NOTI_TYPE.SUCCESS_ID_EXIST);
            $"아이디 중복확인 요청".DLog();

            OnClickCreateCharacter();
        }

    }
}

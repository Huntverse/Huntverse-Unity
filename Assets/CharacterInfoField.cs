using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hunt
{
    public class CharacterInfoField : MonoBehaviour
    {
        private bool isCreated = false;

        [SerializeField] private GameObject createPannel;
        [SerializeField] private GameObject userInfoPannel;

        [SerializeField] private Image professionIcon;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI nameText;

        public int Level
        {
            get => int.Parse(levelText.text);
            set => levelText.text = value.ToString();
            
        }
        public string Name
        {
            get => nameText.text;
            set => nameText.text = value;
        }
        private void Awake()
        {
            
        }
        public void InitField(bool iscreated)
        {
            isCreated = iscreated;

            createPannel.SetActive(!isCreated);
            userInfoPannel.SetActive(isCreated);
        }

        public void SetLevelFieldValue(int level) => Level = level;
        public void SetNameFieldValue(string name) => Name = name;

        public void Bind(CharacterModel model)
        {
            var created = model?.IsCreated == true;
            InitField(created);

            if (!created)
            {
                SetLevelFieldValue(0);
                SetNameFieldValue(string.Empty);
                if (professionIcon != null)
                {
                    professionIcon.sprite = null;
                    professionIcon.enabled = false;
                }
                return;
            }

            SetLevelFieldValue(model.level);
            SetNameFieldValue(model.name);
            if (professionIcon != null)
            {
                if (model.icon != null)
                {
                    professionIcon.sprite = model.icon;
                    professionIcon.enabled = true;
                }
                else
                {
                    professionIcon.enabled = false;
                }
            }
        }
    }
}
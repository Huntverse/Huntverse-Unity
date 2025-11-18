using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

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
        public void InitField(bool iscreated)
        {
            isCreated = iscreated;

            createPannel.SetActive(!isCreated);
            userInfoPannel.SetActive(isCreated);
        }

        public void SetLevelFieldValue(int level) => Level = level;
        public void SetNameFieldValue(string name) => Name = name;

        public async void Bind(CharacterModel model)
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
                await LoadProfessionIcon(model.profession);
            }
        }

        private async UniTask LoadProfessionIcon(ProfessionType profession)
        {
            if (AbLoader.Shared == null)
            {
                "🖼️ [CharacterInfoField] AbLoader.Shared is null".DError();
                return;
            }

            string iconKey = GetProfessionIconKey(profession);
            if (string.IsNullOrEmpty(iconKey))
            {
                professionIcon.enabled = false;
                return;
            }

            try
            {
                var sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(iconKey);
                if (sprite != null)
                {
                    professionIcon.sprite = sprite;
                    professionIcon.enabled = true;
                    $"🖼️ [CharacterInfoField] Icon loaded: {iconKey}".DLog();
                }
                else
                {
                    professionIcon.enabled = false;
                    $"🖼️ [CharacterInfoField] Failed to load icon: {iconKey}".DError();
                }
            }
            catch (System.Exception ex)
            {
                $"🖼️ [CharacterInfoField] Error loading icon: {ex.Message}".DError();
                professionIcon.enabled = false;
            }
        }

        private string GetProfessionIconKey(ProfessionType profession)
        {
            return profession switch
            {
                ProfessionType.Worrior => HuntKeyConst.Ks_Profession_Worrior,
                ProfessionType.Magician => HuntKeyConst.Ks_Profession_Magician,
                ProfessionType.Tanker => HuntKeyConst.Ks_Profession_Tanker,
                _ => string.Empty
            };
        }
    }
}
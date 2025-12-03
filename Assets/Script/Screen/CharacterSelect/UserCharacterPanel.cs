using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using TMPro;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

namespace Hunt
{
    public class UserCharacterPanel : MonoBehaviour
    {
        public CharacterPanelConfig config;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private PentagonBalanceUI balanceData;
        [SerializeField] private Image illustImg;
        [SerializeField] private TextMeshProUGUI savePointText;
        [SerializeField] private TextMeshProUGUI professionText;
        [SerializeField] private Button enterButton;
        private void Awake()
        {
            enterButton.onClick.AddListener(() => EnterVillage().Forget());
        }
        private async UniTask EnterVillage()
        {
            await SceneLoadHelper.Shared.LoadSceneAdditiveMode(ResourceKeyConst.Ks_Village);
        }
        private async UniTask sendMsg()
        {

        }
        private async UniTask recvMsg()
        {

        }
        public async UniTask HandleUpdateConfig(
            int level,
            string name,
            float[] stats,
            string illustKey,
            string savepoint=null,
            string characterProfession="")
        {
            gameObject.SetActive(false);
            gameObject.SetActive(true);
            config.Level= level;
            levelText.text = $"레벨 : "+config.Level.ToString();

            config.UserName= name;
            nameText.text = config.UserName.ToString();

            config.Stats = stats;
            if (balanceData != null)
            {
                balanceData.AnimateStatsFromZero(config.Stats[0], config.Stats[1], config.Stats[2], config.Stats[3], config.Stats[4], 1f);
            }

            config.Illust = illustKey;
            var illust = await AbLoader.Shared.LoadAssetAsync<Sprite>(illustKey);
            illustImg.sprite = illust;

            config.Savepoint= savepoint;
            savePointText.text = $"" + savepoint;

            config.Profession = characterProfession;
            professionText.text = $"직업 : " + config.Profession;

        }



    }

    public struct CharacterPanelConfig
    {
        public int Level;
        public string UserName;
        public float[] Stats;
        public string Illust;
        public string Savepoint;
        public string Profession;
    }

}
using NUnit.Framework;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace hunt
{
    public class GenerationCharacterInfoField : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        public ProfessionType professionType;
        public string characterName => BindKeyConst.GetProfessionMatchName(professionType);
        public List<float> stats = new List<float>(5);
        public string storyString;
        private void OnEnable()
        {
            selectButton.onClick.AddListener(() => OnClickCreateCharacter());
        }
        private void OnDisable()
        {
            selectButton.onClick.RemoveListener(() => OnClickCreateCharacter());

        }
        public void OnClickCreateCharacter()
        {
            CharacterCreateController.Shared.OnCreateNewCharacter(this.professionType);
        }
      

    }
}

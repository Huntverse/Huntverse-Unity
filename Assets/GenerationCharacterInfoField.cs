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
        [SerializeField] private ProfessionType professionType;
        [SerializeField] private string characterName;
        public List<float> stats = new List<float>(5);
        public string storyString;
        private void OnEnable()
        {
            selectButton.onClick.AddListener(() => OnClickField());
        }
        private void OnDisable()
        {
            selectButton.onClick.AddListener(() => OnClickField());

        }
        public void OnClickField()
        {
            CharacterCreateController.Shared.OnCreateNewCharacter(this.professionType);
        }

       
    }
}

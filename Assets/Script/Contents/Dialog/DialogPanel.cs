using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
using System;

namespace Hunt
{
    public class DialogPanel : UIControlBase
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private Image speakerIcon;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        private List<DialogChoiceButton> activeButtons = new List<DialogChoiceButton>();
        private StringBuilder dialogBuilder = new StringBuilder();

        public void Start()
        {
            Hide();
        }
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearChoices();
        }

        public void SetDialogText(string text)
        {

        }

        public void AppenDialogText(char c)
        {
            if (dialogText != null)
            {
                dialogBuilder.Append(c);
                dialogText.text = dialogBuilder.ToString();
            }
        }

        public void SetSpeakerIcon(Sprite sprite)
        {
            if (speakerIcon != null)
            {
                speakerIcon.sprite = sprite;
                speakerIcon.gameObject.SetActive(sprite != null);
            }
        }

        public void ShowChoices(List<DialogChoice> choices, Action<int> onChoiceClick)
        {
            ClearChoices();

            if (choices == null || choices.Count == 0) return;

            for (int i = 0; i < choices.Count; i++)
            {
                GameObject go = Instantiate(choiceButtonPrefab, choiceContainer);
                DialogChoiceButton btn = go.GetComponent<DialogChoiceButton>();

                if (btn != null)
                {
                    int index = i;
                    btn.SetUp(choices[i].choiceText, () => onChoiceClick?.Invoke(index));
                    activeButtons.Add(btn);
                }
            }
        }

        private void ClearChoices()
        {
            if (activeButtons.Count > 0)
            {
                foreach (var btn in activeButtons)
                {
                    if (btn != null)
                    {
                        Destroy(btn.gameObject);
                    }
                    activeButtons.Clear();
                }
            }
        }
    }
}

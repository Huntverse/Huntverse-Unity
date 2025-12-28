using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hunt
{
    public class DialogManager : MonoBehaviourSingleton<DialogManager>
    {
        [SerializeField] private DialogPanel dialogPanel;
        [SerializeField] private float typingSpeed = 0.05f;

        private DialogData currentDialog;
        private int currenNodeIndex;
        private Coroutine typingCoroutine;
        private bool isTyping;
        private Action onDialogEnd;
        private InputManager inputKey;
        protected override bool DontDestroy => false;
        protected override void Awake()
        {
            base.Awake();
            UniTask.WaitUntil(() => !InputManager.Shared);
            inputKey = InputManager.Shared;
            if (dialogPanel == null)
            {
                "DialogPanel이 할당되지 않았습니다".DError();
            }
            else
            {
                dialogPanel.Hide();
            }

            inputKey.Player.Skip.performed += OnSkipPerformed;

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            inputKey.Player.Skip.performed -= OnSkipPerformed;
        }

        private void OnSkipPerformed(InputAction.CallbackContext context)
        {
            if (currentDialog == null) return;

            if (isTyping)
            {
                CompleteTyping();
            }
            else
            {
                ShowNextNode();
            }
        }

        public void StartDialog(DialogData data, Action onComplete = null)
        {
            if (data == null || data.nodes == null || data.nodes.Count == 0)
            {
                $"유효하지 않은 DialogData".DError();
                return;
            }

            currentDialog = data;
            currenNodeIndex = 0;
            onDialogEnd = onComplete;

            LoadSpeakerIcon(data.speakerIconkey);

            dialogPanel.Show();
            ShowCurrentNode();

        }

        public void EndDialog()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            isTyping = false;
            dialogPanel.Hide();
            onDialogEnd?.Invoke();
            currentDialog = null;
        }

        private void ShowCurrentNode()
        {
            if (currentDialog == null || currenNodeIndex >= currentDialog.nodes.Count)
            {
                EndDialog();
                return;
            }

            DialogNode node  = currentDialog.nodes[currenNodeIndex];

            if (node.choices != null && node.choices.Count > 0) 
            {
                dialogPanel.ShowChoices(node.choices, OnChoiceSelected);
            }

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            typingCoroutine = StartCoroutine(TypeText(node.dialogText));
        }

        private void ShowNextNode()
        {
            currenNodeIndex++;
            ShowCurrentNode();
        }

        private IEnumerator TypeText(string text)
        {
            isTyping = true;
            dialogPanel.SetDialogText("");

            foreach(char c in text)
            {
                dialogPanel.AppenDialogText(c);
                yield return new WaitForSeconds(typingSpeed);
            }

            isTyping = false;
            typingCoroutine = null;
        }

        private void CompleteTyping()
        {
            if(typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            if (currentDialog != null && currenNodeIndex < currentDialog.nodes.Count)
            {
                dialogPanel.SetDialogText(currentDialog.nodes[currenNodeIndex].dialogText);
            }

            isTyping= false;    
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            if (currentDialog == null || currenNodeIndex >= currentDialog.nodes.Count)
            {
                return;
            }

            DialogNode node = currentDialog.nodes[currenNodeIndex];

            if (choiceIndex < 0 || choiceIndex >= node.choices.Count)
            {
                "잘못된 선택지 인덱스".DError();
                return;
            }

            DialogChoice choice = node.choices[choiceIndex];

            if (choice.nextNodeId < 0)
            {
                EndDialog();
                return;
            }

            currenNodeIndex = choice.nextNodeId;
            ShowCurrentNode();
        }

        private async UniTask LoadSpeakerIcon(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey))
            {
                dialogPanel.SetSpeakerIcon(null);
                return;
            }

            var sprite = await AbLoader.Shared.LoadAssetAsync<Sprite>(iconKey);
            dialogPanel.SetSpeakerIcon(sprite);
        }
    }
}

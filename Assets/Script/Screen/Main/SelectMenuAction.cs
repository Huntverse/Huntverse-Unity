using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace hunt.ui
{
    /// <summary>
    /// 메뉴 리스트를 키보드/마우스로 탐색하고 선택 상태를 표시.
    /// Enter를 누르면 해당 항목의 Button.onClick을 호출.
    /// </summary>
    public class SelectMenuAction : MonoBehaviour
    {
        [Header("Menu Items")]
        [SerializeField] private List<GameObject> menuFields = new List<GameObject>();
        private string selectedBoolName = ResourceKeyConst.Ka_isActive;
        private int initialIndex = 0;

        private int currentIndex = -1;
        private readonly List<Animator> animators = new();
        private readonly List<Button> buttons = new();

        private void Awake()
        {
            CacheComponents();
            for (int i = 0; i < menuFields.Count; i++)
            {
                var field = menuFields[i];
                if (!field) continue;

                var helper = field.GetComponent<SelectMenuField>();
                if (!helper) helper = field.AddComponent<SelectMenuField>();
                helper.Bind(this, i);
            }
        }

        private void OnEnable()
        {
           
            SetAllSelected(false);
            if (menuFields.Count > 0)
            {
                int init = Mathf.Clamp(initialIndex, 0, menuFields.Count - 1);
                SetIndex(init, playSound: false);
            }
        }

        private void Update()
        {
            if (menuFields.Count == 0) return;

            // 키보드 탐색 (↑/W, ↓/S)
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                Move(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                Move(+1);
            }

            // Enter 또는 KeypadEnter로 선택 실행
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SubmitCurrent();
            }
        }

        private void CacheComponents()
        {
            animators.Clear();
            buttons.Clear();

            foreach (var go in menuFields)
            {
                if (!go)
                {
                    animators.Add(null);
                    buttons.Add(null);
                    continue;
                }

                animators.Add(go.GetComponentInChildren<Animator>(true));
                buttons.Add(go.GetComponentInChildren<Button>(true));
            }
        }

        private void SetAllSelected(bool value)
        {
            for (int i = 0; i < animators.Count; i++)
            {
                if (animators[i] && !string.IsNullOrEmpty(selectedBoolName))
                    animators[i].SetBool(selectedBoolName, value);
            }
        }

        private void Move(int delta)
        {
            int next = currentIndex < 0 ? 0 : (currentIndex + delta + menuFields.Count) % menuFields.Count;
            SetIndex(next);
        }

        public void SetIndex(int index, bool playSound = true)
        {
            if (menuFields.Count == 0) return;
            index = Mathf.Clamp(index, 0, menuFields.Count - 1);
            if (currentIndex == index) return;

            if (currentIndex >= 0 && currentIndex < animators.Count)
            {
                if (animators[currentIndex] && !string.IsNullOrEmpty(selectedBoolName))
                    animators[currentIndex].SetBool(selectedBoolName, false);
            }

            currentIndex = index;

            if (animators[currentIndex] && !string.IsNullOrEmpty(selectedBoolName))
                animators[currentIndex].SetBool(selectedBoolName, true);
        }

        public void SubmitCurrent()
        {
            if (currentIndex < 0 || currentIndex >= buttons.Count) return;
            var btn = buttons[currentIndex];
            if (btn && btn.interactable)
            {
                btn.onClick?.Invoke();
            }
        }

        public void OnHovered(int index)
        {
            SetIndex(index);
        }

        public void OnClicked(int index)
        {
            SetIndex(index);
            SubmitCurrent();
        }
    }
}

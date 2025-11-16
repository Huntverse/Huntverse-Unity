using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace hunt
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickCountEvent : MonoBehaviour
    {
        [SerializeField] private float doubleClickTime = 0.5f;
        [SerializeField] private UnityEvent onDoubleClick;
        [SerializeField] private UnityEvent onOneClick;
        private Button button;
        private float lastClickTime = 0f;

        public static bool SelectedOnce = false;
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            float t = Time.time;

            if (t - lastClickTime < doubleClickTime)
            {
                SelectedOnce = true;
                onDoubleClick?.Invoke();
            }
            else
            {
                SelectedOnce = true;
                onOneClick?.Invoke();
            }

            lastClickTime = t;
        }
    }
}

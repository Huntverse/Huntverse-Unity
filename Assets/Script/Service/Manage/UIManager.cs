using UnityEngine;

namespace Hunt
{
    public class UIManager : MonoBehaviourSingleton<UIManager>
    {
        protected override bool DontDestroy => base.DontDestroy;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}

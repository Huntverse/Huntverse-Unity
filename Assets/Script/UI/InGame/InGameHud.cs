using UnityEngine;

namespace Hunt
{

    public class InGameHud : MonoBehaviourSingleton<InGameHud>
    {
        protected override bool DontDestroy => false;

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

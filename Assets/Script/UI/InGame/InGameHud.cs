using UnityEngine;

namespace Hunt
{

    public class InGameHud : MonoBehaviourSingleton<InGameHud>
    {
        protected override bool DontDestroy => true;

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

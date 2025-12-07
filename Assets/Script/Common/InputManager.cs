using UnityEngine;

namespace Hunt
{

    public class InputManager : MonoBehaviourSingleton<InputManager>
    {
        public InputSystem_Actions Action;
        public InputSystem_Actions.PlayerActions Player;
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
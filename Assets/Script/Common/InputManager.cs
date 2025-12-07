using UnityEngine;

namespace Hunt
{

    public class InputManager : MonoBehaviourSingleton<InputManager>
    {
        public InputSystem_Actions Action;
        public InputSystem_Actions.PlayerActions Player;
        protected override void Awake()
        {
            Action = new InputSystem_Actions();
            Player = Action.Player;
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }



    }

}
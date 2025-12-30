using System;
using UnityEngine;

namespace Hunt
{
    public abstract class InteractionBase : MonoBehaviour, IInteractable
    {
        [Header("SETTINGS")]
        [SerializeField] protected float interactionRange = 3f;

        public event Action<InteractionEventArgs> OnInteractionRequested;

        protected Transform currentInteractor;

        public abstract bool CanInteract();

        public virtual void Interact(Transform interactor)
        {
            if (!CanInteract())
            {
                "상호작용할 수 없습니다".DError();
                return;
            }
            currentInteractor = interactor;
            var eventArgs = new InteractionEventArgs(currentInteractor, transform.position);
            OnInteractionRequested?.Invoke(eventArgs);
            OnInteractLocal(eventArgs);
        }

        public abstract string GetInteractionText();
        public virtual float GetInteractionTriggerRange() => interactionRange;
        public Transform GetTransform() => transform;

        protected abstract void OnInteractLocal(InteractionEventArgs args);
        public virtual void SetInteractor(Transform interactor)
        {
            currentInteractor = interactor;
        }
        public virtual void ClearInteractor()
        {
            currentInteractor = null;
        }
    }
}

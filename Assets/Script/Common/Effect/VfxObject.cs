using System;
using UnityEngine;

namespace Hunt
{

    public class VfxObject : MonoBehaviour
    {
        private Action onReturnPool;
        private IVfxMover mover;
        public string returnOnClipName = "";
        [SerializeField] private Vector3 spawnPosition;
        public Vector3 SpawnPosition => spawnPosition;
        public void Init(Action returnCallback)
        {
            onReturnPool = returnCallback;
            mover = null;
        }

        public void SetMover(IVfxMover vfxmover)
        {
            mover = vfxmover;
        }

        private void Update()
        {
            if (mover != null && !mover.IsFinished)
            {
                mover.Tick(Time.deltaTime);
            }
            else if (mover != null && mover.IsFinished)
            {
                mover = null;
            }
        }
        public void ReturnToPool()
        {
            mover = null;
            
            if (onReturnPool == null)
            {
                $"🎆 [VfxObject] ReturnToPool 호출되었지만 onReturnPool이 null입니다!".DError();
                return;
            }
            
            onReturnPool?.Invoke();
        }

        public void OnAnimationEnd(string clipName = "")
        {
            if (!string.IsNullOrEmpty(returnOnClipName))
            {
                if (clipName == returnOnClipName)
                {
                    ReturnToPool();
                }
            }
            else
            {
                ReturnToPool();
            }
        }

    }
}

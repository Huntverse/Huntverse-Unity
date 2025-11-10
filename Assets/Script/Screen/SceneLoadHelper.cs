using Cysharp.Threading.Tasks;
using Mirror;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace hunt
{
    public class SceneLoadHelper : MonoBehaviourSingleton<SceneLoadHelper>
    {
        // async job stop/cancel
        private CancellationTokenSource cts;
        private SceneInstance curScene;
        protected override bool DontDestroy => base.DontDestroy;
        protected override void Awake()
        {
            base.Awake();
        }
        private void Start()
        {
            cts = new CancellationTokenSource();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            cts.Cancel();
            cts.Dispose();
        }

        public async UniTask LoadSceneSingleMode(string key)
        {
            CancelCurrentOps();

            if(curScene.Scene.IsValid())
            {
                await Addressables.UnloadSceneAsync(curScene);
            }
            var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Single);
            curScene = await handle.ToUniTask(cancellationToken:cts.Token);
        }

        public async UniTask<SceneInstance> LoadSceneAdditiveMode(string key)
        {
            CancelCurrentOps();

            var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive);
            var scene = await handle.ToUniTask(cancellationToken: cts.Token);
            return scene;
        }

        public async UniTask UnloadSceneAdditive(SceneInstance scene)
        {
            if (!scene.Scene.IsValid())
                return;

            CancelCurrentOps();
            await Addressables.UnloadSceneAsync(scene);
        }

        private void CancelCurrentOps()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = new CancellationTokenSource();
        }
    }
}

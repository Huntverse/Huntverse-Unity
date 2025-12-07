using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Hunt
{
    public class SceneLoadHelper : MonoBehaviourSingleton<SceneLoadHelper>
    {
        // async job stop/cancel
        private CancellationTokenSource cts;
        private SceneInstance curScene;
        protected override bool DontDestroy => true;
        protected override void Awake()
        {
            Debug.Log($"[SceneLoadHelper] Awake 호출 - DontDestroy: {DontDestroy}, GameObject: {gameObject.name}");
            base.Awake();
            
            // Awake 후 DontDestroyOnLoad 씬으로 이동했는지 확인
            Debug.Log($"[SceneLoadHelper] Awake 완료 - Scene: {gameObject.scene.name}, Shared: {(Shared == this ? "this" : "other")}");
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

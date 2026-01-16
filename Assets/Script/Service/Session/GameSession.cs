using Cysharp.Threading.Tasks;
using Hunt.Login;
using Hunt.Net;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace Hunt
{
    public enum SceneType
    {
        Village,
        FieldDungeon,
        Town,
        Dungeon
    }
    public class GameSession : MonoBehaviourSingleton<GameSession>
    {
        [SerializeField] private string loginServerIp = "127.0.0.1";
        [SerializeField] private int loginServerPort = 9000;
        private UInt64 loginServerKey;

        private NetworkManager networkManager;
        private string gameServerIp;
        private int gameServerPort;
        private UInt64 gameServerKey;

        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private LoginService loginService;
        public LoginService LoginService => loginService;

        private InGameService inGameService;
        public InGameService InGameService => inGameService;
        #region Field
        public uint CurrentSelectedWorldId { get; private set; }
        public CharModel SelectedCharacterModel { get; protected set; }
        public uint CurrentMapId { get; private set; }

        private UserCharacter localPlayer;
        public UserCharacter LocalPlayer => localPlayer;
        #endregion
       
        protected override bool DontDestroy => base.DontDestroy;
        #region Life
        protected override void Awake()
        {
            base.Awake();
        }
        private void Start()
        {
            InitializeSession();
        }

        private void InitializeSession()
        {
            networkManager = NetworkManager.Shared;
            if (networkManager == null) return;

            loginService = new LoginService(networkManager);
            inGameService = new InGameService(networkManager);

            isInitialized = true;
            $"[GameSession] Session Initialized".DLog();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
        #endregion

        #region Network Connect

        /// <summary> 로그인서버 연결 </summary>
        public async UniTask<bool> ConnectionToLoginServer()
        {
            if (!isInitialized)
            {
                $"[GameSession] 초기화 대기 중 ...".DLog();
                float elapsed = 0f;
                while (!isInitialized && elapsed < 1f)
                {
                    await UniTask.Delay(10);
                    elapsed += 0.01f;
                }

                if (!isInitialized)
                {
                    "[GameSession] 초기화 실패!".DError();
                    return false;
                }
            }

            if (networkManager == null) return false;
            "[GameSession] 로그인서버 연결 시도".DLog();

            if (networkManager.IsExistConnection(loginServerKey))
            {
                networkManager.StopNet(loginServerKey);
            }
            bool connected = false;
            await UniTask.RunOnThreadPool(() =>
            {
                connected = networkManager.ConnLoginServerSync(
                    (e, msg) => { $"[GameSession] 로그인 서버 연결 끊김 : {msg}".DLog(); },
                    () => { $"[GameSession] 로그인 서버 연결 성공".DLog(); },
                    (e) => { $"[GameSession] 로그인 서버 연결 실패:{e.Message}".DLog(); }
                    );
            });
            if (connected)
            {
                networkManager.StartLoginServer();
            }
            return connected;
        }
        /// <summary> 로그인서버 연결해제 </summary>
        public async UniTask DisConnectionToLoginServer()
        {
            $"[GameSession] 로그인 서버 연결을 해제.".DLog();
            await UniTask.RunOnThreadPool(() =>
            {
                networkManager?.DisConnLoginServer();
            });
        }

        private bool hasGameServerInfo = false;
        public void SetGameServerInfo(LoginAns loginans)
        {
            $"[GameSession] 게임 서버 정보 저장 : {gameServerIp} : {gameServerPort}".DLog();
        }
        /// <summary> 게임서버 연결 </summary>
        public async UniTask<bool> ConnectionToGameServer()
        {
            if (!hasGameServerInfo)
            {
                $"[GameSession] 게임 서버 정보가 설정되지 않음.".DError();
                return false;
            }
            if (networkManager == null)
            {
                $"[GameSession] NetworkManager is null".DError();
                return false;
            }

            $"[GameSession] 게임 서버 연결 시도: {gameServerIp} : {gameServerPort}".DLog();

            if (networkManager.IsExistConnection(gameServerKey))
            {
                $"[GameSession] 기존 게임 서버 연결 해제".DLog();
                networkManager.StopNet(gameServerKey);
            }

            bool connected = false;
            await UniTask.RunOnThreadPool(() =>
            {
                var netModule = networkManager.MakeNetModule(
                    NetModule.ServiceType.Game,
                    (error, msg) => { $"[GameSession] 게임 서버 연결 끊김: {error}, {msg}".DLog(); },
                    () => { $"[GameSession] 게임 서버 연결 성공".DLog(); },
                    (e) => { $"[GameSession] 게임 서버 연결 실패 : {e.Message}".DLog(); }
                );

                connected = netModule.SyncConn(gameServerIp, gameServerPort);

                if (connected)
                {
                    networkManager.InsertNetModule(gameServerKey, netModule);
                }
            });

            return connected;
        }
        /// <summary> 게임서버 연결해제 </summary>
        public async UniTask DisConnectionToGameServer()
        {
            $"[GameSession] 게임 서버 연결 해제".DLog();
            await UniTask.RunOnThreadPool(() =>
            {
                if (networkManager != null && networkManager.IsExistConnection(gameServerKey))
                {
                    networkManager.StopNet(gameServerKey);
                }
            });
        }

        #endregion

        #region Bind
        public List<SimpleCharacterInfo> CharacterInfos { get; protected set; }
        public SimpleCharacterInfo SelectedCharacter { get; protected set; }
        public WorldListRequest CachedWorldList { get; private set; }
        public Dictionary<string, List<CharModel>> CachedCharactersByWorld { get; private set; } = new Dictionary<string, List<CharModel>>();
        // Login
        public void SetCharacterList(List<SimpleCharacterInfo> characters)
        {
            CharacterInfos = new List<SimpleCharacterInfo>(characters);
            $"[GameSession] 캐릭터 리스트 저장 : {characters.Count}개".DLog();
        }

        public void AddCharacterInfo(SimpleCharacterInfo character)
        {
            if (CharacterInfos == null)
            {
                CharacterInfos = new List<SimpleCharacterInfo>();
            }
            CharacterInfos.Add(character);
            $"[GameSession] 캐릭터 추가: {character.Name} (CharId: {character.CharId})".DLog();
        }

        public void SelectCharacter(SimpleCharacterInfo character)
        {
            SelectedCharacter = character;
            $"[GameSession] 선택된 캐릭터 : 이름->{character.Name} , 직업->{character.ClassType}".DLog();
        }

        public void SelectCharacterById(ulong charId)
        {
            SelectedCharacter = CharacterInfos?.Find(c => c.CharId == charId);
            if (SelectedCharacter != null)
            {
                $"[GameSession] 캐릭터 선택 : {SelectedCharacter.Name}".DLog();
            }
        }
        public void SetSelectedWorld(uint worldId)
        {
            CurrentSelectedWorldId = worldId;
            $"[GameSession] ✅ 선택된 월드 ID 설정됨: {worldId}".DLog();
        }
        
        public void SetWorldList(WorldListRequest worldList)
        {
            CachedWorldList = worldList;
            $"[GameSession] 월드 리스트 캐싱: {worldList?.worlds?.Count ?? 0}개".DLog();
        }

        /// <summary> 현재 맵 설정 </summary>
        public void SetCurrentMap(uint mapId)
        {
            CurrentMapId = mapId;
            $"[GameSession] 현재 맵: {mapId}".DLog();
        }

        /// <summary> 맵 ID로 씬 타입 판별 </summary>
        public SceneType GetSceneTypeByMapId(uint mapId)
        {
            if (mapId >= 24000 && mapId < 25000) return SceneType.Village;
            if (mapId >= 27000 && mapId < 28000) return SceneType.FieldDungeon;
            
            this.DError("알 수 없는 맵 ID");
            return SceneType.FieldDungeon;
        }

        /// <summary> 맵 ID로 씬 이름 가져오기 </summary>
        public string GetSceneNameByMapId(uint mapId)
        {
            var sceneType = GetSceneTypeByMapId(mapId);
            return sceneType switch
            {
                SceneType.Village => ResourceKeyConst.Ks_Village,
                SceneType.FieldDungeon => ResourceKeyConst.Ks_FieldDungeon,
                _ => ResourceKeyConst.Ks_Village
            };
        }

        #endregion
        #region Player Events
        /// <summary> 로컬 플레이어 스폰 이벤트 </summary>
        public event Action<UserCharacter> OnLocalPlayerSpawned;

        /// <summary> 로컬 플레이어 스폰 알림 </summary>
        public void NotifyLocalPlayerSpawned(UserCharacter player)
        {
            $"[GameSession] 로컬 플레이어 스폰 알림: {player.name}".DLog();
            OnLocalPlayerSpawned?.Invoke(player);
        }

        public async UniTask<UserCharacter> SpawnLocalPlayer(Vector3 pos)
        {
            if (localPlayer != null)
            {
                localPlayer.transform.position = pos;
                $"[GameSession] 기존 플레이어 위치 이동: {pos}".DLog();
                return localPlayer;
            }

            var playergo = await AbLoader.Shared.LoadInstantiateAsync(ResourceKeyConst.Kp_UserCahr);
            if (playergo == null)
            {
                "[GameSession] UserCharacter Prefab 로드 실패".DError();
                return null;
            }

            localPlayer = playergo.GetComponent<UserCharacter>();
            if (localPlayer == null)
            {
                "[GameSession] UserCharacter 컴포넌트를 찾을 수 없음".DError();
                Destroy(playergo);
                return null;
            }

            DontDestroyOnLoad(playergo);
            localPlayer.transform.position = pos;
            
            $"[GameSession] 로컬 플레이어 스폰: {pos}".DLog();
            NotifyLocalPlayerSpawned(localPlayer);

            return localPlayer;
        }

        /// <summary> 플레이어를 포털 위치로 이동 </summary>
        public void MovePlayerToPortal(PortalDirection direction)
        {
            if (localPlayer == null)
            {
                "[GameSession] 로컬 플레이어가 없음".DError();
                return;
            }
            
            var portals = FindObjectsByType<FieldPortal>(FindObjectsSortMode.None);
            foreach (var portal in portals)
            {
                if (portal.Direction == direction)
                {
                    portal.SpawnPlayer(localPlayer);
                    return;
                }
            }
            
            this.DError($"[GameSession] {direction} 포털을 찾을 수 없음");
        }

        
        #endregion

        #region Dev
        // Dev

        public void SelectCharacterModel(CharModel model)
        {
            SelectedCharacterModel = model;
            $"[GameSession] 선택된 캐릭터 (Model): {model.name} (ClassType: {model.classtype})".DLog();
        }
        #endregion

    }
}

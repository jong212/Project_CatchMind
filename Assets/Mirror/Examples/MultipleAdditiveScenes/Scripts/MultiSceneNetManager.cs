using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    // A 서버온리할때 실행됨 
    // B  StartClientFromLobby 함수는 로비씬에서 Join 버튼을 눌렀을 때 실행됨 
    // B-1 StartClient 함수는 클라이언트를 서버에 연결하는 함수인데, 서버 측에서 
    // B-2 OnServerAddPlayer() 콜백함수로 자동으로 호출됩니다. 이 함수는 새로운 플레이어가 서버에 추가될 때 실행되는 콜백 함수입니다.
    [AddComponentMenu("")]
    public class MultiSceneNetManager : NetworkManager
    {
        public static MultiSceneNetManager instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [Header("Spawner Setup")]
        [Tooltip("Reward Prefab for the Spawner")]
        public GameObject rewardPrefab;

        [Header("MultiScene Setup")]
        public int instances = 3;
        public int roomCapacity = 4; // 방 당 최대 플레이어 수 설정

        [Scene]
        public string gameScene;

        // This is set true after server loads all subscene instances
        bool subscenesLoaded;

        // subscenes are added to this list as they're loaded
        readonly List<Scene> subScenes = new List<Scene>();

        // Dictionary to track player count per scene
        Dictionary<Scene, int> scenePlayerCount = new Dictionary<Scene, int>();

     

        #region Server System Callbacks
        //B-2
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAddPlayerDelayed(conn));
        }

        IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
        {
            while (!subscenesLoaded)
                yield return null;
            /*
             # 씬 로드 대상 결정

                 이 코드에서는 gameScene 변수에 저장된 씬 이름을 사용하여 새로운 씬을 로드합니다.
                 gameScene은 이 코드에서 미리 정의된 변수로, 어떤 씬을 로드할지 결정되어 있습니다.
                 따라서 새로 연결된 클라이언트에게 전송되는 SceneMessage의 sceneName 필드에는 gameScene이 설정되어 있습니다.
                 씬 로드 방식:

                 SceneOperation.LoadAdditive를 사용하여 현재 씬에 새 씬을 추가로 로드합니다.
                 이 방식은 기존 씬에 새로운 씬을 병합하는 방식입니다.
                 즉, 현재 씬에 gameScene이 추가로 로드되는 것입니다.
                 씬 로드 시점:

                 이 코드는 새로운 플레이어가 서버에 연결될 때OnServerAddPlayerDelayed 코루틴을 통해 실행됩니다.
                 따라서 새로 연결된 클라이언트에게 gameScene이 추가로 로드됩니다.
             */
            conn.Send(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.LoadAdditive });

            yield return new WaitForEndOfFrame();
            /*
            # 플레이어 객체 이동:
                base.OnServerAddPlayer(conn);을 호출하여 기본 플레이어 생성 로직을 실행합니다.
                GetTargetSceneForPlayer()를 호출하여 플레이어를 배치할 대상 씬을 찾습니다.
                SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);을 사용하여 플레이어 객체를 대상 씬으로 이동시킵니다.
                이 때 플레이어 객체의 NetworkIdentity 컴포넌트가 활성화되어 있는지 확인합니다.
            */
            base.OnServerAddPlayer(conn);            
            Scene targetScene = GetTargetSceneForPlayer();// 플레이어 넣을 씬 찾기
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);
            Debug.Log(targetScene);

            /*
            # PlayArea 활성화
                대상 씬의 루트 게임 오브젝트를 순회하면서 "PlayArea" 게임 오브젝트를 찾습니다.
                찾은 "PlayArea" 게임 오브젝트의 NetworkIdentity 컴포넌트를 강제로 활성화합니다.
            */
            foreach (GameObject go in targetScene.GetRootGameObjects())
            {
                if (go.name == "PlayArea")
                {
                    NetworkIdentity networkIdentity = go.GetComponent<NetworkIdentity>();
                    if (networkIdentity != null)
                    {
                        networkIdentity.enabled = true;
                    }
                }
            }

            if (scenePlayerCount.ContainsKey(targetScene))
            {
                scenePlayerCount[targetScene]++;
            }
            else
            {
                scenePlayerCount[targetScene] = 1;
            }
        }
        // B-2 관련
        // 이 메서드는 플레이어를 수용할 수 있는 적절한 씬을 찾기 위해 subScenes 리스트를 순회합니다. 
        // 만약 플레이어 수가 허용 인원보다 적은 씬이 있다면 그 씬을 반환하고, 그렇지 않으면 플레이어가 없는 씬을 반환합니다. 
        // 모든 씬이 가득 차 있거나 플레이어가 없는 씬이 없는 경우에는 리스트의 마지막 씬을 반환합니다.
        Scene GetTargetSceneForPlayer()
        {
            foreach (var scene in subScenes)
            {
                if (scenePlayerCount.TryGetValue(scene, out int count))
                {
                    if (count < roomCapacity)
                    {
                        return scene;
                    }
                }
                else
                {
                    return scene; // If the scene has no players yet
                }
            }

            return subScenes[subScenes.Count - 1];
        }

        #endregion

        #region Start & Stop Callbacks
        //A
        public override void OnStartServer()
        {
            StartCoroutine(ServerLoadSubScenes());
        }

        // A-1
        IEnumerator ServerLoadSubScenes()
        {
            for (int index = 1; index <= instances; index++)
            {
                yield return SceneManager.LoadSceneAsync(gameScene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });

                Scene newScene = SceneManager.GetSceneAt(index);
                subScenes.Add(newScene);
                scenePlayerCount[newScene] = 0; // Initialize player count for each scene
                Spawner.InitialSpawn(newScene);
            }

            subscenesLoaded = true;
        }

        public override void OnStopServer()
        {
            NetworkServer.SendToAll(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.UnloadAdditive });
            StartCoroutine(ServerUnloadSubScenes());
        }

        IEnumerator ServerUnloadSubScenes()
        {
            for (int index = 0; index < subScenes.Count; index++)
                if (subScenes[index].IsValid())
                    yield return SceneManager.UnloadSceneAsync(subScenes[index]);

            subScenes.Clear();
            scenePlayerCount.Clear();
            subscenesLoaded = false;

            yield return Resources.UnloadUnusedAssets();
        }

        public override void OnStopClient()
        {
            if (mode == NetworkManagerMode.Offline)
                StartCoroutine(ClientUnloadSubScenes());
        }

        IEnumerator ClientUnloadSubScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index) != SceneManager.GetActiveScene())
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(index));
        }

        #endregion

        
        public void StartClientFromLobby()//B 
        {
            SceneManager.LoadScene("MirrorMultipleAdditiveScenesGame");
            StartClient();//B-1
        }

        // Method to handle client connection
       
    } 

    [System.Serializable]
    public class RoomInfo
    {
        public string name;
        public int playerCount;
    }
}

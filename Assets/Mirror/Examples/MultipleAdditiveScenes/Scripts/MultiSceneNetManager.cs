using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    //1. 
    //  
    // B  StartClientFromLobby 함수는 로비씬에서 Join 버튼을 눌렀을 때 실행됨 
    // B-1 
    // B-2 
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
            Debug.Log(scenePlayerCount);
        }

        [Header("Spawner Setup")]
        [Tooltip("Reward Prefab for the Spawner")]
        public GameObject rewardPrefab;

        [Header("MultiScene Setup")]
        public int instances = 3;
        public int roomCapacity = 4; // 방 당 최대 플레이어 수 설정

        [Scene]
        public string gameScene;                                               //게임씬         
        bool subscenesLoaded;
        readonly List<Scene> subScenes = new List<Scene>();
        Dictionary<Scene, int> scenePlayerCount = new Dictionary<Scene, int>();

        // A <서버> 서버온리할때 실행됨
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


// B 클라이언트가 Join 버튼 클릭 시 발동       
        public void StartClientFromLobby()
        {
            SceneManager.LoadScene("MirrorMultipleAdditiveScenesGame");
// B-1 클라이언트를 서버에 연결하는 StartClient 함수이며 
            StartClient();
        }


        #region Server System Callbacks
// B-2 <서버콜백>   OnServerAddPlayer() 콜백함수로 StartClient 를 실행했기 때문에 자동으로 호출됩니다. 이 함수는 새로운 플레이어가 서버에 추가될 때 실행되는 콜백 함수입니다.
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAddPlayerDelayed(conn));
        }

        IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
        {
            while (!subscenesLoaded)
                yield return null;

// B-3 <서버 코루틴> 서버에서 실행은 되지만 아래 코드는 서버가 클라이언트에게 게임씬을 추가로 로드하라는 의미 즉, 클라가 Join을 누르면 어쨋든 서버가 알고 클라에게 겜씬 추가하라고 시키는것임

            conn.Send(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.LoadAdditive });

            yield return new WaitForEndOfFrame();
// # 플레이어 객체 이동:
//base.OnServerAddPlayer(conn);을 호출하여 기본 플레이어 생성 로직을 실행합니다.
//GetTargetSceneForPlayer()를 호출하여 플레이어를 배치할 대상 씬을 찾습니다.
//SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);을 사용하여 플레이어 객체를 대상 씬으로 이동시킵니다.
//이 때 플레이어 객체의 NetworkIdentity 컴포넌트가 활성화되어 있는지 확인합니다.
            base.OnServerAddPlayer(conn);
            Scene targetScene = GetTargetSceneForPlayer();// 플레이어 넣을 씬 찾기
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);
            Debug.Log("클라이언트 창 게임씬 로드함?");

// # PlayArea 활성화
//대상 씬의 루트 게임 오브젝트를 순회하면서 "PlayArea" 게임 오브젝트를 찾습니다.
//찾은 "PlayArea" 게임 오브젝트의 NetworkIdentity 컴포넌트를 강제로 활성화합니다.

            if (scenePlayerCount.ContainsKey(targetScene))
            {
                scenePlayerCount[targetScene]++;
            }
            else
            {
                scenePlayerCount[targetScene] = 1;
            }
        }
// B-4
//이 메서드는 플레이어를 수용할 수 있는 적절한 씬을 찾기 위해 subScenes 리스트를 순회합니다. 
//만약 플레이어 수가 허용 인원보다 적은 씬이 있다면 그 씬을 반환하고, 그렇지 않으면 플레이어가 없는 씬을 반환합니다. 
//모든 씬이 가득 차 있거나 플레이어가 없는 씬이 없는 경우에는 리스트의 마지막 씬을 반환합니다.
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


        // Method to handle client connection

    }

    [System.Serializable]
    public class RoomInfo
    {
        public string name;
        public int playerCount;
    }
}

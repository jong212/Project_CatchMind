using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Linq;

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

        [Header("MultiScene Setup")]
        public int instances = 2;
        public int roomCapacity = 2; // 방 당 최대 플레이어 수 설정

        [Scene]
        public string gameScene;                                               //게임씬         
        bool subscenesLoaded;
        readonly List<Scene> subScenes = new List<Scene>();
        Dictionary<Scene, int> scenePlayerCount = new Dictionary<Scene, int>();

        [SerializeField]Dictionary<Scene, List<NetworkConnectionToClient?>> scenePlayers = new Dictionary<Scene, List<NetworkConnectionToClient?>>();
        Vector3[] spawnPositions = new Vector3[]
{
            new Vector3(-7.01001f, 3.6f, 0),
            new Vector3(7.3f, 3.6f, 0),
            new Vector3(-7.01001f, 1.08f, 0),
            new Vector3(7.3f, 0.99f, 0),
            new Vector3(-7.01001f,-1.29f, 0),
            new Vector3(7.3f,-1.12f, 0),
            new Vector3(-7.01001f,-3.47f, 0),
            new Vector3(7.3f,-3.57f, 0),
};
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
                //서버에서 비동기적으로 게임씬을 로드시켜놓고
                yield return SceneManager.LoadSceneAsync(gameScene, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
                //현재 서버에서 로드 된 씬중 인덱스 1번의 씬을 반환한다 위에서 게임씬을 생성시켰으니 1번은 게임씬임
                Scene newScene = SceneManager.GetSceneAt(index);
                //게임씬 일단 서브씬에 추가하고
                subScenes.Add(newScene);
                scenePlayerCount[newScene] = 0;
                // 추가 된 1번째 게임씬을 배열에 추가한다
                //scenePlayers[newScene] = new List<NetworkConnectionToClient?>();
                scenePlayers[newScene] = new List<NetworkConnectionToClient?>(new NetworkConnectionToClient?[roomCapacity]);


                // 스포너를 통해 씬에서 초기 스폰 작업을 수행한다.
                Spawner.InitialSpawn(newScene);
            }

            subscenesLoaded = true;
        }


// B 클라이언트가 Join 버튼 클릭 시 발동       
        public void StartClientFromLobby()
        {
            SceneManager.LoadScene("MirrorMultipleAdditiveScenesGame");
// B-1 클라이언트를 서버에 연결하는 StartClient 함수이고 OnServerAddPlayer 콜백으로 실행시킴 
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
            Scene targetScene = GetTargetSceneForPlayer();// 플레이어 어느 방으로 보내야 하는지 반환하세요 (서버)
            Vector3 spawnPosition = GetSpawnPosition(targetScene, out int playerIndex); // 반환하는 방에서 몇 번쨰 자리가 비어있는지 확인하고 그 좌표값을 반환하세요 (서버)            
       
            // 일단 플레이어 현재 씬에 추가할게요? 
            // 게임씬은 아님 정확히는 게임씬 이동 전 (서버) 
            // base.OnServerAddPlayer(conn)을 호출하면 호출하면 서버와 클라이언트의 하이어라키에 플레이어 오브젝트가 자동으로 생성됩니다. TargetRpc와 같은 추가적인 동작 없이도 서버와 클라이언트의 상태가 동기화됩니다. (서버에서 실행하고 클라로 상태 전송)
            base.OnServerAddPlayer(conn); 

            // 하이어라키에 생성한 플레이어 정보를 player에 세팅 !(여기 코루틴 타는건 다 서버에서 동작하고 있는상태)
            GameObject player = conn.identity.gameObject;
            // 자 이제 플레이어 객체를 게임씬으로으로 이동할게요? (서버)
            SceneManager.MoveGameObjectToScene(player, targetScene);

            // 클라이언트 플레이어의 PlayerController를 가져와서 TargetRpc 호출
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TargetUpdatePlayerPosition(conn, spawnPosition,playerIndex);
            }
           
                /*player.GetComponent<RectTransform>().anchoredPosition = new Vector2(1, 1);*/
            // 플레이어 목록에 추가
            scenePlayers[targetScene][playerIndex] = conn;

            if (scenePlayerCount.ContainsKey(targetScene))
            {
                scenePlayerCount[targetScene]++;
            }
            else
            {
                scenePlayerCount[targetScene] = 1;
            }
            LogScenePlayers();
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
        Vector3 GetSpawnPosition(Scene scene, out int playerIndex)
        {
            if (scenePlayers.TryGetValue(scene, out List<NetworkConnectionToClient?> players))
            {
                for (int i = 0; i < roomCapacity; i++)
                {
                    if (players[i] == null)
                    {
                        playerIndex = i;
                        Debug.Log($"Spawn position for player index {i} is {spawnPositions[i]}");

                        return spawnPositions[i]; // 인덱스에 따른 미리 정의된 위치 반환
                    }
                }
            }
            playerIndex = -1;
            return Vector3.zero;
        } 
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            foreach (var scene in subScenes)
            {
                if (scenePlayers[scene].Contains(conn))
                {
                    int playerIndex = scenePlayers[scene].IndexOf(conn);
                    scenePlayers[scene][playerIndex] = null;
                    UpdatePlayerPositions(scene);
                    scenePlayerCount[scene]--;
                    LogScenePlayers();
                    break;
                }
            }
        }

        void UpdatePlayerPositions(Scene scene)
        {
            if (scenePlayers.TryGetValue(scene, out List<NetworkConnectionToClient?> players))
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i] != null)
                    {
                        var player = players[i].identity.gameObject;
                        player.transform.position = spawnPositions[i];
                    }
                }
            }
        }
        #endregion

        /*로그체크용*/
        #region Start & Stop Callbacks
        void LogScenePlayers()
        {
            foreach (var kvp in scenePlayers)
            {
                string sceneName = kvp.Key.name;
                string players = string.Join(", ", kvp.Value.Select(c => c != null ? c.connectionId.ToString() : "null"));
                Debug.Log($"Scene: {sceneName}, Players: {players}");
            }
        }

        void Update()
        {
            // 매 프레임마다 콘솔에 로그 출력
            LogScenePlayers();
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
            scenePlayers.Clear();
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

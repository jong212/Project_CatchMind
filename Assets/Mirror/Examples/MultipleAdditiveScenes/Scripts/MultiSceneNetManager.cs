using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace Mirror.Examples.MultipleAdditiveScenes
{
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

        void Start()
        {
            NetworkClient.OnConnectedEvent += OnClientConnected;
        }

        void OnDestroy()
        {
            NetworkClient.OnConnectedEvent -= OnClientConnected;
        }

        #region Server System Callbacks

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAddPlayerDelayed(conn));
        }

        IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
        {
            while (!subscenesLoaded)
                yield return null;

            conn.Send(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.LoadAdditive });

            yield return new WaitForEndOfFrame();

            base.OnServerAddPlayer(conn);

            Scene targetScene = GetTargetSceneForPlayer();
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);

            if (scenePlayerCount.ContainsKey(targetScene))
            {
                scenePlayerCount[targetScene]++;
            }
            else
            {
                scenePlayerCount[targetScene] = 1;
            }
        }

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

        public override void OnStartServer()
        {
            StartCoroutine(ServerLoadSubScenes());
        }

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

        // Method to start client from the lobby
        public void StartClientFromLobby()
        {
            SceneManager.LoadScene("MirrorMultipleAdditiveScenesMain");
            StartClient();
        }

        // Method to handle client connection
        void OnClientConnected()
        {
            // Switch to the game scene
            if (!string.IsNullOrEmpty(gameScene))
            {
                SceneManager.LoadScene(gameScene);
            }
        }
    } 

    [System.Serializable]
    public class RoomInfo
    {
        public string name;
        public int playerCount;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    [AddComponentMenu("")]
    public class MultiSceneNetManager : NetworkManager
    {
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

        /// <summary>
        /// Called on the server when a client adds a new player with NetworkClient.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAddPlayerDelayed(conn));
        }

        // This delay is mostly for the host player that loads too fast for the
        // server to have subscenes async loaded from OnStartServer ahead of it.
        IEnumerator OnServerAddPlayerDelayed(NetworkConnectionToClient conn)
        {
            // wait for server to async load all subscenes for game instances
            while (!subscenesLoaded)
                yield return null;

            // Send Scene message to client to additively load the game scene
            conn.Send(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.LoadAdditive });

            // Wait for end of frame before adding the player to ensure Scene Message goes first
            yield return new WaitForEndOfFrame();

            base.OnServerAddPlayer(conn);

            // Assign the player to a room
            Scene targetScene = GetTargetSceneForPlayer();
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);

            // Update player count for the assigned scene
            if (scenePlayerCount.ContainsKey(targetScene))
            {
                scenePlayerCount[targetScene]++;
            }
            else
            {
                scenePlayerCount[targetScene] = 1;
            }
        }

        // Method to get the target scene for the player based on capacity
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

            // If all scenes are full, return the last one (or handle as needed)
            return subScenes[subScenes.Count - 1];
        }

        #endregion

        #region Start & Stop Callbacks

        /// <summary>
        /// This is invoked when a server is started - including when a host is started.
        /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
        /// </summary>
        public override void OnStartServer()
        {
            StartCoroutine(ServerLoadSubScenes());
        }

        // We're additively loading scenes, so GetSceneAt(0) will return the main "container" scene,
        // therefore we start the index at one and loop through instances value inclusively.
        // If instances is zero, the loop is bypassed entirely.
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

        /// <summary>
        /// This is called when a server is stopped - including when a host is stopped.
        /// </summary>
        public override void OnStopServer()
        {
            NetworkServer.SendToAll(new SceneMessage { sceneName = gameScene, sceneOperation = SceneOperation.UnloadAdditive });
            StartCoroutine(ServerUnloadSubScenes());
        }

        // Unload the subScenes and unused assets and clear the subScenes list.
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

        /// <summary>
        /// This is called when a client is stopped.
        /// </summary>
        public override void OnStopClient()
        {
            // Make sure we're not in ServerOnly mode now after stopping host client
            if (mode == NetworkManagerMode.Offline)
                StartCoroutine(ClientUnloadSubScenes());
        }

        // Unload all but the active scene, which is the "container" scene
        IEnumerator ClientUnloadSubScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index) != SceneManager.GetActiveScene())
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(index));
        }

        #endregion
    }
}

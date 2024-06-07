using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    internal class Spawner
    {
        [ServerCallback]
        internal static void InitialSpawn(Scene scene)
        {
        }

        [ServerCallback]
        internal static void SpawnReward(Scene scene)
        {
        }
    }
}

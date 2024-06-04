using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Mirror.Examples.MultipleAdditiveScenes;

public class RoomListUI : MonoBehaviour
{
    public static RoomListUI instance;

    public Transform roomListParent; // Assign this to the Content GameObject of the Scroll View
    public GameObject roomListItemPrefab;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        // Initialization code if needed
    }

    public void OnJoinRoomButton()
    {
        // Start the client connection
        if (MultiSceneNetManager.instance != null)
        {
            MultiSceneNetManager.instance.StartClientFromLobby();
        }
        else
        {
            Debug.LogError("MultiSceneNetManager instance is not set.");
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Mirror;
public class RoomListManager : NetworkBehaviour
{
    public string lobbyScene;

    private List<Room> rooms;

    private void Start()
    {
        // 플레이어 ID 가져오기
        string playerId = PlayerPrefs.GetString("PlayerID", "Unknown");

        // 플레이어 ID를 사용하여 필요한 초기화 작업 수행
        Debug.Log("Player ID: " + playerId);

        // 방 리스트 불러오기 로직
        LoadRoomList();
    }

    private void LoadRoomList()
    {
        // 방 리스트 불러오는 로직 구현
        rooms = new List<Room>(); // 예시로 빈 리스트 생성
    }

    public void OnCreateRoomButtonClicked()
    {
        // 방 생성 로직
        CreateRoom();
    }

    public void OnJoinRoomButtonClicked(int roomId)
    {
        // 방 참가 로직
        JoinRoom(roomId);
    }

    private void CreateRoom()
    {
        // 방 생성 로직 구현
        SceneManager.LoadScene(lobbyScene);
    }

    private void JoinRoom(int roomId)
    {
        // 방 참가 로직 구현
        SceneManager.LoadScene(lobbyScene);
    }
}

[System.Serializable]
public class Room
{
    public int id;
    public string name;
    public int playerCount;
    public bool isInGame;
}

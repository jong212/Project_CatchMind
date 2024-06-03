using UnityEngine;
using Mirror;

public class ChatUser : NetworkBehaviour
{
    // SyncVar - 서버 변수를 모든 클라에 자동 동기화하는데 사용됨
    // 클라가 직접 변경하면 안되고, 서버에서 변경해야 함
    [SyncVar]
    public string PlayerName;

    // 호스트 또는 서버에서만 호출되는 함수
    public override void OnStartServer()
    {
        PlayerName = (string)connectionToClient.authenticationData;
    }

    public override void OnStartLocalPlayer()
    {
        var objChatUI = GameObject.Find("ChattingUI");
        if(objChatUI != null)
        {
            var chattingUI = objChatUI.GetComponent<ChattingUI>();
            if(chattingUI != null)
            {
                chattingUI.SetLocalPlayerName(PlayerName);
            }
        }

    }
}

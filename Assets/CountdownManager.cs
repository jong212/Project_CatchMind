using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;

public class CountdownManager : NetworkBehaviour
{

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("????zzz");
    }



    private void Update()
    {
        // 클라이언트에서 카운트다운 UI를 업데이트
        // 예: countdownText.text = countdownTime.ToString();
    }
}

using Mirror.Examples.Chat;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    [RequireComponent(typeof(NetworkTransformReliable))]
    public class PlayerController : NetworkBehaviour
    {

        public GameObject newPrefab;
        public GameObject[] Prefabs;
        public RectTransform canvasRectTransform;
        public RectTransform RawRectTransform;
        public TextMeshProUGUI textMeshProUGUI;
        public InputField IField;
        public enum GroundState : byte { Jumping, Falling, Grounded }
        [SyncVar(hook = nameof(OnTextChanged))]
        private string syncedText = "";

        [SyncVar(hook = nameof(OnAlignmentChanged))]
        private TextAlignmentOptions syncedAlignment = TextAlignmentOptions.Left;

        public override void OnStartAuthority()
        {
            IField = GameObject.FindGameObjectWithTag("field").GetComponent<InputField>();
            Debug.Log(IField);
            this.enabled = true;
            if (isLocalPlayer)
            {
                Debug.Log("OnStartAuthority: Initializing components for local player.");

                InitializeComponents();
                string playerId = PlayerPrefs.GetString("PlayerID", "No ID Found");
                if (!string.IsNullOrEmpty(playerId))
                {
                    string characternum = DatabaseUI.Instance.SelectPlayercharacterNumber(playerId);

                    if (!string.IsNullOrEmpty(characternum))
                    {
                        string sceneName = SceneManager.GetActiveScene().name;
                        CmdReplaceChild(sceneName, characternum);
                    }
                }
            }
        }

        void InitializeComponents()
        {
   Canvas canvas = GetComponentInChildren<Canvas>();
    if (canvas != null)
    {
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        
        // 자식 오브젝트 중에서 RectTransform을 가진 오브젝트를 찾음
        foreach (RectTransform child in canvas.GetComponentsInChildren<RectTransform>())
        {
            if (child != canvasRectTransform)
            {
                RawRectTransform = child;
                break;
            }
        }

        Debug.Log(RawRectTransform); // 찾은 RectTransform을 로그로 출력

        textMeshProUGUI = canvas.GetComponentInChildren<TextMeshProUGUI>();
        if (textMeshProUGUI == null)
        {
            Debug.LogError("TextMeshProUGUI component not found during initialization.");
        }
    }
    else
    {
        Debug.LogError("Canvas component not found during initialization.");
    }
        }

        public override void OnStopAuthority()
        {
            this.enabled = false;
        }

        void Update()
        {
            if (isLocalPlayer)
            {
                if (canvasRectTransform == null || textMeshProUGUI == null)
                {
                    InitializeComponents();
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
/*
1. 클라이언트가 텍스트 입력:
    * 클라이언트에서 엔터 키를 누르면 Update 메서드에서 CmdChangeText가 호출됩니다.
    CmdChangeText는 서버에서 실행되며, syncedText 변수를 업데이트합니다.

2. 서버가 변경 사항 전송:
    * 서버에서 syncedText 변수가 업데이트되면, 변경 사항이 모든 클라이언트로 전송됩니다.

3. 클라이언트가 변경 사항 수신:
    * 클라이언트는 변경된 syncedText 값을 수신하고, OnTextChanged 메서드를 호출합니다.
    * OnTextChanged 메서드는 새로운 텍스트 값을 textMeshProUGUI에 설정하여 UI를 업데이트합니다.

요약
SyncVar 변수는 서버에서 변경되면 자동으로 클라이언트로 전송됩니다.
클라이언트에서 SyncVar 변수의 변경 사항을 수신하면 hook 메서드가 호출됩니다.
hook 메서드는 클라이언트 측에서 실행되며, UI 업데이트 등의 작업을 수행합니다.                     
*/
                    string inputText = IField.text;
                    CmdChangeText(inputText);
                }
            }
        }

        void OnTextChanged(string oldText, string newText)
        {
            Debug.Log("OnTextChanged called with text: " + newText);
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = newText;
            }
        }
        void OnAlignmentChanged(TextAlignmentOptions oldAlignment, TextAlignmentOptions newAlignment)
        {
            Debug.Log("OnAlignmentChanged called with alignment: " + newAlignment);
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.alignment = newAlignment;
            }
        }

        [Command]
        void CmdChangeText(string inputText)
        {
            syncedText = inputText;
          /*  RpcChangeText(inputText);*/
        }

     /*   [ClientRpc]
        void RpcChangeText(string inputText)
        {
            Debug.Log("RpcChangeText called on the clients");
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = inputText;
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found in RpcChangeText.");
            }
        }*/

        [Command]
        void CmdReplaceChild(string sceneName, string characternum)
        {
            // 숫자 부분만 추출
            string numberPart = System.Text.RegularExpressions.Regex.Match(characternum, @"\d+").Value;
            Debug.Log("teset" + numberPart);
            if (int.TryParse(numberPart, out int intnum))
            {

                // 인덱스 유효성 검사
                if (intnum < 0 || intnum >= Prefabs.Length)
                {
                    Debug.LogError("Invalid character index.");
                    return;
                }

                // 새로운 자식 오브젝트 생성 및 네트워크 동기화
                GameObject newChild = Instantiate(Prefabs[intnum], transform.position, Prefabs[intnum].transform.rotation);

                // (서버) 참가한 씬을 타겟 변수에 저장
                Scene targetScene = SceneManager.GetSceneByName(sceneName);

                // (서버) 서버에서 생성한 프리팹 게임씬으로 이동
                SceneManager.MoveGameObjectToScene(newChild, targetScene);

                // (서버) 플레이어 자식으로 설정
                newChild.transform.SetParent(transform);

                // (서버) 모든 클라이언트에 프리팹 동기화
                NetworkServer.Spawn(newChild, connectionToClient);
                Debug.Log($"Spawned newChild: {newChild.name}");

                ResetClientToZero(newChild);
            }
            else
            {
                Debug.LogError("???????????");
            }
        }
        [ClientRpc]
        void ResetClientToZero(GameObject obj)
        {
            if (obj != null)
            {
                Debug.Log($"Rotation Before ResetClientToZero: {obj.transform.rotation}");
                obj.transform.localPosition = transform.position;
                Debug.Log($"Rotation After ResetClientToZero: {obj.transform.rotation}");
            }
            else
            {
                Debug.LogError("???????????????????");
            }
        }


        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer)
            {
                InitializeComponents();
            }
            if (textMeshProUGUI != null)
            {
                textMeshProUGUI.text = syncedText;
                textMeshProUGUI.alignment = syncedAlignment;
            }

        }

        [TargetRpc]
        public void TargetUpdatePlayerPosition(NetworkConnection target, Vector3 position, int playerIndex)
        {
            // 클라이언트에서 포지션 값 설정
            transform.position = position;
            if (canvasRectTransform == null || textMeshProUGUI == null)
            {
                InitializeComponents(); // 필요할 경우 컴포넌트 재초기화
            }
            if (canvasRectTransform != null)
            {
                if (playerIndex % 2 == 0) // 짝수 인덱스
                {
                    canvasRectTransform.anchoredPosition = new Vector2(3.23f, 1.98f);
         
                    CmdChangeAlignment(TextAlignmentOptions.Left); // 서버에서 정렬 상태 업데이트

                }
                else // 홀수 인덱스
                {
                    canvasRectTransform.anchoredPosition = new Vector2(-3.55f, 2.03f);
                    if (textMeshProUGUI != null)
                    {
                        Vector3 currentRotation = RawRectTransform.transform.rotation.eulerAngles;
                        currentRotation.y = 180f;
                        RawRectTransform.transform.rotation = Quaternion.Euler(currentRotation);
                        CmdChangeAlignment(TextAlignmentOptions.Right); // 서버에서 정렬 상태 업데이트

                    }
                    else
                    {
                        Debug.LogError("TextMeshProUGUI component not found.");
                    }
                }
            }
            else
            {
                Debug.LogError("Canvas RectTransform not found.");
            }
        }
        [Command]
        void CmdChangeAlignment(TextAlignmentOptions alignment)
        {
            syncedAlignment = alignment;
        }


    }
}

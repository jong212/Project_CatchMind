using Mirror.Examples.Chat;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mirror.Examples.MultipleAdditiveScenes
{
    [RequireComponent(typeof(NetworkTransformReliable))]
    public class PlayerController : NetworkBehaviour
    {

        public GameObject newPrefab;
        public GameObject[] Prefabs;
        public enum GroundState : byte { Jumping, Falling, Grounded }

        public override void OnStartAuthority()
        {
            this.enabled = true;
            if (isLocalPlayer)
            {
                Debug.Log("OnStartAuthority called");

                string playerId = PlayerPrefs.GetString("PlayerID", "No ID Found");
                if (!string.IsNullOrEmpty(playerId))
                {
                    string characternum = DatabaseUI.Instance.SelectPlayercharacterNumber(playerId);
                    Debug.Log($"Character Number: {characternum}");
                    if (!string.IsNullOrEmpty(characternum))
                    {
                        string sceneName = SceneManager.GetActiveScene().name;
                        CmdReplaceChild(sceneName, characternum);
                    }
                }
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
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    string sceneName = SceneManager.GetActiveScene().name;
                    //CmdReplaceChild(sceneName);
                }
            }
        }



        [Command]
        void CmdReplaceChild(string sceneName, string characternum)
        {
            // 숫자 부분만 추출
            string numberPart = System.Text.RegularExpressions.Regex.Match(characternum, @"\d+").Value;
            Debug.Log("teset" + numberPart);
            if (int.TryParse(numberPart, out int intnum))
            {

                // 기존 자식 오브젝트를 삭제
                foreach (Transform child in transform)
                {
                    NetworkServer.Destroy(child.gameObject);
                }

                // 인덱스 유효성 검사
                if (intnum < 0 || intnum >= Prefabs.Length)
                {
                    Debug.LogError("Invalid character index.");
                    return;
                }

                // 새로운 자식 오브젝트 생성 및 네트워크 동기화
                GameObject newChild = Instantiate(Prefabs[intnum], transform.position, transform.rotation);
                Debug.Log($"Instantiated newChild: {newChild.name}");
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
                Debug.LogError("Failed to parse character number.");
            }
        }
        [ClientRpc]
        void ResetClientToZero(GameObject obj)
        {
            if (obj != null)
            {
                obj.transform.localPosition = transform.position;
                Debug.Log($"ResetClientToZero called for: {obj.name}");
            }
            else
            {
                Debug.LogError("ResetClientToZero called with null object.");
            }
        }

        bool isClientInitialized = false; // 클라이언트 초기화 상태 변수 추가

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer)
            {
                isClientInitialized = true; // 클라이언트 초기화 완료
            }
        }

        [TargetRpc]
        public void TargetUpdatePlayerPosition(NetworkConnection target, Vector3 position)
        {
            // 클라이언트에서 포지션 값 설정
            transform.position = position;
            Debug.Log($"Client: Position updated to {position}");
        }


    }
}

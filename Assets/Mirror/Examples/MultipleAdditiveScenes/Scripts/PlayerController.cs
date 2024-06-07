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
        public enum GroundState : byte { Jumping, Falling, Grounded }

        public override void OnStartAuthority()
        {
            this.enabled = true;
        }

        public override void OnStopAuthority()
        {
            this.enabled = false;
            
        }

        void Update()
        {
            if(isLocalPlayer)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    string sceneName = SceneManager.GetActiveScene().name;
                    CmdReplaceChild(sceneName);
                }
            }
        }

       

        [Command]
        void CmdReplaceChild(string sceneName)
        {
            // 기존 자식 오브젝트를 삭제
            foreach (Transform child in transform)
            {
                NetworkServer.Destroy(child.gameObject);
            }
            // 새로운 자식 오브젝트 스폰
            GameObject newChild = Instantiate(newPrefab, transform.position, transform.rotation);            
            Scene targetScene = SceneManager.GetSceneByName(sceneName);            
            SceneManager.MoveGameObjectToScene(newChild, targetScene);
            newChild.transform.SetParent(transform);
            NetworkServer.Spawn(newChild, connectionToClient);
            //newChild.transform.localPosition = Vector3.zero;
            //newChild.GetComponent<ChildObject>().RpcSetParent(connectionToClient.identity);
            ResetClientToZero(newChild);
            // 클라이언트에서 자식 오브젝트를 동기화
        }

        [ClientRpc]
        void ResetClientToZero(GameObject obj)
        {
            obj.transform.localPosition = transform.position;
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

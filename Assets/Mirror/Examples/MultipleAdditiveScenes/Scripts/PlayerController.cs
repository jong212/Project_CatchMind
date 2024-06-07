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
            Debug.Log(connectionToClient);

            Scene targetScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.MoveGameObjectToScene(newChild, targetScene);
            NetworkServer.Spawn(newChild, connectionToClient);
            newChild.GetComponent<ChildObject>().RpcSetParent(connectionToClient.identity);


            //newChild.transform.SetParent(connectionToClient.identity.transform); // 부모 설정
            //RpcReplaceChild(connectionToClient.identity, newChild.GetComponent<NetworkIdentity>());

            // 클라이언트에서 자식 오브젝트를 동기화
        }
        /*[ClientRpc]
        void RpcReplaceChild(NetworkIdentity parent, NetworkIdentity newChildIdentity)
        {
            if (isServer || !isClientInitialized) return; // 서버 또는 초기화되지 않은 클라이언트는 실행하지 않음


            // 서버에서 생성된 프리팹을 찾아서 부모 설정
            GameObject newChild = newChildIdentity.gameObject;
            if (newChild != null)
            {
                newChild.transform.SetParent(parent.transform);
            }
        }*/
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

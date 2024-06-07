using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildObject : NetworkBehaviour
{
    /*[Command]
    public void CmdSetParent(NetworkIdentity identity)
    {
        RpcSetParent(identity);
    }*/
    [ClientRpc]
    public void RpcSetParent(NetworkIdentity identity)
    {
        this.transform.SetParent(identity.transform);
    }
}

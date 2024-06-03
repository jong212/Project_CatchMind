using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetSpawnedSubObject : NetworkBehaviour
{
    public float _destroyAfter = 2.0f;
    public float _force = 1000;

    public Rigidbody RigidBody_SubObj;

    public override void OnStartServer()
    {
        Invoke(nameof(DestroySelf), _destroyAfter);
    }

    private void Start()
    {
        RigidBody_SubObj.AddForce(transform.forward * _force);
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(this.gameObject);
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        DestroySelf();
    }
}

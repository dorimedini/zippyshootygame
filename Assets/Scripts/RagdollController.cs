using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RagdollController : MonoBehaviourPun
{
    public GameObject ragdoll;

    public void BroadcastRagdoll(Vector3 position, Quaternion rotation, float timeout)
    {
        photonView.RPC("Ragdoll", RpcTarget.All, position, rotation, timeout);
    }

    [PunRPC]
    public void Ragdoll(Vector3 position, Quaternion rotation, float timeout)
    {
        Destroy(Instantiate(ragdoll, position, rotation), timeout);
    }
}

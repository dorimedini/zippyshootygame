using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class RagdollController : MonoBehaviourPun
{
    public static GameObject ragdoll;

    void Awake()
    {
        ragdoll = Resources.Load("PlayerRagdoll") as GameObject;
    }

    public void BroadcastRagdoll(Vector3 position, Vector3 velocity, Quaternion rotation, float timeout)
    {
        photonView.RPC("Ragdoll", RpcTarget.Others, position, velocity, rotation, timeout);
    }

    [PunRPC]
    public void Ragdoll(Vector3 position, Vector3 velocity, Quaternion rotation, float timeout)
    {
        var activeRagdoll = Instantiate(ragdoll, position, rotation);
        activeRagdoll.GetComponent<Rigidbody>().velocity = velocity;
        Destroy(activeRagdoll, timeout);
    }
}

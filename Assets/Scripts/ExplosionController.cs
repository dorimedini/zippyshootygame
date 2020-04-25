using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ExplosionController : MonoBehaviourPun
{
    public void BroadcastExplosion(Vector3 position, string shooterId)
    {
        photonView.RPC("Explosion", RpcTarget.All, position, shooterId);
    }

    [PunRPC]
    public void Explosion(Vector3 position, string shooterId)
    {
        float radius = 15f;
        float force = 25f;
        float upforce = 1f;
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (Collider col in colliders)
        {
            // Only affect the local player, provided he's not the shooter.
            // We don't want to affect the shooter because it would interfere with the shoot-down-and-launch movement mechanic
            if (col.GetComponent<NetworkCharacter>() != null &&
                col.GetComponent<PhotonView>().Owner.UserId != shooterId &&
                photonView.IsMine)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                float dist = (rb.position - position).magnitude;
                if (dist <= radius)
                {
                    // When player is off the ground, root motion isn't applied. In case player is grounded when explosion hit,
                    // momentarily turn off root motion until player is lifted off the ground.
                    col.GetComponent<PlayerMovementController>().DisableRootMotionFor(0.1f);
                    // Some force from the explosion source, and some "upward" (inward-radial) force. Must be proportional to distance
                    Vector3 explosionForce = ((rb.position - position).normalized * force + (-rb.position * upforce)) / dist;
                    rb.AddForce(explosionForce, ForceMode.Impulse);
                }
            }
        }
    }
}

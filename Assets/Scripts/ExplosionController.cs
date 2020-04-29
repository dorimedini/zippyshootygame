using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ExplosionController : MonoBehaviourPun
{
    public DamageController dmgCtrl;
    public string localUserId;

    public void BroadcastExplosion(Vector3 position, string shooterId)
    {
        photonView.RPC("Explosion", RpcTarget.All, position, shooterId);
    }

    [PunRPC]
    public void Explosion(Vector3 position, string shooterId)
    {
        Collider[] colliders = Physics.OverlapSphere(position, UserDefinedConstants.explosionRadius);
        foreach (Collider col in colliders)
        {
            // Only affect the local player, provided he's not the shooter.
            // To check this, compare the user ID to the shooter AND to the local player AND to the explosion victim.
            // We don't want to affect the shooter because it would interfere with the shoot-down-and-launch movement mechanic
            if (col.GetComponent<NetworkCharacter>() == null)
                continue;
            // The "Explosion" global view isn't instantiated on the network so it has no "owner".
            // As such, we use the local-user-ID variable set by the network manager.
            string hitUserId = col.GetComponent<PhotonView>().Owner.UserId;
            if (hitUserId != shooterId && hitUserId == localUserId)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                float dist = (rb.ClosestPointOnBounds(position) - position).magnitude;
                if (dist <= UserDefinedConstants.explosionRadius)
                {
                    // When player is off the ground, root motion isn't applied. In case player is grounded when explosion hit,
                    // momentarily turn off root motion until player is lifted off the ground.
                    col.GetComponent<PlayerMovementController>().DisableRootMotionFor(0.1f);
                    // Some force from the explosion source, and some "upward" (inward-radial) force. Must be proportional to distance
                    Vector3 explosionForce = (  UserDefinedConstants.explosionForce * (rb.position - position).normalized +
                                                UserDefinedConstants.explosionLift * (-rb.position)
                                             ) / (dist + 1); // In case dist is close to zero
                    rb.AddForce(explosionForce, ForceMode.Impulse);
                    // Do damage (AFTER adding force, so if the ragdoll replaces it it'll fly off).
                    // I don't know what ClosestPointOnBounds returns if the point is in the collider so clamp the dist/radius ratio to [0,1]
                    // and use that value to get the proportion of damage to deal.
                    float damage = UserDefinedConstants.projectileHitDamage * (1 - Mathf.Clamp01(dist / UserDefinedConstants.explosionRadius));
                    dmgCtrl.BroadcastInflictDamage(shooterId, damage, hitUserId);
                }
            }
        }
    }
}

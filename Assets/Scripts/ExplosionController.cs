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
        photonView.RPC("RemoteExplosion", RpcTarget.All, position, shooterId);
    }

    [PunRPC]
    public void RemoteExplosion(Vector3 position, string shooterId)
    {
        // Only affect the local player, provided he's not the shooter.
        foreach (var col in OtherPlayersInExplosion(position, shooterId, true))
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            Vector3 hitPosition = rb.ClosestPointOnBounds(position);
            float dist = (hitPosition - position).magnitude;
            if (dist <= UserDefinedConstants.explosionRadius)
            {
                // When player is off the ground, root motion isn't applied. In case player is grounded when explosion hit,
                // momentarily turn off root motion until player is lifted off the ground.
                col.GetComponent<PlayerMovementController>().DisableRootMotionFor(0.1f);
                // Some force from the explosion source, and some "upward" (inward-radial) force. Must be proportional to distance
                Vector3 explosionForce = ExplosionForce(hitPosition, position, dist); // In case dist is close to zero
                rb.AddForce(explosionForce, ForceMode.Impulse);
                // Do damage (AFTER adding force, so if the ragdoll replaces it it'll fly off).
                // I don't know what ClosestPointOnBounds returns if the point is in the collider so clamp the dist/radius ratio to [0,1]
                // and use that value to get the proportion of damage to deal.
                float damage = UserDefinedConstants.projectileHitDamage * (1 - Mathf.Clamp01(dist / UserDefinedConstants.explosionRadius));
                dmgCtrl.BroadcastInflictDamage(shooterId, damage, col.GetComponent<PhotonView>().Owner.UserId);
            }
        }
    }

    List<Collider> OtherPlayersInExplosion(Vector3 position, string shooterId, bool localOnly)
    {
        List<Collider> playerCols = new List<Collider>();
        foreach (Collider col in Physics.OverlapSphere(position, UserDefinedConstants.explosionRadius))
        {
            if (col.GetComponent<NetworkCharacter>() == null)
                continue;
            string hitUser = col.GetComponent<PhotonView>().Owner.UserId;
            if (hitUser != shooterId && (!localOnly || hitUser == localUserId))
                playerCols.Add(col);
        }
        return playerCols;
    }

    /** Returns true iff rigidbody is in radius. Out parameter: the force to apply */
    Vector3 ExplosionForce(Vector3 hitPosition, Vector3 explosionPosition, float dist)
    {
        // Most force in the explosion-->hitPosition direction, and a bit of lift in the radial (-hitPosition) direction.
        // Divide by dist+1, because dist can be zero.
        return (UserDefinedConstants.explosionForce * (hitPosition - explosionPosition).normalized + UserDefinedConstants.explosionLift * (-hitPosition)) / (dist + 1);
    }
}

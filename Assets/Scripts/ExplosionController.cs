using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class ExplosionController : MonoBehaviourPun
{
    public DamageController dmgCtrl;
    public string localUserId;

    public void BroadcastExplosion(Vector3 position, string shooterId)
    {
        // Send a message to each remote player containing relevant explosion data.
        // We do these calculations here because we're applying the same force both on the remote player (via RPC) and locally (to give immediate feedback).
        // Compute hit players, which force to apply and how far away the target is (for damage calculation).
        Dictionary<string, Vector3> userIdsTohits = new Dictionary<string, Vector3>();
        Dictionary<string, float> userIdsToDistances = new Dictionary<string, float>();
        foreach (Collider remotePlayerCol in OtherPlayersInExplosion(position, shooterId, false))
        {
            string hitUserId = remotePlayerCol.GetComponent<PhotonView>().Owner.UserId;
            Rigidbody rb = remotePlayerCol.GetComponent<Rigidbody>();
            Vector3 hitPosition = rb.ClosestPointOnBounds(position);
            float dist = (hitPosition - position).magnitude;
            if (dist <= UserDefinedConstants.explosionRadius)
            {
                userIdsTohits[hitUserId] = ExplosionForce(hitPosition, position, dist);
                userIdsToDistances[hitUserId] = dist;
                // While we're here, apply the force locally to the network character
                remotePlayerCol.GetComponent<NetworkCharacter>().ApplyLocalForce(userIdsTohits[hitUserId], ForceMode.Impulse);
            }
        }

        // Get players and send RPCs
        Dictionary<int, Player> playerDict = PhotonNetwork.CurrentRoom.Players;
        foreach (string targetUserId in userIdsTohits.Keys)
        {
            Vector3 force = userIdsTohits[targetUserId];
            foreach (int playerIdx in playerDict.Keys)
            {
                Player player = playerDict[playerIdx];
                if (player.UserId == targetUserId)
                {
                    photonView.RPC("RemoteExplosion", player, userIdsTohits[targetUserId], userIdsToDistances[targetUserId], shooterId);
                    break;
                }
            }
        }
    }

    [PunRPC]
    public void RemoteExplosion(Vector3 explosionForce, float dist, string shooterId)
    {
        // We only reach this method if this local player was hit by an explosive force.
        // Apply force (disable root motion for a bit):
        Player player = PhotonNetwork.LocalPlayer;
        GameObject playerObj = NetworkCharacter.GetPlayerGameObject(player);
        playerObj.GetComponent<PlayerMovementController>().DisableRootMotionFor(UserDefinedConstants.explosionParalysisTime);
        playerObj.GetComponent<Rigidbody>().AddForce(explosionForce, ForceMode.Impulse);
        // Do damage (to self):
        float damage = UserDefinedConstants.projectileHitDamage * (1 - Mathf.Clamp01(dist / UserDefinedConstants.explosionRadius));
        dmgCtrl.BroadcastInflictDamage(shooterId, damage, player.UserId);
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

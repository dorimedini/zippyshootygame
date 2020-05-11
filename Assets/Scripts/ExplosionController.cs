using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class ExplosionController : MonoBehaviourPun
{
    public DamageController dmgCtrl;
    public string localUserId;
    public GameObject explosionPrefab;
    public AudioClip explosionSound;

    public void BroadcastExplosion(Vector3 position, string shooterId, bool friendlyFire)
    {
        // Send a message to each remote player containing relevant explosion data.
        // We do these calculations here because we're applying the same force both on the remote player (via RPC) and locally (to give immediate feedback).
        // Compute hit players, which force to apply and how far away the target is (for damage calculation).
        Dictionary<string, Vector3> userIdsTohits = new Dictionary<string, Vector3>();
        Dictionary<string, float> userIdsToDistances = new Dictionary<string, float>();
        foreach (Collider remotePlayerCol in PlayersInExplosion(position, false))
        {
            string hitUserId = Tools.NullToEmptyString(remotePlayerCol.GetComponent<PhotonView>().Owner.UserId);
            // If friendly fire is off don't register a hit on the shooter
            if (!friendlyFire && hitUserId == shooterId)
                continue;
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
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string userId = Tools.NullToEmptyString(player.UserId);
            if (userIdsTohits.ContainsKey(userId))
            {
                // Trigger a damaging explosion
                photonView.RPC("RemoteExplosion", player, position, userIdsTohits[userId], userIdsToDistances[userId], shooterId);
            }
            else
            {
                // Trigger the graphic, no damage
                photonView.RPC("RemoteExplosionFriendly", player, position);
            }
        }
    }

    [PunRPC]
    public void RemoteExplosionFriendly(Vector3 position) { Explosion(position); }

    [PunRPC]
    public void RemoteExplosion(Vector3 position, Vector3 explosionForce, float dist, string shooterId)
    {
        // Instantiate explosion graphic and sound
        Explosion(position);

        // Apply force (disable root motion for a bit):
        Player player = PhotonNetwork.LocalPlayer;
        GameObject playerObj = NetworkCharacter.GetPlayerGameObject(player);
        playerObj.GetComponent<PlayerMovementController>().DisableRootMotionFor(UserDefinedConstants.explosionParalysisTime);
        playerObj.GetComponent<Rigidbody>().AddForce(explosionForce, ForceMode.Impulse);
        // Do damage (to self):
        float damage = UserDefinedConstants.projectileHitDamage * (1 - Mathf.Clamp01(dist / UserDefinedConstants.explosionRadius));
        dmgCtrl.BroadcastInflictDamage(shooterId, damage, player.UserId);
    }

    void Explosion(Vector3 position)
    {
        var explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        AudioSource.PlayClipAtPoint(explosionSound, position);
        Destroy(explosion, 3f);
    }

    List<Collider> PlayersInExplosion(Vector3 position, bool localOnly)
    {
        List<Collider> playerCols = new List<Collider>();
        foreach (Collider col in Physics.OverlapSphere(position, UserDefinedConstants.explosionRadius))
        {
            if (col.GetComponent<NetworkCharacter>() == null)
                continue;
            string hitUser = col.GetComponent<PhotonView>().Owner.UserId;
            if (!localOnly || hitUser == localUserId)
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

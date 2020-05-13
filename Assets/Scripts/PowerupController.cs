using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PowerupController : MonoBehaviourPun
{
    public GameObject[] powerupPrefabs;

    private System.Random rnd;

    // Start is called before the first frame update
    void Start()
    {
        rnd = new System.Random();
    }

    public int RandomPowerupIdx() { return rnd.Next(0, powerupPrefabs.Length - 1); }

    public void SpawnPowerup(Vector3 direction, int powerupIdx, string powerupId)
    {
        // TODO: Spawn a powerup at origin. Add a collider (for everyone, this method is called via RPC).
        // TODO: Any LOCAL player who picks up a powerup: give him the powerup, but maybe revoke it later.
        // TODO: During that time fire an RPC to all powerup controllers with the timestamp at time of powerup pickup (and locally call the same RPC).
        // TODO: The other (remote) powerup controllers remove the powerup from the active powerup data structure.
        // TODO: If a powerup pickup request is recieved for a powerup that no longer exists, but the timestamp is lower than that of the original
        // TODO: powered-up player, revoke the powered-up player's power via RPC call and give the earlier player the powerup.
        // TODO: Revoke silently (or maybe give some "DENIED" feedback like rumor has that Quake did)
    }

    public void BroadcastPickupPowerup(string powerupId, string userId)
    {
        // TODO: Get current timestamp.
        // TODO: If powerup ID is already in the picked-up list with earlier timestamp, do nothing.
        // TODO: Otherwise, add the entry to the dictionary of powerup IDs --> timestamps and send the RPC to all powerup controllers.
        // TODO: Also add the user ID to the powerupID-->userID dictionary.
    }

    [PunRPC]
    public void PickupPowerup(string powerupId, string userId, float timestamp)
    {
        // TODO: If the given timestamp is less than an existing timestamp for the powerup, broadcast a Deny to the userId.
        // TODO: Otherwise, update the dictionary with the powerup owner.
        // TODO: If an entry already existed for the powerup with a player ID who was slower, broadcast a Deny to that player
    }

    public void BroadcastDenyPowerup(string powerupId, string userId)
    {
        // TODO: Call a targeted RPC to the given user that denies the powerup granted
    }

    [PunRPC]
    public void DenyPowerup(string powerupId)
    {
        // TODO: Find the powerupable local player and deny the powerup with the given ID
    }
}

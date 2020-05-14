using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PowerupController : MonoBehaviourPun
{
    public GameObject[] powerupPrefabs;

    private System.Random rnd;

    private Dictionary<string, string> powerupPickedUpBy;
    private Dictionary<string, long> powerupPickedUpAt;
    private Dictionary<string, Powerup> powerups;

    // Start is called before the first frame update
    void Start()
    {
        rnd = new System.Random();
        powerupPickedUpBy = new Dictionary<string, string>();
        powerupPickedUpAt = new Dictionary<string, long>();
        powerups = new Dictionary<string, Powerup>();
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

    public void BroadcastPickupPowerup(string powerupId)
    {
        // If someone already beat us to it, do nothing. This counts as buggy, because it means the local clock is laggy...
        long timestamp = Tools.Timestamp();
        string userIdOfPickerUpper = PhotonNetwork.LocalPlayer.UserId;
        if (!UserWinsFightForPowerup(powerupId, userIdOfPickerUpper, timestamp))
        {
            Debug.LogError("Current timestamp is " + timestamp + "," +
                " but remote player " + powerupPickedUpBy[powerupId] + " " +
                "already logged pickup at " + powerupPickedUpAt[powerupId]);
            return;
        }

        // OK, it's available. Add it to the dictionaries and tell other players who picked it up and when
        UpdateData(powerupId, userIdOfPickerUpper, timestamp);
        photonView.RPC("PickupPowerup", RpcTarget.Others, powerupId, userIdOfPickerUpper, timestamp);
    }

    [PunRPC]
    public void PickupPowerup(string powerupId, string userId, long timestamp)
    {
        // No matter who actually got it, if it hasn't been destroyed yet the powerup is gonna have to disappear now
        PowerupPickedUp(powerupId);

        // Someone else picked up a powerup.
        // If the powerup ID doesn't exist in the dictionaries, great - add it and return
        if (!powerupPickedUpAt.ContainsKey(powerupId))
        {
            UpdateData(powerupId, userId, timestamp);
            return;
        }

        // Some else picked it up (maybe after, maybe before).
        // If the previous player picked it up earlier we may need to send a Deny to the RPC source.
        // To prevent a situation where all clients send the same Deny to the lagging player who thought he picked up a powerup,
        // only the player who really picked up the powerup should send the Deny.
        // On the other hand, if the caller of this RPC beat the currently powered-up player, someone needs to Deny the slower
        // player. This operation can be handled *locally*: only the local player needs to actually be denied, the other players
        // will get RPCs with timestamps and know who's powered up.

        // In any case, from here only the local player has stuff to do
        string currentPoweredUpUser = powerupPickedUpBy[powerupId];
        if (currentPoweredUpUser != PhotonNetwork.LocalPlayer.UserId)
            return;

        if (UserWinsFightForPowerup(powerupId, userId, timestamp))
        {
            // Local player lost fight for powerup. Deny self, update dictionaries
            UpdateData(powerupId, userId, timestamp);
            DenyPowerup(powerupId);
        }
        else
        {
            // Local player gets to keep the powerup. Deny the caller
            BroadcastDenyPowerup(powerupId, userId);
        }
    }

    public void BroadcastDenyPowerup(string powerupId, string userId)
    {
        photonView.RPC("DenyPowerup", NetworkCharacter.GetPlayerByUserID(userId), powerupId);
    }

    [PunRPC]
    public void DenyPowerup(string powerupId)
    {
        NetworkCharacter.GetPlayerGameObject(PhotonNetwork.LocalPlayer).GetComponent<PowerupableCharacter>().DenyPower(powerupId);
    }

    public void PowerupPickedUp(string powerupId)
    {
        // TODO: Destroy local instance of the powerup (if still exists).
        // TODO: Play a sound, dissolve all fancy-like (maybe logic and assets exist in the prefab?)
    }

    bool UserWinsFightForPowerup(string powerupId, string userId, long timestamp)
    {
        // If it's up for grabs, yeah totally take it
        if (!powerupPickedUpAt.ContainsKey(powerupId))
            return true;

        // Faster user wins
        if (powerupPickedUpAt[powerupId] != timestamp)
            return powerupPickedUpAt[powerupId] > timestamp;

        // To be consistent with who gets the powerup in case of a tie in timestamps, use lex order on user IDs
        return userId.CompareTo(powerupPickedUpBy[powerupId]) < 0;
    }

    void UpdateData(string powerupId, string userId, long timestamp)
    {
        powerupPickedUpAt[powerupId] = timestamp;
        powerupPickedUpBy[powerupId] = userId;
    }
}

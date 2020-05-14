using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.Linq;

public class PowerupController : MonoBehaviourPun
{
    public GameObject[] powerupPrefabs;
    public AudioClip pickupSound;
    public SunController sun;

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
        // Spawn a powerup s.t. it's contained in the sun but "touches" it from within:
        //              ______
        //          ___/      \___
        //        _/              \_
        //       /                  \
        //      |                    |
        //     / __                   \
        // /__ |/  \                  |
        // \   |\__/                  |
        //     \                      /
        //      |                    |
        //       \_                _/
        //         \___        ___/
        //             \______/
        //
        GameObject powerupObj = Instantiate(
            powerupPrefabs[powerupIdx],
            Vector3.zero,
            Tools.Geometry.UpRotation(-direction));
        powerups[powerupId] = powerupObj.GetComponentInChildren<Powerup>();

        // We need the powerup to be offset some positive value in the direction, so gravity takes effect
        float offset = Mathf.Max(0.1f, sun.Radius() - powerups[powerupId].Radius());
        powerupObj.transform.position = direction.normalized * offset;

        // Init field values
        powerups[powerupId].direction = direction;
        powerups[powerupId].powerupCtrl = this;
        powerups[powerupId].powerupId = powerupId;
        powerups[powerupId].powerupIdx = powerupIdx;
    }

    public void BroadcastPickupPowerup(string powerupId)
    {
        // If someone already beat us to it, do nothing. This counts as buggy, because it means the local clock is laggy...
        long timestamp = Tools.Timestamp();
        string userIdOfPickerUpper = Tools.NullToEmptyString(PhotonNetwork.LocalPlayer.UserId);
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

        // Grant the player the actual power
        NetworkCharacter
            .GetPlayerGameObject(PhotonNetwork.LocalPlayer)
            .GetComponent<PowerupableCharacter>()
            .GrantPower(powerupId, powerups[powerupId].powerupIdx);
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
        if (currentPoweredUpUser != Tools.NullToEmptyString(PhotonNetwork.LocalPlayer.UserId))
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
        NetworkCharacter
            .GetPlayerGameObject(PhotonNetwork.LocalPlayer)
            .GetComponent<PowerupableCharacter>()
            .DenyPower(powerupId);
    }

    public void PowerupPickedUp(string powerupId)
    {
        // If gameobject is destroyed or already picked up (can happen if a remote user didn't get the RPC that the powerup
        // was picked up before sending his own) just return
        if (!powerups.ContainsKey(powerupId))
            return;
        Powerup powerup = powerups[powerupId];

        // Play pickup sound
        AudioSource.PlayClipAtPoint(pickupSound, powerup.transform.position);

        // Disable colliders so players can walk through the powerup
        SphereCollider[] cols = powerup.GetComponentsInChildren<SphereCollider>();
        foreach (var col in cols)
            col.enabled = false;

        // Hide all graphics except the particles
        powerup.GetComponentInChildren<Light>().enabled = false;
        MeshRenderer[] meshes = powerup.GetComponentsInChildren<MeshRenderer>();
        foreach (var mesh in meshes)
            mesh.enabled = false;

        // Disable looping on the partical system so the particles dissipate instead of suddenly disappearing
        var main = powerup.GetComponentInChildren<ParticleSystem>().main;
        main.loop = false;

        // Destroy instance after timeout
        Destroy(powerup.gameObject, 1.5f);
    }

    bool UserWinsFightForPowerup(string powerupId, string userId, long timestamp)
    {
        // We shouldn't be picking things up twice!
        if (powerupPickedUpBy.ContainsKey(powerupId) && powerupPickedUpBy[powerupId] == userId)
        {
            Debug.LogError("User " + userId + " picked up powerup " + powerupId + " twice");
            return false;
        }

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

using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Powerup : MonoBehaviour
{
    public Vector3 direction;
    public string powerupId;
    public PowerupController powerupCtrl;

    private bool grounded;

    void Start()
    {
        grounded = false;
    }

    void Update()
    {
        // FIXME: Make sure powerup always remains in the correct direction! It shouldn't be able to move other than up/down
        if (grounded)
        {
            // TODO: Make the powerup bob up and down (sin/cos style)
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // If the powerup hit a pillar, it's now grounded
        if (collision.collider.GetComponent<PillarBehaviour>() != null)
        {
            grounded = true;
            return;
        }

        // If the powerup hit the LOCAL player, broadcast a message to the powerup controller
        NetworkCharacter netChar = collision.collider.GetComponent<NetworkCharacter>();
        if (netChar != null && netChar.IsLocalPlayer())
        {
            powerupCtrl.BroadcastPickupPowerup(powerupId, netChar.UserId());
            // TODO: Destroy powerup and play a sound, all fancy-like (maybe logic and assets exist in the prefab?)
            return;
        }
    }
}

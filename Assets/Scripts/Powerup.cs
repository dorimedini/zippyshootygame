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

    private bool grounded, locallyPickedUp;

    void Start()
    {
        locallyPickedUp = grounded = false;
    }

    void Update()
    {
        if (grounded)
        {
            // TODO: Make the powerup bob up and down (sin/cos style)
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (locallyPickedUp) return;

        // If the powerup hit a pillar, it's now grounded. Shouldn't move anymore
        if (collision.collider.GetComponent<PillarBehaviour>() != null)
        {
            grounded = true;
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<GravityAffected>().enabled = false;
            return;
        }

        // If the powerup hit the LOCAL player, broadcast a message to the powerup controller
        NetworkCharacter netChar = collision.collider.GetComponent<NetworkCharacter>();
        if (netChar != null && netChar.IsLocalPlayer())
        {
            powerupCtrl.BroadcastPickupPowerup(powerupId);
            powerupCtrl.PowerupPickedUp(powerupId);
            locallyPickedUp = true;
            return;
        }
    }

    public float Radius() { return GetComponent<SphereCollider>().radius * transform.localScale.x; }
}

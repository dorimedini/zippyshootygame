using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Powerup : MonoBehaviour
{
    public Vector3 direction;
    public string powerupId;
    public int powerupIdx;
    public PowerupController powerupCtrl;

    private bool grounded, locallyPickedUp;
    private Vector3 groundedBaseLocation;
    private float bobDegree;
    private const float bobRadius = 0.5f;

    void Start()
    {
        locallyPickedUp = grounded = false;
    }

    void FixedUpdate()
    {
        if (grounded)
        {
            bobDegree += Time.deltaTime;
            transform.position = groundedBaseLocation + bobRadius * Mathf.Cos(bobDegree) * direction.normalized;
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
            // For bobbing motion, set current counter (degree) to minimal bob value (close to ground) and set the center of
            // the bob motion to current position plus bobbing radius
            bobDegree = 0;
            groundedBaseLocation = transform.position - bobRadius * direction.normalized;
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

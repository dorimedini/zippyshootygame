using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using System;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public Rigidbody rb;

    // Projectiles should ignore collision with the shooter player.
    // This string may be empty if in offline mode, but that's OK we only use this for uniqueness
    public string shooterId;

    // Used to destroy all instances of projectile on all clients
    public string projectileId;

    private bool destroyed;

    public bool lockedOn;
    public TargetableCharacter target;

    Mesh mesh;
    MeshRenderer rend;
    PillarExtensionController pillarCtrl;
    ProjectileController projectileCtrl;
    ExplosionController explosionCtrl;

    List<int> octoTriangles, hexTriangles, squareTriangles;

    // Start is called before the first frame update
    void Start()
    {
        destroyed = false;
        InitControllers();
    }

    void Update()
    {
        if (!lockedOn)
        {
            // Rotate the head of the missile in the direction of the current speed
            transform.up = rb.velocity.normalized;
        }
        else
        {
            // Set U to be the up vector of the missile (what we want facing the target) and let D be the vector from the projectile
            // to the target.
            // We want to rotate Angle(U,V) degrees (capped by missile turn speed) towards V round the UxV axis (or VxU?).
            // Quaternion.Lerp doesn't rotate around the correct axis, so we need to use RotateAround; manually cap the rotation angle
            Vector3 targetDirection = (target.centerTransform.position - transform.position).normalized;
            Vector3 rotationAxis = Vector3.Cross(transform.up, targetDirection);
            // Take missile turn speed to be the maximal angle change per frame.
            float angle = Mathf.Min(Vector3.Angle(transform.up, targetDirection), UserDefinedConstants.missileTurnSpeed);
            // When the missile starts going fast the turnspeed needs a boost, otherwise the missile will miss if it's in the air for too long.
            // Proportional to the square root of the velocity means the missile will have a harder time turning the longer it's in the air, but still
            // has a better turn speed the faster it goes. This doesn't look so bad
            float speedOffsetMultplier = 5 * Mathf.Sqrt(rb.velocity.magnitude);
            transform.RotateAround(transform.position, rotationAxis, Mathf.Min(speedOffsetMultplier * UserDefinedConstants.missileTurnSpeed, angle));
            // Accelerate
            rb.velocity = (rb.velocity.magnitude + UserDefinedConstants.missileAcceleration * Time.deltaTime) * transform.up;
        }
    }

    void InitControllers()
    {
        pillarCtrl = GameObject.Find("_GLOBAL_VIEWS").GetComponentInChildren<PillarExtensionController>();
        if (pillarCtrl == null)
            Debug.LogError("Got null PillarExtensionController");
        projectileCtrl = GameObject.Find("_GLOBAL_VIEWS").GetComponentInChildren<ProjectileController>();
        if (projectileCtrl == null)
            Debug.LogError("Got null ProjectileController");
        explosionCtrl = GameObject.Find("_GLOBAL_VIEWS").GetComponentInChildren<ExplosionController>();
        if (explosionCtrl == null)
            Debug.LogError("Got null ExplosionController");
    }

    /** Only the shooter's instance of the projectile has a collider */
    void OnCollisionEnter(Collision col)
    {
        if (destroyed) return; // Don' process more than one collision... hope this helps...?

        // The first thing a projectile hits should destroy it (unless it's the shooter)
        GameObject obj = col.gameObject;

        // Check if it's a player. If it is, filter out the shooter.
        PlayerMovementController pmc = obj.GetComponent<PlayerMovementController>();
        bool hitPlayer = (pmc != null);
        if (hitPlayer)
        {
            // Is this the shooter?
            if (obj.GetComponent<PhotonView>().Owner.UserId == shooterId)
                return;
        }

        // Did we hit a pillar?
        PillarBehaviour pillar = obj.GetComponent<PillarBehaviour>();
        bool hitPillar = (pillar != null);
        if (hitPillar)
        {
            // TODO: One day I'll find out why objects are suddenly null...
            if (pillarCtrl == null)
                InitControllers();
            pillarCtrl.BroadcastHitPillar(pillar.id);
        }

        // Is this projectile seeking a targetable player? If so we need to tell him we're not seeking him anymore
        if (lockedOn)
        {
            target.BroadcastBecameUntargeted(shooterId);
        }

        // Did we hit the sun?
        SunController sun = obj.GetComponent<SunController>();
        if (sun != null)
            sun.Hit(shooterId);

        // In any case, all collisions destroy the projectile
        explosionCtrl.BroadcastExplosion(transform.position, shooterId);
        projectileCtrl.BroadcastDestroyProjectile(projectileId);
        destroyed = true;
    }
}

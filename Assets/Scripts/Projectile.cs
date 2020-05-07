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
    public Transform target;

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
            // Rotate to face target ('up' direction is the missile pointy bit) and accelerate in target's direction.
            // Don't turn immediately though, limit by turn speed.
            // We want to look at the sun, with 'up' direction towards target.
            Vector3 targetDirection = (target.position - transform.position).normalized;
            Vector3 perpToTarget = Quaternion.Euler(0, 90, 0) * targetDirection;
            Quaternion targetRotation = Quaternion.LookRotation(perpToTarget, targetDirection);
            // Take missile turn speed to be the maximal angle change per frame.
            float angle = Quaternion.Angle(transform.rotation, targetRotation);
            // When the missile starts going fast the turnspeed needs a boost, otherwise the missile will miss if it's in the air for too long
            float speedOffsetMultplier = 5 * Mathf.Sqrt(rb.velocity.magnitude);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Mathf.Clamp01(speedOffsetMultplier * UserDefinedConstants.missileTurnSpeed / angle));
            // Accelerate
            rb.velocity = (rb.velocity.magnitude + UserDefinedConstants.missileAcceleration * Time.deltaTime) * transform.up;
            // FIXME: Quaternion.Lerp doesn't seem like it's lerping around the axis I want.
            // To fix this we need finer control over what the target rotation is:
            // Set U to be the up vector of the missile (what we want facing the target) and let D be the vector from the projectile
            // to the target.
            // We want to rotate Angle(U,V) degrees (capped by missile turn speed) towards V round the UxV axis (or VxU?).
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

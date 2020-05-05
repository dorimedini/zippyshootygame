using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;


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

    private bool lockedOn;

    Mesh mesh;
    MeshRenderer rend;
    PillarExtensionController pillarCtrl;
    ProjectileController projectileCtrl;
    ExplosionController explosionCtrl;

    List<int> octoTriangles, hexTriangles, squareTriangles;

    // Start is called before the first frame update
    void Start()
    {
        lockedOn = destroyed = false;
        InitControllers();
    }

    void Update()
    {
        if (!lockedOn)
        {
            // Just rotate the head of the missile in the direction of the current speed
            transform.up = rb.velocity.normalized;
        }
        else
        {
            // TODO: Implement lock-on mechanism for shooting characters
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

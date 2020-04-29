﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    // Projectiles should ignore collision with the shooter player.
    // This string may be empty if in offline mode, but that's OK we only use this for uniqueness
    public string shooterId;

    // Used to destroy all instances of projectile on all clients
    public string projectileId;

    private bool destroyed;

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
        mesh = GetComponent<MeshFilter>().mesh;
        rend = GetComponent<MeshRenderer>();
        mesh.MarkDynamic();
        octoTriangles = new List<int>();
        hexTriangles = new List<int>();
        squareTriangles = new List<int>();

        // First 12*8 triangles are the 12 octogons.
        // The next (12*5/2) * 2 triangles are the little square gaps between octogons.
        // The remaining are the hexagons. Each hex touches 3 octogons so there are (12*5/3) hexagons
        // with 4 triangles in each, so a total of 4*12*5/3=80.
        // Multiply all those by 3 to get the number of vertices related to the triangles.
        var triangles = mesh.GetTriangles(0);
        int octs = 3 * 12 * 8;
        int squares = 3 * 12 * 5;
        int hexes = 3 * 4 * 12 * 5 / 3;
        for (int t = 0; t < octs; ++t)
            octoTriangles.Add(triangles[t]);
        for (int t = octs; t < octs + squares; ++t)
            squareTriangles.Add(triangles[t]);
        for (int t = octs + squares; t < octs + squares + hexes; ++t)
            hexTriangles.Add(triangles[t]);

        mesh.subMeshCount = 3;
        mesh.SetTriangles(octoTriangles, 0);
        mesh.SetTriangles(squareTriangles, 1);
        mesh.SetTriangles(hexTriangles, 2);

        Material color0 = Resources.Load("Materials/projectile_blue_0", typeof(Material)) as Material;
        Material color1 = Resources.Load("Materials/projectile_blue_1", typeof(Material)) as Material;
        Material color2 = Resources.Load("Materials/projectile_blue_2", typeof(Material)) as Material;
        List<Material> colorMats = new List<Material>() { color0, color1, color2 };
        rend.materials = colorMats.ToArray();
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

        // Did we hit a pillar or a player?
        PillarBehaviour pillar = obj.GetComponent<PillarBehaviour>();
        bool hitPillar = (pillar != null);
        if (hitPillar)
        {
            // TODO: One day I'll find out why objects are suddenly null...
            if (pillarCtrl == null)
                InitControllers();
            pillarCtrl.BroadcastHitPillar(pillar.id);
        }

        // In any case, all collisions destroy the projectile
        explosionCtrl.BroadcastExplosion(transform.position, shooterId);
        projectileCtrl.BroadcastDestroyProjectile(projectileId);
        destroyed = true;
    }
}

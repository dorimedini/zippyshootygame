﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    // Projectiles should ignore collision with the shooter player
    public int shooterId;

    private bool destroyed;

    Mesh mesh;
    MeshRenderer renderer;
    Rigidbody rb;

    List<int> octoTriangles, hexTriangles, squareTriangles;

    // Start is called before the first frame update
    void Start()
    {
        destroyed = false;
        rb = GetComponent<Rigidbody>();
        mesh = GetComponent<MeshFilter>().mesh;
        renderer = GetComponent<MeshRenderer>();
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
        renderer.materials = colorMats.ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        GeoPhysics.ApplyGravity(rb);
    }

    void OnCollisionEnter(Collision col)
    {
        if (destroyed) return; // Don' process more than one collision... hope this helps...?

        // The first thing a projectile hits should destroy it (unless it's the shooter)
        GameObject obj = col.gameObject;
        if (obj.GetInstanceID() == shooterId)
            return;

        // Did we hit a tile or a player?
        TileBehaviour tile = obj.GetComponent<TileBehaviour>();
        FirstPersonController fps = obj.GetComponent<FirstPersonController>();
        if (tile != null)
        {
            tile.projectileHit();
        }
        else if (fps != null)
        {
            // TODO: Implement damage
        }

        Debug.Log(string.Format("Projectile destroyed after hitting a {0} object", fps == null ? "non-player" : "player"));
        Destroy(gameObject);
        destroyed = true;
    }
}

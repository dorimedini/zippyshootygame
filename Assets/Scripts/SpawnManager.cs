using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnManager : MonoBehaviour
{
    GeoSphereGenerator gsg;
    List<TileBehaviour> tiles;

    // How far above ground do players spawn (in addition to the offset caused by initialHeight)
    public float spawnHeight;

    void Start()
    {
        gsg = GameObject.Find("GeoSphere").GetComponent<GeoSphereGenerator>();
        if (gsg == null)
            Debug.LogError("Got null GeoSphereGenerator");
        tiles = gsg.GetTiles();
    }

    public List<Tuple<Vector3, Quaternion>> PlayerSpawnPoints()
    {
        // Players should spawn above one of the pentagons.
        // Need to spawn them high enough s.t. they don't fall through; say, initialHeight+something.
        // Also, after setting the 'up' direction as the center of the sphere, we need to give a random look direction.
        var spawns = new List<Tuple<Vector3, Quaternion>>();
        for (int i=0; i<12; ++i)
        {
            var pent = tiles[i];
            Vector3 pentPoint = pent.transform.position;
            spawns.Add(new Tuple<Vector3, Quaternion>(
                pent.transform.position + (pent.currentHeight + spawnHeight) * (-pentPoint.normalized),
                Quaternion.LookRotation(Tools.Geometry.RandomDirectionOnPlane(-pentPoint.normalized), -pentPoint.normalized)
            ));
        }
        return spawns;
    }

    public GameObject SpawnMyself()
    {
        var spawnPoints = PlayerSpawnPoints();
        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        Vector3 spawnLoc = spawnPoint.Item1;
        Quaternion spawnRot = spawnPoint.Item2;
        Debug.Log(string.Format("Spawning player at location/rotation {0}/{1}", spawnLoc, spawnRot));
        GameObject player = PhotonNetwork.Instantiate("Player", spawnLoc, spawnRot);
        // Good, now activate all relevant scripts
        player.GetComponentInChildren<Camera>().enabled = true;
        player.GetComponentInChildren<AudioListener>().enabled = true;
        player.GetComponentInChildren<MouseLookController>().enabled = true;
        player.GetComponentInChildren<PlayerMovementController>().enabled = true;
        player.GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;   // First-person doesn't see own body
        return player;
    }
}

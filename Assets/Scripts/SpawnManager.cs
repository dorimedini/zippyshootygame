using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SpawnManager : MonoBehaviour
{
    public GameObject spawnCam;
    public GeoSphereGenerator gsg;
    public RagdollController ragdollCtrl;
    public MessagesController msgCtrl;

    List<PillarBehaviour> pillars = null;
    private float respawnTime;
    private bool ragdollActive;
    private GameObject activeRagdoll;

    // How far above ground do players spawn (in addition to the offset caused by initialHeight)
    public float spawnHeight;

    void Start()
    {
        InitLocalVariables();
    }

    void Update()
    {
        respawnTime = Mathf.Max(0, respawnTime - Time.deltaTime);
        if (ragdollActive)
        {
            msgCtrl.ReplaceLine(string.Format("Respawn in {0:f2}", respawnTime));
            spawnCam.transform.LookAt(activeRagdoll.transform.position);
            if (Tools.NearlyEqual(respawnTime, 0, 0.01f))
            {
                ragdollActive = false;
                SpawnMyself();
                DisableSpawnCamera();
            }
        }
    }

    void InitLocalVariables()
    {
        pillars = gsg.GetPillars();
    }

    public List<Tuple<Vector3, Quaternion>> PlayerSpawnPoints()
    {
        // Players should spawn above one of the pentagons.
        // Need to spawn them high enough s.t. they don't fall through; say, initialHeight+something.
        // Also, after setting the 'up' direction as the center of the sphere, we need to give a random look direction.
        if (pillars == null)
            InitLocalVariables();
        var spawns = new List<Tuple<Vector3, Quaternion>>();
        for (int i=0; i<12; ++i)
        {
            var pent = pillars[i];
            Vector3 pentPoint = pent.transform.position;
            spawns.Add(new Tuple<Vector3, Quaternion>(
                pent.transform.position + (pent.currentHeight + spawnHeight) * (-pentPoint.normalized),
                Quaternion.LookRotation(Tools.Geometry.RandomDirectionOnPlane(-pentPoint.normalized), -pentPoint.normalized)
            ));
        }
        return spawns;
    }

    public void KillAndRespawn(GameObject player)
    {
        if (player.transform.GetComponent<PhotonView>().InstantiationId == 0)
        {
            // Local offline player
            Destroy(player);
        }
        else
        {
            // Ragdoll is basically graphics, have every player instantiate it locally. Desync in location of the ragdoll is acceptable.
            // We spawn our own ragdoll though, so we can tell the spawn camera to follow the ragdoll
            ragdollCtrl.BroadcastRagdoll(player.transform.position, player.transform.rotation, UserDefinedConstants.spawnTime);
            activeRagdoll = Instantiate(RagdollController.ragdoll, player.transform.position, player.transform.rotation);
            Destroy(activeRagdoll, UserDefinedConstants.spawnTime + 0.2f); // Give some legroom so the spawn camera can access position of ragdoll while waiting to spawn
            ragdollActive = true;
            respawnTime = UserDefinedConstants.spawnTime;
            msgCtrl.AppendMessage("U DED");
            msgCtrl.AppendMessage(string.Format("Respawn in {0:f2}", UserDefinedConstants.spawnTime));
            ActivateSpawnCam(activeRagdoll);
            PhotonNetwork.Destroy(player);
        }
    }

    public GameObject SpawnMyself()
    {
        msgCtrl.AppendMessage("Respawning...");
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
        player.GetComponentInChildren<GravityAffected>().enabled = true;
        player.GetComponentInChildren<ShootingCharacter>().enabled = true;
        player.GetComponentInChildren<CrosshairGUIController>().enabled = true;
        player.GetComponentInChildren<HeadTowardsOrigin>().enabled = true;
        player.transform.Find("UI").gameObject.SetActive(true);
        // Remote players have isKinematic set to true by default. This prevents jitter, because player position/rotation is defined completely
        // by the transform component.
        // Local players must be kinematic because they apply forces to themselves (jump, explosion knockback, grapple...)
        player.GetComponent<Rigidbody>().isKinematic = false;
        return player;
    }

    private void ActivateSpawnCam(GameObject follow)
    {
        spawnCam.SetActive(true);
        spawnCam.transform.LookAt(follow.transform.position);
    }

    private void DisableSpawnCamera() { spawnCam.SetActive(false); }
}

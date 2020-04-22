using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ProjectileController : MonoBehaviourPun
{
    public static int projectileIdLength = 10;

    public GameObject projectilePrefab;

    private Dictionary<string, GameObject> activeProjectiles;

    void Start()
    {
        InitActiveProjectileDict();
    }

    void InitActiveProjectileDict()
    {
        activeProjectiles = new Dictionary<string, GameObject>();
    }

    public void BroadcastFireProjectile(Vector3 source, Vector3 force, string shooterId)
    {
        string projectileId = RandomStrings.Generate(projectileIdLength);
        photonView.RPC("FireProjectile", RpcTarget.All, source, force, shooterId, projectileId);
    }

    /** We don't want to send each projectile's location updates on the network, so let's hope initial spawn location 
     *  and force vector is good enough for syncing */
    [PunRPC]
    void FireProjectile(Vector3 source, Vector3 force, string shooterId, string projectileId)
    {
        // TODO: Once again, find why these are sometimes null..
        if (activeProjectiles == null)
            InitActiveProjectileDict();
        GameObject projectile = Instantiate(projectilePrefab, source, Quaternion.identity);
        activeProjectiles[projectileId] = projectile;
        projectile.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
        projectile.GetComponent<Projectile>().shooterId = shooterId;
        projectile.GetComponent<Projectile>().projectileId = projectileId;
        // FIXME: This seems to cause a single "master" player to be the only one who enables the colliders on projectiles.
        // FIXME: Maybe because this script isn't on a player prefab...?
        if (photonView.IsMine)
        {
            projectile.GetComponent<MeshCollider>().enabled = true;
        }
    }

    public void BroadcastDestroyProjectile(string projectileId)
    {
        photonView.RPC("DestroyProjectile", RpcTarget.All, projectileId);
    }

    [PunRPC]
    void DestroyProjectile(string projectileId)
    {
        Destroy(activeProjectiles[projectileId]);
        activeProjectiles.Remove(projectileId);
    }
}

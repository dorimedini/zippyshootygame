using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ProjectileController : MonoBehaviourPun
{
    public static int projectileIdLength = 10;

    public GameObject projectilePrefab;
    public GameObject explosionPrefab;
    public AudioClip explosionSound;
    public AudioClip[] fireSounds;
    public AudioSource localFireSoundSource;

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
        // The local player instantiates a projectile with a collider, the rest don't. So, fire the RPC first, then instantiate
        // our own copy (we'll instantiate faster than the rest anyway)
        string projectileId = RandomStrings.Generate(projectileIdLength);
        photonView.RPC("FireProjectile", RpcTarget.Others, source, force, shooterId, projectileId);
        GameObject projectile = InstantiateProjectileWithoutCollider(source, force, shooterId, projectileId);
        projectile.GetComponent<MeshCollider>().enabled = true;
        // Local player needs to hear his own shot anyway, but sometimes when player is traveling fast it fades fast.
        // To solve this, the remote players will call PlayClipAtPoint, while the local player uses the local audiosource.
        localFireSoundSource.volume = UserDefinedConstants.shotSoundVolume;
        localFireSoundSource.clip = RandomShotSound();
        localFireSoundSource.Play();
    }

    /** We don't want to send each projectile's location updates on the network, so let's hope initial spawn location 
     *  and force vector is good enough for syncing */
    [PunRPC]
    void FireProjectile(Vector3 source, Vector3 force, string shooterId, string projectileId)
    {
        InstantiateProjectileWithoutCollider(source, force, shooterId, projectileId);
        AudioSource.PlayClipAtPoint(RandomShotSound(), source, UserDefinedConstants.shotSoundVolume);
    }

    public void BroadcastDestroyProjectile(string projectileId)
    {
        photonView.RPC("DestroyProjectile", RpcTarget.All, projectileId);
    }

    [PunRPC]
    void DestroyProjectile(string projectileId)
    {
        var explosion = Instantiate(explosionPrefab, activeProjectiles[projectileId].transform.position, activeProjectiles[projectileId].transform.rotation);
        AudioSource.PlayClipAtPoint(explosionSound, activeProjectiles[projectileId].transform.position);
        Destroy(activeProjectiles[projectileId]);
        activeProjectiles.Remove(projectileId);
        Destroy(explosion, 3f);
    }

    GameObject InstantiateProjectileWithoutCollider(Vector3 source, Vector3 force, string shooterId, string projectileId)
    {
        // TODO: Once again, find why these are sometimes null..
        if (activeProjectiles == null)
        {
            Debug.LogWarning("activeProjectiles is null!");
            InitActiveProjectileDict();
        }
        GameObject projectile = Instantiate(projectilePrefab, source, Quaternion.identity);
        activeProjectiles[projectileId] = projectile;
        projectile.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
        projectile.GetComponent<Projectile>().shooterId = shooterId;
        projectile.GetComponent<Projectile>().projectileId = projectileId;
        return projectile;
    }

    AudioClip RandomShotSound() { return fireSounds[Mathf.FloorToInt(Random.Range(0, fireSounds.Length - 0.01f))]; }
}

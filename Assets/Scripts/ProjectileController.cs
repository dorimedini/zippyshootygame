using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

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

    public void BroadcastFireSeekingProjectile(Vector3 source, Vector3 force, string shooterId, string targetUserId)
    {
        BroadcastFireProjectileAux(source, force, Vector3.zero, shooterId, true, targetUserId);
    }
    public void BroadcastFireProjectile(Vector3 source, Vector3 force, Vector3 currentShooterSpeed, string shooterId)
    {
        BroadcastFireProjectileAux(source, force, currentShooterSpeed, shooterId, false, "");
    }
    void BroadcastFireProjectileAux(Vector3 source, Vector3 force, Vector3 currentShooterSpeed, string shooterId, bool seeking, string targetUserId)
    {
        // The local player instantiates a projectile with a collider, the rest don't. So, fire the RPC first, then instantiate
        // our own copy (we'll instantiate faster than the rest anyway)
        string projectileId = RandomStrings.Generate(projectileIdLength);
        photonView.RPC("FireProjectile", RpcTarget.Others, source, force, currentShooterSpeed, shooterId, projectileId, seeking, targetUserId);
        GameObject projectile = InstantiateProjectileWithoutCollider(source, force, currentShooterSpeed, shooterId, projectileId, seeking, targetUserId);
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
    void FireProjectile(Vector3 source, Vector3 force, Vector3 currentShooterSpeed, string shooterId, string projectileId, bool seeking, string targetUserId)
    {
        InstantiateProjectileWithoutCollider(source, force, currentShooterSpeed, shooterId, projectileId, seeking, targetUserId);
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

    GameObject InstantiateProjectileWithoutCollider(Vector3 source, Vector3 force, Vector3 currentShooterSpeed, string shooterId, string projectileId, bool seeking, string targetUserId)
    {
        // TODO: Once again, find why these are sometimes null..
        if (activeProjectiles == null)
        {
            Debug.LogWarning("activeProjectiles is null!");
            InitActiveProjectileDict();
        }
        GameObject projectileObj = Instantiate(projectilePrefab, source, Quaternion.identity);
        activeProjectiles[projectileId] = projectileObj;
        projectileObj.GetComponent<Rigidbody>().velocity = seeking ?
            force.normalized :
            currentShooterSpeed;
        projectileObj.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        projectile.shooterId = shooterId;
        projectile.projectileId = projectileId;
        projectile.lockedOn = seeking;
        if (seeking)
        {
            // Set initial transform to align to correct direction
            projectile.transform.up = force.normalized;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.UserId == targetUserId)
                {
                    projectile.target = NetworkCharacter.GetPlayerCenter(player);
                    break;
                }
            }
        }
        return projectileObj;
    }

    AudioClip RandomShotSound() { return fireSounds[Mathf.FloorToInt(Random.Range(0, fireSounds.Length - 0.01f))]; }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class SunController : MonoBehaviourPun
{
    public int chargeStages;
    public Material mat;
    public Color originalColor;
    public GameObject shockwavePrefab;
    public GameObject[] powerupPrefabs;
    public SunrayController sunray;
    public SunIsAngryController angrySun;
    public DamageController dmgCtrl;

    private int chargeStage;

    // Start is called before the first frame update
    void Start()
    {
        ResetColor();
    }

    public void BroadcastHit(string shooterId)
    {
        if (Random.Range(0, 1) <= UserDefinedConstants.chanceForSunToGivePowerup)
        {
            // Choose a random direction in which to drop the powerup
            Vector3 direction = Random.onUnitSphere;
            int powerupIdx = (new System.Random()).Next(0, powerupPrefabs.Length - 1);
            photonView.RPC("HitAndPowerup", RpcTarget.Others, shooterId, direction, powerupIdx);
            HitAndPowerup(shooterId, direction, powerupIdx);  // Do this locally so player gets quick feedback
        }
        else
        {
            string targetUserId = PhotonNetwork.PlayerList[(new System.Random()).Next(0, PhotonNetwork.PlayerList.Length)].UserId;
            photonView.RPC("HitAndTarget", RpcTarget.Others, shooterId, targetUserId);
            HitAndTarget(shooterId, targetUserId);  // Do this locally so player gets quick feedback
            // TODO: Should the sunray really be fired locally...?
        }
    }

    [PunRPC]
    public void HitAndTarget(string shooterId, string targetUserId)
    {
        GeneralSunHit(shooterId);
        SunAngryAt(NetworkCharacter.GetPlayerCenter(NetworkCharacter.GetPlayerByUserID(targetUserId)), shooterId);
    }

    [PunRPC]
    public void HitAndPowerup(string shooterId, Vector3 direction, int powerupIdx)
    {
        GeneralSunHit(shooterId);
        // TODO: Spawn a powerup close to the sun. Colliders on everyone.
        // TODO: Any player who picks up a powerup: display pickup graphics, but delay giving the player the benefits for half a second.
        // TODO: During that time fire an RPC to a powerup controller with the timestamp at time of powerup pickup.
        // TODO: The powerup controller should handle a list of power
    }

    void GeneralSunHit(string shooterId)
    {
        IncrementColor();
        if (chargeStage == chargeStages)
        {
            Overcharge(shooterId);
        }
    }

    void SunAngryAt(Transform target, string shooterId)
    {
        angrySun.Play();
        sunray.GetAngryAt(target, shooterId);
    }

    void Overcharge(string shooterId)
    {
        ResetColor();
        Destroy(Instantiate(shockwavePrefab, Vector3.zero, Quaternion.identity), 5f);

        // Damage and knock back relative to distance from sun
        GameObject playerObj = NetworkCharacter.GetPlayerGameObject(PhotonNetwork.LocalPlayer);
        float dist = playerObj.transform.position.magnitude;
        float damage = UserDefinedConstants.sunDamage * (1 - dist / UserDefinedConstants.sphereRadius);
        dmgCtrl.BroadcastInflictDamage(shooterId, damage, PhotonNetwork.LocalPlayer.UserId);
        playerObj.GetComponent<Rigidbody>().AddForce(playerObj.transform.position.normalized * damage, ForceMode.Impulse);
    }

    void OnDestroy()
    {
        ResetColor();
    }

    void ResetColor()
    {
        chargeStage = 0;
        mat.color = originalColor;
    }

    void IncrementColor()
    {
        ++chargeStage;
        mat.color = Color.Lerp(originalColor, Color.red, (float)chargeStage / (chargeStages + 1));
    }
}

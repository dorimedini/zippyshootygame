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
    public SunrayController sunray;
    public SunIsAngryController angrySun;
    public DamageController dmgCtrl;
    public PowerupController powerupCtrl;

    private int chargeStage;

    // Start is called before the first frame update
    void Start()
    {
        ResetColor();
    }

    public void BroadcastHit(string shooterId)
    {
        if (Random.value <= UserDefinedConstants.chanceForSunToGivePowerup)
        {
            // Choose a random direction in which to drop the powerup
            Vector3 direction = Random.onUnitSphere;
            int powerupIdx = powerupCtrl.RandomPowerupIdx();
            string powerupId = Tools.GenerateRandomString(16);
            photonView.RPC("HitAndPowerup", RpcTarget.Others, shooterId, direction, powerupIdx, powerupId);
            HitAndPowerup(shooterId, direction, powerupIdx, powerupId);  // Do this locally so player gets quick feedback
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
    public void HitAndPowerup(string shooterId, Vector3 direction, int powerupIdx, string powerupId)
    {
        GeneralSunHit(shooterId);
        powerupCtrl.SpawnPowerup(direction, powerupIdx, powerupId);
    }

    public float Radius() { return GetComponent<SphereCollider>().radius * transform.localScale.x; }

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

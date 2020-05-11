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

    private int chargeStage;

    // Start is called before the first frame update
    void Start()
    {
        ResetColor();
    }

    public void BroadcastHit(string shooterId)
    {
        string targetUserId = PhotonNetwork.PlayerList[(new System.Random()).Next(0, PhotonNetwork.PlayerList.Length)].UserId;
        photonView.RPC("Hit", RpcTarget.Others, shooterId, targetUserId);
        Hit(shooterId, targetUserId);  // Do this locally so player gets quick feedback
        // TODO: Should the sunray really be fired locally...?
    }

    [PunRPC]
    public void Hit(string shooterId, string targetUserId)
    {
        IncrementColor();
        if (chargeStage == chargeStages)
        {
            Overcharge(shooterId);
        }
        SunAngryAt(NetworkCharacter.GetPlayerCenter(NetworkCharacter.GetPlayerByUserID(targetUserId)), shooterId);
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

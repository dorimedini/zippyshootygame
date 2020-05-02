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
    public DamageController dmgCtrl;

    private int chargeStage;

    // Start is called before the first frame update
    void Start()
    {
        chargeStage = 0;
    }

    public void BroadcastHit(string shooterId)
    {
        photonView.RPC("Hit", RpcTarget.Others, shooterId);
        Hit(shooterId);  // Do this locally so player gets quick feedback
    }

    [PunRPC]
    public void Hit(string shooterId)
    {
        ++chargeStage;
        mat.color = Color.Lerp(originalColor, Color.red, (float)chargeStage / (chargeStages + 1));
        if (chargeStage == chargeStages)
        {
            Overcharge(shooterId);
        }
    }

    void Overcharge(string shooterId)
    {
        chargeStage = 0;
        mat.color = originalColor;
        Destroy(Instantiate(shockwavePrefab, Vector3.zero, Quaternion.identity), 5f);

        // Damage and knock back relative to distance from sun
        GameObject playerObj = PhotonNetwork.LocalPlayer.TagObject as GameObject;
        float dist = playerObj.transform.position.magnitude;
        float damage = UserDefinedConstants.sunDamage * (1 - dist / UserDefinedConstants.sphereRadius);
        dmgCtrl.BroadcastInflictDamage(shooterId, damage, PhotonNetwork.LocalPlayer.UserId);
        playerObj.GetComponent<Rigidbody>().AddForce(playerObj.transform.position.normalized * damage, ForceMode.Impulse);
    }
}

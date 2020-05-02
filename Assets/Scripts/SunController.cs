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
        Debug.Log("Overcharge");
        Destroy(Instantiate(shockwavePrefab, Vector3.zero, Quaternion.identity), 5f);
        // TODO: Every player will call this, so display graphics and damage/knock back the LOCAL player.
    }
}

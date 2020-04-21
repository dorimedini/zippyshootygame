using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class DamageController : MonoBehaviourPun
{
    public void BroadcastInflictDamage(string shooterId, int damage, string targetUserId)
    {
        photonView.RPC("InflictDamage", RpcTarget.All, shooterId, damage, targetUserId);
    }

    /** Every client should run this so everyone knows how much health each player has */
    [PunRPC]
    void InflictDamage(string shooterID, int damage, string targetUserId)
    {
        Dictionary<int, Player> playerDict = PhotonNetwork.CurrentRoom.Players;
        foreach (Player player in playerDict.Values)
        {
            if (player.UserId == targetUserId)
            {
                GameObject obj = player.TagObject as GameObject;
                Health h = obj.GetComponent<Health>();
                if (h == null)
                    Debug.LogError("No Health component found on component with a PlayerMoveController!");
                h.InflictDamage(damage);
                return;
            }
        }
        Debug.LogError(string.Format("No player with userID {0} found!", targetUserId));
    }
}

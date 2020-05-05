using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PillarExtensionController : MonoBehaviourPun
{
    List<PillarBehaviour> pillars = null;

    public void Init(List<PillarBehaviour> pillars)
    {
        this.pillars = pillars;
    }

    public void BroadcastHitPillar(int pillarId)
    {
        photonView.RPC("HitPillar", RpcTarget.All, pillarId);
    }

    [PunRPC]
    void HitPillar(int pillarId)
    {
        // TODO: Find out why this can happen and why GetPillars() helps...
        if (pillarId != pillars[pillarId].id)
        {
            Debug.LogError("Have stale pillar IDs");
            // TODO: Declare this code dead one day
            /* 
            pillars = gsg.GetPillars();
            if (pillarId != pillars[pillarId].id)
            {
                Debug.LogError(string.Format("FATAL: pillar IDs and indices don't match up: pillars[{0}].id=={1}", pillarId, pillars[pillarId].id));
                return;
            }
            */
        }
        pillars[pillarId].projectileHit();
    }
}

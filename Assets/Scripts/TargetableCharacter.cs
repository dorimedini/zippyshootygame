using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TargetableCharacter : MonoBehaviourPun
{
    public void BecameTargeted()
    {
        // TODO: Give remote player some indication he's being targeted
    }

    public void BecameLockedOn()
    {
        // TODO: Maybe now the remote player should be much more stressed out
    }

    public void BecameUntargeted()
    {
        // TODO: Stop whatever locking indicators were active on the remote player.
        // TODO: If remote player is hit, STOP LOCK INDICATORS IMMEDIATELY ON REMOTE MACHINE
    }
}
